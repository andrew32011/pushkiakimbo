using System.Collections;
using UnityEngine;

namespace CrowdRunner
{
    // Базовая панель UI: корень, показ/скрытие с анимацией выезжания окна, доступ к менеджеру
    // и авто-обновление по изменению экономики. Конкретные панели переопределяют Refresh().
    public abstract class UIPanel : MonoBehaviour
    {
        [SerializeField] protected GameObject _root;
        [SerializeField] protected RectTransform _slideRect; // окно, которое выезжает (если задано)
        [SerializeField] protected float _slideFromX = -2400f; // стартовая позиция за экраном
        [SerializeField] protected float _slideSpeed = 14f;

        protected static RunnerGameManager GM => RunnerGameManager.Instance;

        private Vector2 _shownPos;
        private bool _posCaptured;
        private Coroutine _co;

        protected virtual void Awake()
        {
            if (_slideRect != null) { _shownPos = _slideRect.anchoredPosition; _posCaptured = true; }
        }

        protected virtual void OnEnable()
        {
            var gm = GM;
            if (gm != null) gm.OnEconomyChanged += Refresh;
        }

        protected virtual void OnDisable()
        {
            var gm = GM;
            if (gm != null) gm.OnEconomyChanged -= Refresh;
        }

        public virtual void Show(bool visible)
        {
            if (visible)
            {
                if (_root != null) _root.SetActive(true);
                Refresh();
                if (_slideRect != null && gameObject.activeInHierarchy)
                {
                    if (!_posCaptured) { _shownPos = _slideRect.anchoredPosition; _posCaptured = true; }
                    if (_co != null) StopCoroutine(_co);
                    _co = StartCoroutine(Slide(_shownPos, false));
                }
            }
            else
            {
                if (_slideRect != null && _root != null && _root.activeInHierarchy && gameObject.activeInHierarchy)
                {
                    if (_co != null) StopCoroutine(_co);
                    _co = StartCoroutine(Slide(new Vector2(_slideFromX, _shownPos.y), true));
                }
                else if (_root != null) _root.SetActive(false);
            }
        }

        private IEnumerator Slide(Vector2 target, bool hideAtEnd)
        {
            if (!hideAtEnd) _slideRect.anchoredPosition = new Vector2(_slideFromX, _shownPos.y);
            while ((_slideRect.anchoredPosition - target).sqrMagnitude > 4f)
            {
                _slideRect.anchoredPosition = Vector2.Lerp(_slideRect.anchoredPosition, target, _slideSpeed * Time.unscaledDeltaTime);
                yield return null;
            }
            _slideRect.anchoredPosition = target;
            if (hideAtEnd && _root != null) _root.SetActive(false);
            _co = null;
        }

        // Обновление содержимого панели. По умолчанию ничего не делает.
        public virtual void Refresh() { }

        // Закрытие оверлея (снимает паузу) и возврат в главное меню. Переопределяемы при нужде.
        public virtual void OnClose() { Show(false); GM?.CloseOverlay(); }
        public virtual void OnMenu() { Show(false); GM?.ShowMenu(); }
    }
}
