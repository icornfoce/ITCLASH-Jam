using UnityEngine;

/// <summary>
/// แปะ Script นี้ไว้ที่ GameObject ที่ต้องการกำหนดคำเฉพาะเจาะจง
/// (ถ้าไม่มี Script นี้ ระบบจะเอาชื่อ GameObject ไปแปลงเป็นคำแทน)
/// </summary>
public class ScanLabel : MonoBehaviour
{
    [Tooltip("คำที่ต้องพิมพ์เพื่อสแกนวัตถุนี้ (ใช้ตัวพิมพ์เล็ก)")]
    public string word;
}
