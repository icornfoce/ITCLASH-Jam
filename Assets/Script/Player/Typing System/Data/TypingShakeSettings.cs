using UnityEngine;

[CreateAssetMenu(fileName = "TypingShakeSettings", menuName = "Scriptable Objects/Typing Shake Settings")]
public class TypingShakeSettings : ScriptableObject
{
    public int wordLengthThreshold = 10;
    public float duration = 0.3f;
    public float magnitude = 0.2f;
}
