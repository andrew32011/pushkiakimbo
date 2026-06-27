using UnityEngine;

namespace CrowdRunner
{
    // Ввод: перетаскивание пальцем/мышью по горизонтали -> смещение отряда по X.
    // Накапливает дельту между кадрами, отряд её "потребляет".
    public class RunnerInput : MonoBehaviour
    {
        [SerializeField] private float _sensitivity = 0.02f; // мир. единиц на пиксель (выше = чувствительнее драг)

        public bool Locked { get; set; } = true;

        private bool _dragging;
        private float _lastX;
        private float _accumDeltaX;

        private void Update()
        {
            if (Locked) { _dragging = false; _accumDeltaX = 0f; return; }

            if (Input.GetMouseButtonDown(0))
            {
                _dragging = true;
                _lastX = Input.mousePosition.x;
            }
            else if (Input.GetMouseButton(0) && _dragging)
            {
                float cur = Input.mousePosition.x;
                _accumDeltaX += (cur - _lastX) * _sensitivity;
                _lastX = cur;
            }
            else if (Input.GetMouseButtonUp(0))
            {
                _dragging = false;
            }
        }

        // Отряд забирает накопленное смещение.
        public float ConsumeDeltaX()
        {
            float d = _accumDeltaX;
            _accumDeltaX = 0f;
            return d;
        }
    }
}
