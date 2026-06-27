using UnityEngine;

namespace CrowdRunner
{
    // Один стикмен отряда. Хранит точку вылета снаряда и опциональную модель оружия.
    public class UnitController : MonoBehaviour
    {
        [SerializeField] private Transform _muzzle;     // откуда летит снаряд
        [SerializeField] private Transform _weaponMount; // куда крепится модель оружия

        private GameObject _weaponModel;

        public Vector3 MuzzleWorld => _muzzle != null ? _muzzle.position : transform.position + Vector3.up * 0.5f;

        public void EquipWeapon(GameObject prefab)
        {
            if (_weaponModel != null) Destroy(_weaponModel);
            if (prefab == null || _weaponMount == null) return;
            _weaponModel = Instantiate(prefab, _weaponMount);
            _weaponModel.transform.localPosition = Vector3.zero;
            _weaponModel.transform.localRotation = Quaternion.identity;
            _weaponModel.transform.localScale = Vector3.one;
            // отключаем возможные коллайдеры на модели оружия
            foreach (var c in _weaponModel.GetComponentsInChildren<Collider>()) c.enabled = false;
        }
    }
}
