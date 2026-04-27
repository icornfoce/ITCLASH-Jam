using UnityEngine;

[CreateAssetMenu(fileName = "NewChangeModelEffect", menuName = "Rouge-like/Code for Buff/Change Model")]
public class ChangeModelEffect : UpgradeEffect
{
    [Tooltip("Prefab ของ Model ใหม่ที่ต้องการเปลี่ยน (ควรมี Animator ติดอยู่ด้วย)")]
    public GameObject newModelPrefab;

    public override void Apply(GameObject player)
    {
        if (newModelPrefab == null)
        {
            Debug.LogWarning("[ChangeModelEffect] New Model Prefab is missing!");
            return;
        }

        // 1. ค้นหา PlayerController เพื่ออัปเดตการอ้างอิง Animator
        PlayerController pc = player.GetComponent<PlayerController>();
        
        // 2. กำจัด Model เก่าออกไป
        if (pc != null && pc.animator != null)
        {
            GameObject oldModel = pc.animator.gameObject;

            // --- ป้องกันกล้องหาย ---
            // ค้นหากล้อง หรือ Cinemachine ที่อาจจะติดอยู่กับ Model เก่า
            // และย้ายมันกลับมาที่ตัว Player (ตัวแม่) ก่อนจะลบ Model ทิ้ง
            Camera[] cameras = oldModel.GetComponentsInChildren<Camera>();
            foreach (Camera cam in cameras)
            {
                cam.transform.SetParent(player.transform);
            }

            // สำหรับ Cinemachine (ถ้ามี)
            var vCams = oldModel.GetComponentsInChildren<Unity.Cinemachine.CinemachineCamera>();
            foreach (var vcam in vCams)
            {
                vcam.transform.SetParent(player.transform);
            }

            Destroy(oldModel);
        }
        else
        {
            // กรณีที่ไม่มี Animator ในสคริปต์ ให้พยายามหาลูกตัวแรกที่อาจจะเป็น Model
            if (player.transform.childCount > 0)
            {
                // ตรวจสอบเบื้องต้น (ระวังอย่าไปลบพวก Camera หรือ Particle อื่นๆ)
                // ในที่นี้เราจะลบเฉพาะตัวที่น่าจะเป็น Model จริงๆ
                // แต่เพื่อความปลอดภัยที่สุด การลบผ่าน pc.animator ดีที่สุดครับ
            }
        }

        // 3. สร้าง Model ใหม่เข้าไปเป็นลูกของ Player
        GameObject newModel = Instantiate(newModelPrefab, player.transform);
        
        // รีเซ็ตตำแหน่งให้อยู่ตรงกลาง Player
        newModel.transform.localPosition = Vector3.zero;
        newModel.transform.localRotation = Quaternion.identity;

        // 4. อัปเดต Animator ใน PlayerController เพื่อให้ยังควบคุมท่าทางได้ปกติ
        if (pc != null)
        {
            pc.animator = newModel.GetComponent<Animator>();
            
            // หาก Animator อยู่ในลูกตัวถัดลงไปอีกชั้น ให้ใช้ GetComponentInChildren
            if (pc.animator == null)
            {
                pc.animator = newModel.GetComponentInChildren<Animator>();
            }

            Debug.Log($"<color=cyan>[Effect]</color> Successfully changed model to: {newModelPrefab.name}");
        }
    }
}
