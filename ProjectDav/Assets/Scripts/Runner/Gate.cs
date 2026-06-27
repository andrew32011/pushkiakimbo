using UnityEngine;

namespace CrowdRunner
{
    // Ворота-модификатор числа юнитов (главная механика). Ставятся парами.
    public class Gate : MonoBehaviour
    {
        [SerializeField] private TextMesh _label;
        [SerializeField] private Renderer _background;

        private GateOp _op;
        private int _value;
        private bool _isWeaponPickup;
        private Gate _pair;
        private bool _consumed;

        public void Init(GateOp op, int value, Color color)
        {
            _op = op; _value = value; _isWeaponPickup = false; _consumed = false;
            if (_background != null && _background.material != null) _background.material.color = color;
            if (_label != null) { _label.text = LabelText(); _label.transform.rotation = Quaternion.Euler(0f, 180f, 0f); }
        }

        // Ворота-апгрейд оружия (золотое оружие).
        public void InitWeaponPickup(Color color)
        {
            _isWeaponPickup = true; _consumed = false;
            if (_background != null && _background.material != null) _background.material.color = color;
            if (_label != null) { _label.text = "ОРУЖИЕ"; _label.transform.rotation = Quaternion.Euler(0f, 180f, 0f); }
        }

        public void SetPair(Gate pair) => _pair = pair;

        private string LabelText()
        {
            switch (_op)
            {
                case GateOp.Add: return "+" + _value;
                case GateOp.Multiply: return "x" + _value;
                case GateOp.Subtract: return "-" + _value;
                case GateOp.Divide: return "÷" + _value;
            }
            return _value.ToString();
        }

        public void ApplyTo(SquadController squad)
        {
            if (_consumed) return;
            _consumed = true;
            if (_pair != null) _pair.Consume();

            if (_isWeaponPickup) squad.UpgradeWeapon();
            else squad.ModifyUnits(_op, _value);

            AudioController.Instance?.PlayGate();
            EffectsManager.Burst(transform.position + Vector3.up * 1.2f, new Color(0.6f, 0.9f, 1f), 1.5f);
            gameObject.SetActive(false);
        }

        public void Consume()
        {
            _consumed = true;
            gameObject.SetActive(false);
        }
    }
}
