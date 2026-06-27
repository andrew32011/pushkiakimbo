using UnityEngine;

namespace CrowdRunner
{
    // Рантайм-обёртка модели: инстанцирует под parent, масштабирует к нужной высоте,
    // ставит ногами на уровень родителя, опц. тонирует, выключает коллайдеры.
    public static class ModelUtil
    {
        public static GameObject Wrap(GameObject prefab, Transform parent, float targetHeight, Color? tint, out Renderer[] renderers)
        {
            renderers = System.Array.Empty<Renderer>();
            if (prefab == null || parent == null) return null;

            var model = Object.Instantiate(prefab, parent);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            model.transform.localScale = Vector3.one;

            var rends = model.GetComponentsInChildren<Renderer>();
            if (rends.Length > 0)
            {
                Bounds b = rends[0].bounds;
                for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
                float scale = targetHeight / Mathf.Max(0.01f, b.size.y);
                model.transform.localScale = Vector3.one * scale;

                // после масштаба — поставить ногами на уровень родителя
                Bounds b2 = rends[0].bounds;
                for (int i = 1; i < rends.Length; i++) b2.Encapsulate(rends[i].bounds);
                model.transform.position += new Vector3(0f, parent.position.y - b2.min.y, 0f);

                if (tint.HasValue)
                {
                    var mat = new Material(Shader.Find("Standard")) { color = tint.Value };
                    foreach (var r in rends) if (r != null) r.sharedMaterial = mat;
                }
            }

            foreach (var c in model.GetComponentsInChildren<Collider>()) c.enabled = false;
            renderers = rends;
            return model;
        }
    }
}
