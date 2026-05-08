namespace ITCLASH.Enemies
{
    /// <summary>
    /// Interface สำหรับ Object ที่รับแรง Knockback ได้
    /// EnemyController implement ไว้แล้ว
    /// </summary>
    public interface IKnockbackable
    {
        /// <summary>
        /// ดัน Object ด้วยแรง force (World Space) เป็นเวลา duration วินาที
        /// </summary>
        void ApplyKnockback(UnityEngine.Vector3 force, float duration = 0.35f);
    }
}
