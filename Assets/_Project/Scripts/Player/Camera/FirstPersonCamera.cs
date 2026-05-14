using UnityEngine;
using Unity.Cinemachine; 

public class FirstPersonCamera : MonoBehaviour
{
    [Header("Look Settings")]
    public Transform playerBody;
    public float mouseSensitivity = 100f;
    public float topClamp = 85f;
    public float bottomClamp = -85f;

    private float verticalRotation = 0f;
    private CinemachineOrbitalFollow orbitalFollow;
    private CinemachineCamera vcam;

    void Start()
    {
        vcam = GetComponent<CinemachineCamera>();
        if (vcam == null) vcam = GetComponentInParent<CinemachineCamera>();

        if (vcam != null)
        {
            // ปลดล็อคการ Aim ของ Cinemachine เพื่อให้ Script เราคุมเองได้ 100%
            // แต่ยังเหลือ Follow ไว้เพื่อให้กล้องเลื่อนตามตัวละครได้
            orbitalFollow = vcam.GetComponent<CinemachineOrbitalFollow>();
            
            // หมายเหตุ: การตั้ง LookAt เป็น null จะช่วยให้ Cinemachine ไม่ฝืนหมุนกล้อง
            vcam.LookAt = null; 
        }

        // เริ่มต้นค่าการหมุนจากมุมปัจจุบัน
        verticalRotation = transform.localEulerAngles.x;
        if (verticalRotation > 180) verticalRotation -= 360f;
    }

    void LateUpdate()
    {
        HandleLook();
    }

    private void HandleLook()
    {
        if (playerBody == null) return;

        if (Cursor.lockState != CursorLockMode.Locked)
        {
            if (Input.GetMouseButtonDown(0)) CursorUnlocker.ApplyLock();
            return;
        }

        // Get Mouse Input
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.unscaledDeltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.unscaledDeltaTime;

        // 1. หมุนตัวละคร (Yaw)
        playerBody.Rotate(Vector3.up * mouseX);

        // 2. คำนวณการก้มเงย (Pitch)
        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, bottomClamp, topClamp);

        // 3. [ nuclear option ] บังคับหมุนแบบ World Space 
        // เพื่อไม่ให้ Cinemachine หรือระบบอื่นมาเขียนทับ localRotation ของเราได้
        // การคูณแบบนี้คือ: หมุนตามตัวละคร (Yaw) แล้วค่อยก้มเงย (Pitch)
        transform.rotation = playerBody.rotation * Quaternion.Euler(verticalRotation, 0f, 0f);

        // 4. อัปเดตค่ากลับไปที่ Cinemachine (ถ้ามี) เพื่อให้ระบบ Zoom/Follow ยังทำงานได้
        if (orbitalFollow != null)
        {
            orbitalFollow.VerticalAxis.Value = verticalRotation;
        }
    }
}
