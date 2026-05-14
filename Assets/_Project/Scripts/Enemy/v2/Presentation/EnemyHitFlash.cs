using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace ITCLASH.Enemies
{
    /// <summary>
    /// สคริปต์เสริมสำหรับทำให้ศัตรูกระพริบเมื่อโดนดาเมจ
    /// - รองรับการโดนดาเมจทุกรูปแบบ (รวมถึงจาก Dev Panel)
    /// - ค้นหา Renderer ทั้งหมดในตัวมอนสเตอร์ให้อัตโนมัติ
    /// </summary>
    public class EnemyHitFlash : MonoBehaviour
    {
        [Header("Colors")]
        public Color hitColor = Color.white;
        public Color healColor = Color.green;
        public float flashDuration = 0.1f;
        
        private EnemyController controller;
        private struct MaterialData
        {
            public Color originalColor;
            public Color originalEmission;
            public bool hadEmission;
            public bool hasColorProp;
            public string colorPropName;
        }
        private Dictionary<Material, MaterialData> originalMatData = new Dictionary<Material, MaterialData>();
        private Coroutine flashCoroutine;

        private void Awake()
        {
            controller = GetComponent<EnemyController>();
            CacheOriginalMaterials();
        }

        private void CacheOriginalMaterials()
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            originalMatData.Clear();

            foreach (Renderer r in renderers)
            {
                if (r is ParticleSystemRenderer || r == null) continue;

                foreach (Material mat in r.materials)
                {
                    if (mat == null || originalMatData.ContainsKey(mat)) continue;

                    MaterialData data = new MaterialData();
                    
                    // เช็ค Property ชื่อสี
                    if (mat.HasProperty("_BaseColor")) { data.hasColorProp = true; data.colorPropName = "_BaseColor"; }
                    else if (mat.HasProperty("_Color")) { data.hasColorProp = true; data.colorPropName = "_Color"; }

                    if (data.hasColorProp) data.originalColor = mat.GetColor(data.colorPropName);

                    // เก็บค่า Emission
                    if (mat.HasProperty("_EmissionColor"))
                    {
                        data.originalEmission = mat.GetColor("_EmissionColor");
                        data.hadEmission = mat.IsKeywordEnabled("_EMISSION");
                    }

                    originalMatData[mat] = data;
                }
            }
        }

        private void OnEnable()
        {
            if (controller != null)
            {
                controller.OnDamaged.AddListener(HandleDamaged);
                controller.OnHealed.AddListener(HandleHealed);
            }
        }

        private void OnDisable()
        {
            if (controller != null)
            {
                controller.OnDamaged.RemoveListener(HandleDamaged);
                controller.OnHealed.RemoveListener(HandleHealed);
            }
        }

        private void HandleDamaged(float amount) { StartFlash(hitColor); }
        private void HandleHealed(float amount) { StartFlash(healColor); }

        private void StartFlash(Color color)
        {
            if (!gameObject.activeInHierarchy) return;
            if (flashCoroutine != null) StopCoroutine(flashCoroutine);
            flashCoroutine = StartCoroutine(FlashRoutine(color));
        }

        private IEnumerator FlashRoutine(Color color)
        {
            foreach (var entry in originalMatData)
            {
                Material mat = entry.Key;
                if (mat == null) continue;

                if (entry.Value.hasColorProp) mat.SetColor(entry.Value.colorPropName, color);

                if (mat.HasProperty("_EmissionColor"))
                {
                    mat.SetColor("_EmissionColor", color * 2.5f); 
                    mat.EnableKeyword("_EMISSION");
                }
            }

            yield return new WaitForSeconds(flashDuration);

            foreach (var entry in originalMatData)
            {
                Material mat = entry.Key;
                MaterialData data = entry.Value;
                if (mat == null) continue;

                if (data.hasColorProp) mat.SetColor(data.colorPropName, data.originalColor);

                if (mat.HasProperty("_EmissionColor"))
                {
                    mat.SetColor("_EmissionColor", data.originalEmission);
                    if (!data.hadEmission) mat.DisableKeyword("_EMISSION");
                }
            }
            flashCoroutine = null;
        }
    }
}
