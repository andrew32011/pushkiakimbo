using System.Collections.Generic;
using UnityEngine;

namespace CrowdRunner
{
    // Блок-бонус, едущий по боковой дорожке навстречу отряду. Имеет HP — выбивается пулями
    // (или собирается касанием). Даёт юнитов либо апгрейд оружия. Встаёт в очередь за теми,
    // что впереди на той же дорожке, не наслаиваясь.
    public class Booster : MonoBehaviour
    {
        private enum Kind { Units, Weapon }

        [SerializeField] private TextMesh _label;
        [SerializeField] private BoxCollider _zone;

        private static readonly List<Booster> Active = new List<Booster>();

        private Kind _kind = Kind.Units;
        private float _hp, _maxHp;
        private int _bonus = 5;
        private float _speed = 3.5f;
        private float _stopDist = 5f;   // дистанция головного блока перед игроком
        private float _gap = 2.2f;      // дистанция в очереди
        private float _vulnDist = 7f;   // ближе этого можно пробить пулями, дальше — нет
        private bool _dead;

        public bool IsDead => _dead;

        public void Init(int bonus, float hp, float speed, float stopDist, float gap, float vulnDist)
        {
            _kind = Kind.Units;
            _bonus = Mathf.Max(1, bonus);
            Setup(hp, speed, stopDist, gap, vulnDist);
        }

        public void InitWeapon(float hp, float speed, float stopDist, float gap, float vulnDist)
        {
            _kind = Kind.Weapon;
            Setup(hp, speed, stopDist, gap, vulnDist);
        }

        private void Setup(float hp, float speed, float stopDist, float gap, float vulnDist)
        {
            _maxHp = _hp = Mathf.Max(1f, hp);
            _speed = speed; _stopDist = stopDist; _gap = gap; _vulnDist = vulnDist; _dead = false;
            if (_zone != null) _zone.isTrigger = true;
            UpdateLabel();
            TintForKind();
        }

        private void OnEnable() { if (!Active.Contains(this)) Active.Add(this); }
        private void OnDisable() { Active.Remove(this); }

        private void Update()
        {
            if (_dead) return;
            var gm = RunnerGameManager.Instance;
            if (gm == null || gm.Phase != GamePhase.Running) return;
            var squad = gm.Squad;
            if (squad == null) return;

            float myX = transform.position.x, myZ = transform.position.z;

            // нижняя граница: головной блок встаёт на _stopDist, остальные — за тем, кто впереди
            float floorZ = squad.Center.z + _stopDist;
            for (int i = 0; i < Active.Count; i++)
            {
                var b = Active[i];
                if (b == null || b == this || b._dead) continue;
                if (Mathf.Abs(b.transform.position.x - myX) > 1f) continue; // другая дорожка
                float bz = b.transform.position.z;
                if (bz < myZ) floorZ = Mathf.Max(floorZ, bz + _gap);
            }

            float nz = Mathf.Max(myZ - _speed * Time.deltaTime, floorZ);
            transform.position = new Vector3(myX, transform.position.y, nz);
        }

        public void TakeDamage(float dmg)
        {
            if (_dead) return;
            // пока блок не доехал близко по своей дорожке — пулями не пробить
            var squad = RunnerGameManager.Instance?.Squad;
            if (squad != null && (transform.position.z - squad.Center.z) > _vulnDist) return;
            _hp -= dmg;
            EffectsManager.Burst(transform.position + Vector3.up * 1f, KindColor(), 0.4f);
            if (_hp <= 0f) Collect();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_dead) return;
            var sq = other.GetComponentInParent<SquadController>();
            if (sq != null) Collect();
        }

        private void Collect()
        {
            if (_dead) return;
            _dead = true;
            Active.Remove(this);

            var squad = RunnerGameManager.Instance?.Squad;
            if (_kind == Kind.Weapon)
            {
                squad?.UpgradeWeapon();
                EffectsManager.Float(transform.position + Vector3.up * 2f, "ОРУЖИЕ", new Color(1f, 0.82f, 0.2f));
            }
            else
            {
                if (squad != null) squad.ModifyUnits(GateOp.Add, _bonus);
                EffectsManager.Float(transform.position + Vector3.up * 2f, "+" + _bonus, new Color(0.5f, 1f, 0.6f));
            }

            AudioController.Instance?.PlayGate();
            EffectsManager.Burst(transform.position + Vector3.up * 1f, KindColor(), 1.4f);
            Destroy(gameObject);
        }

        private Color KindColor() => _kind == Kind.Weapon ? new Color(1f, 0.82f, 0.2f) : new Color(0.5f, 1f, 0.6f);

        private void TintForKind()
        {
            var rend = GetComponentInChildren<Renderer>();
            if (rend != null && rend.material != null) rend.material.color = KindColor();
        }

        private void UpdateLabel()
        {
            if (_label == null) return;
            _label.text = _kind == Kind.Weapon ? "ОРУЖИЕ" : "+" + _bonus;
            _label.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        }
    }
}
