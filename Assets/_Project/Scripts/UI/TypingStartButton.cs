using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

namespace ITClash.UI
{
    public class TypingStartButton : MonoBehaviour
    {
        [Header("── Target Word ──")]
        [SerializeField] private string targetWord = "START";

        [Header("── Scene Navigation ──")]
        [SerializeField] private string targetSceneName;
        [SerializeField] private float delayBeforeLoad = 0.5f;

        [Header("── UI References ──")]
        [Tooltip("ใช้ TMP_InputField เพื่อให้เหมือนระบบเก่า (AimTypingSystem)")]
        [SerializeField] private TMP_InputField inputField;

        [Header("── Colors ──")]
        [SerializeField] private Color typedColor = new Color(1f, 1f, 1f, 1f);       // สีตัวอักษรหลัก
        [SerializeField] private Color selectionColor = new Color(0.1f, 0.4f, 0.8f, 0.2f); // สีไฮไลท์ (ทำให้คำดูจาง)
        [SerializeField] private Color errorColor = new Color(1f, 0.2f, 0.2f, 1f);      // สีเมื่อพิมพ์ผิด

        [Header("── Audio ──")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip typeSFX;
        [SerializeField] private AudioClip errorSFX;
        [SerializeField] private AudioClip successSFX;

        [Header("── Fade Transition ──")]
        [SerializeField] private CanvasGroup fadeCanvasGroup;
        [SerializeField] private float fadeDuration = 1f;

        [Header("── Animation ──")]
        [SerializeField] private float shakeIntensity = 10f;
        [SerializeField] private float shakeDuration = 0.3f;

        // ─── Internal ───
        private int _currentIndex = 0;
        private bool _isCompleted = false;
        private bool _isShaking = false;
        private Vector2 _originalPos;
        private RectTransform _rectTransform;
        private string _lastInput = "";

        private void Awake()
        {
            if (inputField != null)
            {
                _rectTransform = inputField.GetComponent<RectTransform>();
                _originalPos = _rectTransform.anchoredPosition;
                
                // ตั้งค่าเบื้องต้นให้เหมือนระบบเก่า
                inputField.onValueChanged.AddListener(OnInputValueChanged);
                inputField.textComponent.color = typedColor;
                inputField.selectionColor = selectionColor;
                
                // ปิดการใช้ Rich Text ใน InputField เพื่อไม่ให้ตีกับระบบคัดลอกตำแหน่ง
                inputField.richText = false;
            }

            if (audioSource == null) audioSource = GetComponentInParent<AudioSource>();
        }

        private void Start()
        {
            ResetTyping();
        }

        private void ResetTyping()
        {
            _currentIndex = 0;
            _isCompleted = false;
            _lastInput = "";
            
            if (inputField != null)
            {
                inputField.interactable = true;
                inputField.ActivateInputField();
                UpdateDisplay();
            }
        }

        private void Update()
        {
            if (_isCompleted || inputField == null) return;

            // บังคับให้ Focus ตลอดเวลาเพื่อให้พิมพ์ได้ทันที
            if (!inputField.isFocused)
            {
                inputField.ActivateInputField();
            }
        }

        private void OnInputValueChanged(string newValue)
        {
            if (_isCompleted) return;

            // ตรวจสอบว่าเป็นการลบคำหรือไม่
            bool isDeleting = newValue.Length < _lastInput.Length;
            if (isDeleting)
            {
                UpdateDisplay();
                return;
            }

            // ตรวจสอบตัวอักษรล่าสุด
            if (newValue.Length > _currentIndex)
            {
                char typed = newValue[newValue.Length - 1];
                char expected = targetWord[_currentIndex];

                if (char.ToUpper(typed) == char.ToUpper(expected))
                {
                    _currentIndex++;
                    PlaySFX(typeSFX);
                    
                    if (_currentIndex >= targetWord.Length)
                    {
                        _isCompleted = true;
                        OnWordCompleted();
                    }
                    else
                    {
                        UpdateDisplay();
                    }
                }
                else
                {
                    PlaySFX(errorSFX);
                    StartCoroutine(ShowErrorFlash());
                }
            }

            _lastInput = inputField.text;
        }

        private void UpdateDisplay()
        {
            if (inputField == null) return;

            // ใช้เทคนิค Autocomplete แบบระบบเก่าเป๊ะๆ
            inputField.SetTextWithoutNotify(targetWord);
            inputField.selectionAnchorPosition = _currentIndex;
            inputField.selectionFocusPosition = targetWord.Length;
            inputField.caretPosition = _currentIndex;
        }

        private IEnumerator ShowErrorFlash()
        {
            if (inputField == null) yield break;

            inputField.textComponent.color = errorColor;
            
            if (_rectTransform != null && !_isShaking)
                StartCoroutine(ShakeRoutine());

            yield return new WaitForSecondsRealtime(0.15f);

            inputField.textComponent.color = typedColor;
            UpdateDisplay();
        }

        private IEnumerator ShakeRoutine()
        {
            _isShaking = true;
            float elapsed = 0f;

            while (elapsed < shakeDuration)
            {
                float offsetX = Random.Range(-shakeIntensity, shakeIntensity);
                _rectTransform.anchoredPosition = _originalPos + new Vector2(offsetX, 0f);
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            _rectTransform.anchoredPosition = _originalPos;
            _isShaking = false;
        }

        private void OnWordCompleted()
        {
            PlaySFX(successSFX);

            if (inputField != null)
            {
                inputField.SetTextWithoutNotify(targetWord);
                inputField.selectionAnchorPosition = targetWord.Length;
                inputField.selectionFocusPosition = targetWord.Length;
                inputField.interactable = false;
            }

            if (!string.IsNullOrEmpty(targetSceneName))
            {
                if (fadeCanvasGroup != null)
                    StartCoroutine(FadeOutAndLoad());
                else
                    StartCoroutine(DelayedLoad());
            }
        }

        private IEnumerator DelayedLoad()
        {
            yield return new WaitForSecondsRealtime(delayBeforeLoad);
            SceneManager.LoadScene(targetSceneName);
        }

        private IEnumerator FadeOutAndLoad()
        {
            yield return new WaitForSecondsRealtime(delayBeforeLoad * 0.5f);

            fadeCanvasGroup.blocksRaycasts = true;
            float timer = 0f;
            while (timer < fadeDuration)
            {
                timer += Time.unscaledDeltaTime;
                fadeCanvasGroup.alpha = timer / fadeDuration;
                yield return null;
            }
            fadeCanvasGroup.alpha = 1f;
            SceneManager.LoadScene(targetSceneName);
        }

        private void PlaySFX(AudioClip clip)
        {
            if (audioSource != null && clip != null)
                audioSource.PlayOneShot(clip);
        }
    }
}
