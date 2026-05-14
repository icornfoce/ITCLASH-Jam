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

    /// <summary>
    /// Helper static method to lock cursor and keep state in sync
    /// </summary>
    public static void ApplyLock()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        IsLocked = true;
    }

    /// <summary>
    /// Helper static method to unlock cursor and keep state in sync
    /// </summary>
    public static void ApplyUnlock()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        IsLocked = false;
    }

    private void Start()
    {
        if (lockOnStart)
        {
            ApplyLock();
        }
    }

    private void Update()
    {
        // 1. เมื่อกด Alt (ซ้ายหรือขวา) ค้างไว้ -> ปลดล็อคเมาส์
        if (Input.GetKeyDown(KeyCode.LeftAlt) || Input.GetKeyDown(KeyCode.RightAlt))
        {
            ApplyUnlock();
        }
        
        // 2. เมื่อปล่อยปุ่ม Alt -> กลับไปล็อคเมาส์เหมือนเดิม
        if (Input.GetKeyUp(KeyCode.LeftAlt) || Input.GetKeyUp(KeyCode.RightAlt))
        {
            ApplyLock();
        }

        // 3. เมื่อคลิกที่หน้าจอเกม และเมาส์ยังไม่ล็อค -> ให้ล็อคกลับมา (กันพลาดตอน Alt-Tab)
        if (Input.GetMouseButtonDown(0) && Cursor.lockState != CursorLockMode.Locked)
        {
            // เช็คว่าไม่ได้คลิกโดน UI (Optional: ถ้ามีระบบ UI เยอะๆ อาจต้องเช็ค EventSystem)
            ApplyLock();
        }
    }

    public void LockCursor()
    {
        ApplyLock();
    }

    public void UnlockCursor()
    {
        ApplyUnlock();
    }
}
