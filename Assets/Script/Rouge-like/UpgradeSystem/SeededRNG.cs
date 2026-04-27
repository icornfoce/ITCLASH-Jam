using UnityEngine;

public class SeededRNG : MonoBehaviour
{
    public uint seed;
    private uint state;

    public void Init(uint initialSeed)
    {
        seed = initialSeed;
        state = seed;
    }

    // อัลกอริทึม Mulberry32 จำลองแบบฉบับ C#
    private uint Next()
    {
        state += 0x6D2B79F5;
        uint t = state;
        t = (t ^ (t >> 15)) * (t | 1);
        t ^= t + (t ^ (t >> 7)) * (t | 61);
        return t ^ (t >> 14);
    }

    public float NextFloat()
    {
        return (Next() / (float)uint.MaxValue);
    }

    public int NextInt(int min, int max)
    {
        if (min >= max) return min;
        return (int)(min + (NextFloat() * (max - min)));
    }
}
