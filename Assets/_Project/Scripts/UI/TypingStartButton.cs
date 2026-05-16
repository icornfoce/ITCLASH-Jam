using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

namespace ITClash.UI
{
    /// <summary>
    /// ปุ่มเริ่มเกมแบบพิมพ์คำ — แทนที่การคลิก
    /// แสดงคำเป้าหมาย ผู้เล่นพิมพ์ตัวอักษรที่ถูกต้องทีละตัว พิมพ์ครบ = เริ่มเกม
    /// </summary>
    public class TypingStartButton : MonoBehaviour
    {
        [Header("── Target Word ──")]
        [Tooltip("คำที่ผู้เล่นต้องพิมพ์เพื่อเริ่มเกม")]
        [SerializeField] private string targetWord = "START";

        [Header("── Scene Navigation ──")]
        [SerializeField] private string targetSceneName;
        [SerializeField] private float delayBeforeLoad = 0.5f;

        [Header("── UI References ──")]
        [Tooltip("TextMeshProUGUI สำหรับแสดงคำที่ต้องพิมพ์")]
        [SerializeField] private TextMeshProUGUI wordDisplay;

        [Header("── Colors ──")]
        [SerializeField] private Color typedColor = new Color(0.2f, 1f, 0.4f);    // สีตัวอักษรที่พิมพ์ถูกแล้ว
        [SerializeField] private Color untypedColor = new Color(1f, 1f, 1f, 0.5f); // สีตัวอักษรที่ยังไม่ได้พิมพ์
        [SerializeField] private Color errorColor = new Color(1f, 0.2f, 0.2f);     // สีเมื่อพิมพ์ผิด

        [Header("── Audio ──")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip typeSFX;
        [SerializeField] private AudioClip errorSFX;
        [SerializeField] private AudioClip successSFX;

        [Header("── Fade Transition ──")]
        [SerializeField] private CanvasGroup fadeCanvasGroup;
        [SerializeField] private float fadeDuration = 1f;

        [Header("── Animation ──")]
        [Tooltip("สั่นเมื่อพิมพ์ผิด")]
        [SerializeField] private float shakeIntensity = 10f;
        [SerializeField] private float shakeDuration = 0.3f;

        // ─── Internal ───
        private int _currentIndex = 0;
        private bool _isCompleted = false;
        private bool _isShaking = false;
        private Vector2 _originalPos;
        private RectTransform _rectTransform;

        private void Awake()
        {
            _rectTransform = wordDisplay != null ? wordDisplay.GetComponent<RectTransform>() : null;
            if (_rectTransform != null) _originalPos = _rectTransform.anchoredPosition;

            if (audioSource == null) audioSource = GetComponentInParent<AudioSource>();
        }

        private void Start()
        {
            _currentIndex = 0;
            _isCompleted = false;
            UpdateDisplay();
        }

        private void Update()
        {
            if (_isCompleted) return;

            // จับทุกตัวอักษรที่พิมพ์ในเฟรมนี้
            if (!string.IsNullOrEmpty(Input.inputString))
            {
                foreach (char c in Input.inputString)
                {
                    ProcessChar(c);
                    if (_isCompleted) break;
                }
            }
        }

        private void ProcessChar(char typed)
        {
            if (_currentIndex >= targetWord.Length) return;

            char expected = targetWord[_currentIndex];

            // เทียบแบบ case-insensitive
            if (char.ToUpper(typed) == char.ToUpper(expected))
            {
                // ✅ ถูกต้อง
                _currentIndex++;
                PlaySFX(typeSFX);
                UpdateDisplay();

                // พิมพ์ครบ!
                if (_currentIndex >= targetWord.Length)
                {
                    _isCompleted = true;
                    OnWordCompleted();
                }
            }
            else
            {
                // ❌ ผิด — รีเซ็ตกลับเริ่มต้น
                _currentIndex = 0;
                PlaySFX(errorSFX);
                StartCoroutine(ShowErrorFlash());
            }
        }

        private void UpdateDisplay()
        {
            if (wordDisplay == null) return;

            // สร้างข้อความด้วย rich text: ส่วนที่พิมพ์แล้ว = สีเขียว, ที่เหลือ = สีจาง
            string typedHex = ColorUtility.ToHtmlStringRGBA(typedColor);
            string untypedHex = ColorUtility.ToHtmlStringRGBA(untypedColor);

            string typedPart = targetWord.Substring(0, _currentIndex);
            string untypedPart = targetWord.Substring(_currentIndex);

            wordDisplay.text = $"<color=#{typedHex}>{typedPart}</color><color=#{untypedHex}>{untypedPart}</color>";
        }

        private IEnumerator ShowErrorFlash()
        {
            if (wordDisplay == null) yield break;

            // แสดงข้อความเป็นสีแดงชั่วครู่
            string errorHex = ColorUtility.ToHtmlStringRGBA(errorColor);
            wordDisplay.text = $"<color=#{errorHex}>{targetWord}</color>";

            // สั่น
            if (_rectTransform != null && !_isShaking)
            {
                StartCoroutine(ShakeRoutine());
            }

            yield return new WaitForSecondsRealtime(0.15f);

            // กลับสู่ปกติ
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

            // แสดงข้อความครบเป็นสีเขียวสดใส
            string typedHex = ColorUtility.ToHtmlStringRGBA(typedColor);
            if (wordDisplay != null)
                wordDisplay.text = $"<color=#{typedHex}>{targetWord}</color>";

            if (!string.IsNullOrEmpty(targetSceneName))
            {
                if (fadeCanvasGroup != null)
                {
                    StartCoroutine(FadeOutAndLoad());
                }
                else
                {
                    StartCoroutine(DelayedLoad());
                }
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
