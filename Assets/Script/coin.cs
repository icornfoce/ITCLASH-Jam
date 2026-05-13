using UnityEngine;

public class coin : MonoBehaviour
{
    public int coinValue = 1;
    public GameObject gameObject;
    // Update is called once per frame
    public void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<Player>() != null)
        {
            PlayerCoin.playerCoin += coinValue;
            Destroy(gameObject);
        }
    }
}
