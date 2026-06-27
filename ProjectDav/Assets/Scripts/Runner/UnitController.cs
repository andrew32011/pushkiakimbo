using UnityEngine;

namespace CrowdRunner
{
    // Один стикмен отряда. Точка вылета снаряда + крепление модели оружия.
    public class UnitController : MonoBehaviour
    {
        [SerializeField] private Transform _muzzle;
        [SerializeField] private Transform _weaponMount;
        [SerializeField] private Transform _modelRoot;   // держатель тела (для подмены из пака)
        [SerializeField] private float _modelHeight = 1.6f;

        private GameObject _weaponModel;
        private GameObject _weaponSource;

        // Тело юнита из пака (если задано) подменяет запечённое; null — оставляем запечённое.
        public void SetBodyModel(GameObject modelPrefab)
        {
            if (modelPrefab == null || _modelRoot == null) return;
            for (int i = _modelRoot.childCount - 1; i >= 0; i--) Destroy(_modelRoot.GetChild(i).gameObject);
            ModelUtil.Wrap(modelPrefab, _modelRoot, _modelHeight, new Color(0.3f, 0.6f, 1f), out _);
        }

        public Vector3 MuzzleWorld => _muzzle != null ? _muzzle.position : transform.position + Vector3.up * 0.6f;

        public void EquipWeapon(GameObject prefab)
        {
            if (prefab == _weaponSource) return; // уже надето
            _weaponSource = prefab;
            if (_weaponModel != null) Destroy(_weaponModel);
            if (prefab == null || _weaponMount == null) return;

            _weaponModel = Instantiate(prefab, _weaponMount);
            // нормализуем размер (масштаб ассетов неизвестен)
            var rends = _weaponModel.GetComponentsInChildren<Renderer>();
            if (rends.Length > 0)
            {
                Bounds b = rends[0].bounds;
                for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
                float maxDim = Mathf.Max(0.001f, Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z)));
                float scale = 0.6f / maxDim;
                _weaponModel.transform.localScale = Vector3.one * scale;
            }
            _weaponModel.transform.localPosition = Vector3.zero;
            _weaponModel.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            foreach (var c in _weaponModel.GetComponentsInChildren<Collider>()) c.enabled = false;
        }
    }
}
