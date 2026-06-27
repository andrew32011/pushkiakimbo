using UnityEngine;
using UnityEngine.UI;

namespace CrowdRunner
{
    public class WinUI : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private Text _resultText;

        public void Show(bool v) { if (_root != null) _root.SetActive(v); }

        public void Set(int coins, int kills)
        {
            if (_resultText != null) _resultText.text = $"Эпоха пройдена!\nНаграда: {coins}\nУбито: {kills}";
        }

        // ---- Кнопки ----
        public void OnDouble() => RunnerGameManager.Instance?.DoubleReward();
        public void OnMenu() => RunnerGameManager.Instance?.ShowMenu();
        public void OnNext() => RunnerGameManager.Instance?.ShowMenu(); // выбор эпохи в меню
    }
}
