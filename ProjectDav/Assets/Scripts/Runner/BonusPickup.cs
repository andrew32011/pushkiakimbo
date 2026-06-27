using UnityEngine;

namespace CrowdRunner
{
    // Бонус, подъезжающий сбоку и встающий в очередь ПЕРЕД отрядом (в пределах дороги).
    // Тормозит у отряда, собирается последовательно. Тип: +юниты или апгрейд оружия.
    public class BonusPickup : MonoBehaviour
    {
        public enum Kind { Units, Weapon }

        [SerializeField] private TextMesh _label;
        [SerializeField] private Renderer _background;

        private Kind _kind;
        private int _value;
        private float _roadHalf = 3.6f;
        private float _approach = 3.5f;   // скорость подъезда (-Z)
        private float _easeX = 3f;        // скорость подстройки по X к отряду
        private bool _used;

        public void InitUnits(int value, Color color)
        {
            _kind = Kind.Units; _value = value; _used = false;
            if (_background != null && _background.material != null) _background.material.color = color;
            if (_label != null) { _label.text = "+" + value; _label.transform.rotation = Quaternion.Euler(0f, 180f, 0f); }
        }

        public void InitWeapon(Color color)
        {
            _kind = Kind.Weapon; _used = false;
            if (_background != null && _background.material != null) _background.material.color = color;
            if (_label != null) { _label.text = "ОРУЖИЕ"; _label.transform.rotation = Quaternion.Euler(0f, 180f, 0f); }
        }

        private void Update()
        {
            var gm = RunnerGameManager.Instance;
            if (gm == null || gm.Phase != GamePhase.Running) return;
            var squad = gm.Squad;
            if (squad == null) return;

            Vector3 pos = transform.position;
            float dz = pos.z - squad.Center.z;
            // тормозит возле отряда, чтобы собирать последовательно
            float speed = dz > 6f ? _approach : Mathf.Lerp(0.4f, _approach, Mathf.Clamp01(dz / 6f));
            pos.z -= speed * Time.deltaTime;
            // встаёт перед отрядом по его полосе (в пределах дороги)
            float targetX = Mathf.Clamp(squad.Center.x, -_roadHalf, _roadHalf);
            pos.x = Mathf.MoveTowards(pos.x, targetX, _easeX * Time.deltaTime);
            transform.position = pos;
        }

        public void Collect(SquadController squad)
        {
            if (_used) return;
            _used = true;
            if (_kind == Kind.Weapon) squad.UpgradeWeapon();
            else squad.ModifyUnits(GateOp.Add, _value);
            AudioController.Instance?.PlayGate();
            EffectsManager.Burst(transform.position + Vector3.up * 0.6f, new Color(0.6f, 1f, 0.7f), 1.2f);
            gameObject.SetActive(false);
        }
    }
}
