using UnityEngine;

public class SlipperySurface : MonoBehaviour
{
    [Header("Anti-Climb / Slippery Settings")]
    [Tooltip("ความแรงในการผลักผู้เล่นให้ตกลงมา")]
    public float pushOffForce = 15f;

    private void OnTriggerStay(Collider other)
    {
        // ตรวจสอบว่าสิ่งที่เหยียบอยู่คือผู้เล่นหรือไม่
        CharacterController playerController = other.GetComponent<CharacterController>();
        if (playerController != null)
        {
            // หาจุดศูนย์กลางของวัตถุนี้
            Vector3 center = transform.position;
            
            // หาว่าผู้เล่นอยู่ฝั่งไหนของวัตถุ เพื่อผลักออกไปด้านนั้น
            Vector3 pushDirection = other.transform.position - center;
            pushDirection.y = 0; // ล็อกแกน Y ไว้ จะได้ผลักออกด้านข้าง
            
            // กรณีที่ผู้เล่นยืนอยู่ตรงกลางจุดศูนย์กลางเป๊ะๆ (เดี๋ยวจะผลักไม่ออก)
            if (pushDirection.sqrMagnitude < 0.01f)
            {
                pushDirection = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f));
            }

            // เพิ่มแรงดึงลงด้านล่าง เพื่อให้ร่วงลงพื้นไวๆ
            pushDirection.y = -2f;

            // ออกแรงผลักผู้เล่น
            playerController.Move(pushDirection.normalized * pushOffForce * Time.deltaTime);
        }
    }
}
