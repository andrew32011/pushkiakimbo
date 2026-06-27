using UnityEngine;
using UnityEngine.UI;

namespace CrowdRunner
{
    public class UpgradeUI : UIPanel
    {
        [SerializeField] private Text _coinsText;
        [SerializeField] private Text[] _levelTexts = new Text[4]; // по UpgradeType
        [SerializeField] private Text[] _costTexts = new Text[4];

        public override void Refresh()
        {
            var gm = GM;
            if (gm == null) return;
            if (_coinsText != null) _coinsText.text = gm.Coins.ToString();
            for (int i = 0; i < 4; i++)
            {
                var t = (UpgradeType)i;
                if (_levelTexts != null && i < _levelTexts.Length && _levelTexts[i] != null)
                    _levelTexts[i].text = "Ур. " + gm.GetUpgradeLevel(t);
                if (_costTexts != null && i < _costTexts.Length && _costTexts[i] != null)
                    _costTexts[i].text = gm.IsUpgradeMax(t) ? "MAX" : gm.GetUpgradeCost(t).ToString();
            }
        }

        private void Buy(UpgradeType t)
        {
            if (GM != null && GM.TryBuyUpgrade(t)) Refresh();
        }

        // ---- Кнопки ----
        public void OnBuyDamage() => Buy(UpgradeType.Damage);
        public void OnBuyStartUnits() => Buy(UpgradeType.StartUnits);
        public void OnBuyFireRate() => Buy(UpgradeType.FireRate);
        public void OnBuyVolley() => Buy(UpgradeType.Volley);
        public void OnFreeUpgrade() { GM?.GrantFreeUpgrade(); Refresh(); }
        public void OnClose() { Show(false); GM?.CloseOverlay(); }
    }
}
