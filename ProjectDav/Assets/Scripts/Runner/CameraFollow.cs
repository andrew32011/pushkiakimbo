using UnityEngine;

namespace CrowdRunner
{
    // Камера следует за отрядом сзади-сверху (портретный ракурс). Следит только по Z.
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform _target;     // отряд
        [SerializeField] private Vector3 _offset = new Vector3(0f, 7f, -7f);
        [SerializeField] private float _followLerp = 6f;
        [SerializeField] private float _pitch = 32f;

        private void LateUpdate()
        {
            if (_target == null) return;
            Vector3 desired = new Vector3(0f, _offset.y, _target.position.z + _offset.z);
            transform.position = Vector3.Lerp(transform.position, desired, _followLerp * Time.deltaTime);
            transform.rotation = Quaternion.Euler(_pitch, 0f, 0f);
        }

        public void SnapToTarget()
        {
            if (_target == null) return;
            transform.position = new Vector3(0f, _offset.y, _target.position.z + _offset.z);
            transform.rotation = Quaternion.Euler(_pitch, 0f, 0f);
        }

        public void SetTarget(Transform t) => _target = t;
    }
}
