using UnityEngine;

namespace CrowdRunner
{
    // Камера привязана к центру толпы. Скользит за ней с задержкой (плавный lerp) и
    // повторяет горизонтальный сдвиг отряда ОСЛАБЛЕННО — так игрок видит, что отряд
    // дошёл до крайней дорожки, но камера не дёргается на всю ширину.
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform _target;     // центр отряда
        [SerializeField] private Vector3 _offset = new Vector3(0f, 12f, -13f);
        [SerializeField] private float _followLerp = 4f;        // меньше — больше задержка/скольжение
        [SerializeField] private float _horizontalFactor = 0.5f; // насколько слабее камера повторяет сдвиг по X
        [SerializeField] private float _pitch = 38f;

        private void LateUpdate()
        {
            if (_target == null) return;
            transform.position = Vector3.Lerp(transform.position, Desired(), _followLerp * Time.deltaTime);
            transform.rotation = Quaternion.Euler(_pitch, 0f, 0f);
        }

        private Vector3 Desired()
        {
            return new Vector3(
                _target.position.x * _horizontalFactor + _offset.x,
                _offset.y,
                _target.position.z + _offset.z);
        }

        public void SnapToTarget()
        {
            if (_target == null) return;
            transform.position = Desired();
            transform.rotation = Quaternion.Euler(_pitch, 0f, 0f);
        }

        public void SetTarget(Transform t) => _target = t;
    }
}
