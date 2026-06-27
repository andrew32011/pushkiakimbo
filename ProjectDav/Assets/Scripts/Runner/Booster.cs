using UnityEngine;

namespace CrowdRunner
{
    // Боковой бонус-блок: имеет HP. Расстреливаешь снарядами (или задеваешь отрядом) —
    // блок разрушается и даёт +N юнитов. Ставятся рядами вдоль края дороги.
    public class Booster : MonoBehaviour
    {
        [SerializeField] private TextMesh _label;
        [SerializeField] private BoxCollider _zone;   // компактный триггер вокруг блока

        private float _hp, _maxHp;
        private int _bonus = 5;
        private bool _dead;

        public bool IsDead => _dead;

        public void Init(int bonus, float hp)
        {
            _bonus = Mathf.Max(1, bonus);
            _maxHp = _hp = Mathf.Max(1f, hp);
            _dead = false;
            UpdateLabel();
            if (_zone != null) _zone.isTrigger = true;
        }

        // Урон от снарядов отряда.
        public void TakeDamage(float dmg)
        {
            if (_dead) return;
            _hp -= dmg;
            EffectsManager.Burst(transform.position + Vector3.up * 1f, new Color(0.5f, 1f, 0.6f), 0.4f);
            if (_hp <= 0f) { Collect(); return; }
            UpdateLabel();
        }

        // Сбор при прямом касании отрядом (если заехали в блок).
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

            var squad = RunnerGameManager.Instance?.Squad;
            if (squad != null) squad.ModifyUnits(GateOp.Add, _bonus);

            AudioController.Instance?.PlayGate();
            EffectsManager.Burst(transform.position + Vector3.up * 1f, new Color(0.5f, 1f, 0.6f), 1.4f);
            EffectsManager.Float(transform.position + Vector3.up * 2f, "+" + _bonus, new Color(0.5f, 1f, 0.6f));
            Destroy(gameObject);
        }

        private void UpdateLabel()
        {
            if (_label == null) return;
            _label.text = "+" + _bonus;
            _label.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        }
    }
}
