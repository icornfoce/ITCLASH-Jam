using UnityEngine;

/// <summary>
/// CursorUnlocker — สคริปต์แยกสำหรับคุมเมาส์โดยเฉพาะ
/// </summary>
public class CursorUnlocker : MonoBehaviour
{
    [Header("Settings")]
    public bool lockOnStart = true;

    // ตัวแปร Static ที่สคริปต์อื่น (เช่น กล้อง) สามารถเข้ามาเช็คได้
    public static bool IsLocked { get; private set; }

    private void Start()
    {
        if (lockOnStart)
        {
            LockCursor();
        }
    }

    private void Update()
    {
        // 1. เมื่อกด Alt (ซ้ายหรือขวา) ค้างไว้ -> ปลดล็อคเมาส์
        if (Input.GetKeyDown(KeyCode.LeftAlt) || Input.GetKeyDown(KeyCode.RightAlt))
        {
            UnlockCursor();
        }
        
        // 2. เมื่อปล่อยปุ่ม Alt -> กลับไปล็อคเมาส์เหมือนเดิม
        if (Input.GetKeyUp(KeyCode.LeftAlt) || Input.GetKeyUp(KeyCode.RightAlt))
        {
            LockCursor();
        }
    }

    public void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        IsLocked = true;
    }

    public void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        IsLocked = false;
    }
}
