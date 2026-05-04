using UnityEngine;

[CreateAssetMenu(fileName = "NewDebugEffect", menuName = "Rouge-like/Code for Buff/Debug Log")]
public class DebugEffect : UpgradeEffect
{
    public string message = "Buff Selected!";

    public override void Apply(GameObject player)
    {
        Debug.Log($"<color=cyan>[Effect]</color> {message} on {player.name}");
    }
}
