using UnityEngine;
using UnityEngine.UI;

namespace CrowdRunner
{
    public class VictoryUI : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private Text _resultText;

        public void Show(bool v) { if (_root != null) _root.SetActive(v); }

        public void Set(int survivors, int coins)
        {
            if (_resultText != null) _resultText.text = $"Уровень пройден!\nВыжило: {survivors}\nНаграда: {coins}";
        }

        public void OnNext() => RunnerGameManager.Instance?.NextLevel();
        public void OnDouble() => RunnerGameManager.Instance?.DoubleReward();
        public void OnMenu() => RunnerGameManager.Instance?.ShowMenu();
    }
}
