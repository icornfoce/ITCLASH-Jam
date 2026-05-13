using UnityEngine;

public class loopScript : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        int ans = 0;
        for (int i = 1; i <= 10000; i++)
        {
            ans += i;
            if (i == 10000)
            {
                Debug.Log(ans);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {

    }
}
