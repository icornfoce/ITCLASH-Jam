using UnityEngine;

public abstract class UpgradeEffect : ScriptableObject
{
    // ฟังก์ชันที่จะถูกเรียกใช้งานเมื่อเลือกบัพ
    // player: คือตัวละครที่ได้รับบัพนี้
    public abstract void Apply(GameObject player);
}
