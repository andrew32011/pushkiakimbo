using UnityEngine;
using UnityEngine.UI;

namespace CrowdRunner
{
    public class VictoryUI : UIPanel
    {
        [SerializeField] private Text _resultText;
        [SerializeField] private GameObject _doubleButton;  // x2 за рекламу — пока доступно

        public void Set(int survivors, int coins, bool canDouble)
        {
            if (_resultText != null) _resultText.text = $"Уровень пройден!\nВыжило: {survivors}\nНаграда: {coins}";
            if (_doubleButton != null) _doubleButton.SetActive(canDouble);
        }

        public void OnNext() => GM?.NextLevel();
        public void OnDouble() => GM?.DoubleReward();
        public void OnMenu() => GM?.ShowMenu();
    }
}
