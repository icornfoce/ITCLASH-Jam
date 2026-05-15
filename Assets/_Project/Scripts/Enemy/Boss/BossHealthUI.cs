using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using ITCLASH.Enemies;

public class BossHealthUI : MonoBehaviour
{
    private static BossHealthUI _instance;
    public static BossHealthUI Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Object.FindFirstObjectByType<BossHealthUI>();
            }
            return _instance;
        }
    }

    [Header("UI Elements")]
    [SerializeField] private GameObject healthBarRoot;
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Slider easeHealthSlider;
    [SerializeField] private TextMeshProUGUI bossNameText;
    
    [Header("Settings")]
    [SerializeField] private float lerpSpeed = 5f;
    [SerializeField] private float showDelay = 0.5f;

    private MiniBoss activeBoss;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        if (healthBarRoot != null) healthBarRoot.SetActive(false);
    }

    public void Initialize(MiniBoss boss, string name)
    {
        activeBoss = boss;
        if (bossNameText != null) bossNameText.text = name;
        
        if (healthSlider != null)
        {
            healthSlider.maxValue = boss.maxHealth;
            healthSlider.value = boss.maxHealth;
        }
        
        if (easeHealthSlider != null)
        {
            easeHealthSlider.maxValue = boss.maxHealth;
            easeHealthSlider.value = boss.maxHealth;
        }

        StopAllCoroutines();
        StartCoroutine(ShowBarRoutine());
    }

    private IEnumerator ShowBarRoutine()
    {
        yield return new WaitForSeconds(showDelay);
        if (healthBarRoot != null) healthBarRoot.SetActive(true);
    }

    public void Hide()
    {
        if (healthBarRoot != null) healthBarRoot.SetActive(false);
    }

    private void Update()
    {
        if (activeBoss == null || healthSlider == null) return;

        // Update main slider
        healthSlider.value = activeBoss.GetCurrentHealth();

        // Smoothly lerp the ease slider
        if (easeHealthSlider != null)
        {
            if (Mathf.Abs(easeHealthSlider.value - healthSlider.value) > 0.01f)
            {
                easeHealthSlider.value = Mathf.Lerp(easeHealthSlider.value, healthSlider.value, Time.deltaTime * lerpSpeed);
            }
            else
            {
                easeHealthSlider.value = healthSlider.value;
            }
        }

        if (activeBoss.IsDead)
        {
            activeBoss = null;
            Hide();
        }
    }
}
