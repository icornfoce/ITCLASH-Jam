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
        public Color hitColor = Color.red;
        public Color healColor = Color.green;
        public float flashDuration = 0.15f;
        
        private EnemyController controller;
        private Renderer[] renderers;
        private Dictionary<Renderer, Color[]> originalColors = new Dictionary<Renderer, Color[]>();
        private Coroutine flashCoroutine;

        private void Awake()
        {
            controller = GetComponent<EnemyController>();
            
            // เก็บ Renderer และสีดั้งเดิมทั้งหมดไว้
            renderers = GetComponentsInChildren<Renderer>();
            foreach (Renderer r in renderers)
            {
                if (r is ParticleSystemRenderer) continue; 

                Color[] colors = new Color[r.materials.Length];
                for (int i = 0; i < r.materials.Length; i++)
                {
                    if (r.materials[i].HasProperty("_Color"))
                    {
                        colors[i] = r.materials[i].color;
                    }
                }
                originalColors[r] = colors;
            }
        }

        private void OnEnable()
        {
            if (controller != null)
            {
                // ลงทะเบียนรับเหตุการณ์ทั้งโดนดาเมจและได้รับฮีล
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

        private void HandleDamaged(float amount)
        {
            StartFlash(hitColor);
        }

        private void HandleHealed(float amount)
        {
            StartFlash(healColor);
        }

        private void StartFlash(Color color)
        {
            if (!gameObject.activeInHierarchy) return;

            if (flashCoroutine != null) StopCoroutine(flashCoroutine);
            flashCoroutine = StartCoroutine(FlashRoutine(color));
        }

        private IEnumerator FlashRoutine(Color color)
        {
            // 1. เปลี่ยนเป็นสีเป้าหมาย
            foreach (Renderer r in renderers)
            {
                if (r == null) continue;
                for (int i = 0; i < r.materials.Length; i++)
                {
                    if (r.materials[i].HasProperty("_Color"))
                    {
                        r.materials[i].color = color;
                    }
                }
            }

            yield return new WaitForSeconds(flashDuration);

            // 2. คืนค่าสีดั้งเดิม
            foreach (Renderer r in renderers)
            {
                if (r == null || !originalColors.ContainsKey(r)) continue;
                Color[] orig = originalColors[r];
                for (int i = 0; i < r.materials.Length; i++)
                {
                    if (i < orig.Length && r.materials[i].HasProperty("_Color"))
                    {
                        r.materials[i].color = orig[i];
                    }
                }
            }
            
            flashCoroutine = null;
        }
    }
}
