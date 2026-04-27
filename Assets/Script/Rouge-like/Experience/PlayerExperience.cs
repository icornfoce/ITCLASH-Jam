using System;
using UnityEngine;

public class PlayerExperience : MonoBehaviour
{
    public static PlayerExperience Instance { get; private set; }

    [Header("Experience Settings")]
    [Tooltip("จุดหมายที่ Gem จะลอยไปหา (เช่น Empty Object) หากไม่ได้ใส่ไว้ จะลอยเข้าหาวัตถุที่ใช้สคริปต์นี้แทน")]
    public Transform gemCollectorPoint;
    public int currentLevel = 1;
    public float currentExp = 0f;
    public float baseExpNeed = 100f; // EXP ของเลเวล 1
    public float expMultiplierPerLevel = 1.25f; // เลเวลต่อไปใช้ EXP มากขึ้น 25%

    public float maxExpForNextLevel;

    [Header("Growth Settings")]
    [Tooltip("ตัวคูณ Exp เช่น โบนัส 10% ให้ใส่ 10")]
    public float expGrowthRate = 0f; 

    public Action<int> OnLevelUp;
    public Action<float, float> OnExpChanged; // CurrentEXP, MaxEXP

    [HideInInspector] 
    public int queuedLevelUps = 0; // คิวสำหรับ Level Up ที่รอผู้เล่นกดบัฟ

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
        
        CalculateNextLevelExp();
    }

    private void Start()
    {
        OnExpChanged?.Invoke(currentExp, maxExpForNextLevel);
    }

    public void AddExperience(float gemBaseValue)
    {
        float expReceived = gemBaseValue * (1f + (expGrowthRate / 100f));
        currentExp += expReceived;

        CheckLevelUp();

        OnExpChanged?.Invoke(currentExp, maxExpForNextLevel);
    }

    private void CheckLevelUp()
    {
        while (currentExp >= maxExpForNextLevel)
        {
            currentExp -= maxExpForNextLevel;
            currentLevel++;
            
            CalculateNextLevelExp();
            
            // ข้ามการสุ่มบัฟให้ที่เลเวลเริ่มต้น (เพราะ 1 ไป 2 ถึงจะได้บัฟแรก)
            if (currentLevel > 1)
            {
                queuedLevelUps++;
            }
            
            OnLevelUp?.Invoke(currentLevel);
            Debug.Log($"Level Up! Now Level {currentLevel}");
        }

        ProcessLevelUpQueue();
    }

    // ฟังก์ชันจัดการดึงคิวพาเนลออกมาโชว์
    public void ProcessLevelUpQueue()
    {
        // หากมีพาเนลกำลังใช้งานอยู่ (ผู้เล่นกำลังเลือกบัฟ) ให้หยุดกระบวนการนี้ รอจนกว่า UI จะปิดลง
        if (LevelUpManager.Instance != null && LevelUpManager.Instance.IsPanelActive())
        {
            return;
        }

        if (queuedLevelUps > 0)
        {
            queuedLevelUps--;
            if (LevelUpManager.Instance != null)
            {
                LevelUpManager.Instance.ShowLevelUpPanel();
            }
            else
            {
                Debug.LogWarning("[PlayerExperience] ไม่พบ LevelUpManager ในฉาก ระบบจะเคลียร์คิวทิ้งอัตโนมัติ");
                ProcessLevelUpQueue();
            }
        }
    }

    private void CalculateNextLevelExp()
    {
        // สูตรคำนวณ EXP ของเลเวลถัดไป = BaseEXP * (Multiplier ^ (CurrentLevel - 1))
        maxExpForNextLevel = baseExpNeed * Mathf.Pow(expMultiplierPerLevel, currentLevel - 1);
    }

    // ฟังก์ชันไว้เรียกให้ Growth Rate เพิ่มขึ้นเวลากินไอเทมหรืออัพสกิล
    public void AddGrowthRate(float percent)
    {
        expGrowthRate += percent;
    }
}
