using UnityEngine;

namespace CrowdRunner
{
    // Базовая панель UI: общий корневой объект, показ/скрытие, доступ к менеджеру
    // и авто-обновление по изменению экономики. Конкретные панели переопределяют Refresh().
    public abstract class UIPanel : MonoBehaviour
    {
        [SerializeField] protected GameObject _root;

        protected static RunnerGameManager GM => RunnerGameManager.Instance;

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
            if (_root != null) _root.SetActive(visible);
            if (visible) Refresh();
        }

        // Обновление содержимого панели. По умолчанию ничего не делает.
        public virtual void Refresh() { }
    }
}
