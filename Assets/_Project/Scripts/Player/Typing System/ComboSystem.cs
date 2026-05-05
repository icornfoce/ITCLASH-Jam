using UnityEngine;
using TMPro;
using System.Collections;

public class ComboSystem : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float comboMaxTime = 10f;
    [SerializeField] private float fadeSpeed = 5f;
    [SerializeField] private Vector3 popScale = new Vector3(1.2f, 1.2f, 1.2f);
    
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI comboText;
    [SerializeField] private UnityEngine.UI.Slider comboTimerSlider;
    [SerializeField] private CanvasGroup canvasGroup;

    public int CurrentCombo { get; private set; }
    private float comboTimer;
    private Vector3 originalScale;
    private Coroutine popCoroutine;

    private void Awake()
    {
        if (comboText != null)
            originalScale = comboText.transform.localScale;
            
        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
            
        ResetCombo();
    }

    private void Update()
    {
        if (CurrentCombo > 0)
        {
            comboTimer -= Time.deltaTime;
            
            // Optional: Update color or shake as timer runs out
            if (comboTimer <= 0)
            {
                ResetCombo();
            }
            
            // Smooth fade in
            if (canvasGroup != null)
                canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, 1f, Time.deltaTime * fadeSpeed);
                
            if (comboTimerSlider != null)
            {
                comboTimerSlider.maxValue = comboMaxTime;
                comboTimerSlider.value = comboTimer;
            }
        }
        else
        {
            // Smooth fade out
            if (canvasGroup != null)
                canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, 0f, Time.deltaTime * fadeSpeed);
        }
    }

    public void AddCombo()
    {
        CurrentCombo++;
        comboTimer = comboMaxTime;
        if (comboTimerSlider != null) comboTimerSlider.value = comboMaxTime;
        Debug.Log($"<color=yellow>[ComboSystem] Combo Added! Current: {CurrentCombo}</color>");
        
        UpdateUI();
        TriggerPopEffect();
    }

    public void ResetCombo()
    {
        if (CurrentCombo > 0) Debug.Log("<color=red>[ComboSystem] Combo Reset!</color>");
        CurrentCombo = 0;
        comboTimer = 0;
        if (comboTimerSlider != null) comboTimerSlider.value = 0;
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (comboText != null)
        {
            comboText.text = CurrentCombo > 0 ? $"COMBO x{CurrentCombo}" : "";
        }
    }

    private void TriggerPopEffect()
    {
        if (comboText == null) return;
        
        if (popCoroutine != null) StopCoroutine(popCoroutine);
        popCoroutine = StartCoroutine(PopSequence());
    }

    private IEnumerator PopSequence()
    {
        float duration = 0.15f;
        float elapsed = 0f;
        
        // Scale Up
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            comboText.transform.localScale = Vector3.Lerp(originalScale, popScale, elapsed / duration);
            yield return null;
        }
        
        elapsed = 0f;
        // Scale Down
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            comboText.transform.localScale = Vector3.Lerp(popScale, originalScale, elapsed / duration);
            yield return null;
        }
        
        comboText.transform.localScale = originalScale;
        popCoroutine = null;
    }
}
