using UnityEngine;

namespace CrowdRunner
{
    // Ворота на полосе. Левая полоса — оружие/урон, правая — пополнение отряда.
    public class Gate : MonoBehaviour
    {
        [SerializeField] private TextMesh _label;
        [SerializeField] private Renderer _background;

        private GateType _type;
        private GateOp _op;
        private int _value;
        private bool _isWeaponSwap;
        private WeaponType _weapon;
        private bool _consumed;

        public void Init(GateType type, GateOp op, int value, bool weaponSwap, WeaponType weapon, Color color)
        {
            _type = type;
            _op = op;
            _value = value;
            _isWeaponSwap = weaponSwap;
            _weapon = weapon;
            _consumed = false;

            if (_background != null && _background.material != null) _background.material.color = color;
            if (_label != null) _label.text = LabelText();
        }

        private string LabelText()
        {
            if (_isWeaponSwap) return _weapon.ToString().ToUpper();
            string sign = _op == GateOp.Multiply ? "x" : (_op == GateOp.Subtract ? "-" : "+");
            return sign + _value;
        }

        // Вызывается сенсором отряда.
        public void ApplyTo(SquadController squad)
        {
            if (_consumed) return;
            _consumed = true;

            if (_isWeaponSwap)
            {
                squad.SetWeapon(_weapon);
            }
            else if (_type == GateType.Weapon)
            {
                squad.ModifyDamage(_op, _value);
            }
            else // Units
            {
                squad.ModifyUnits(_op, _value);
            }

            AudioController.Instance?.PlayGate();
            gameObject.SetActive(false);
        }
    }
}
