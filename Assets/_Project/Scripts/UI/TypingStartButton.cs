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
        [Tooltip("TextMeshProUGUI สำหรับแสดงข้อความที่กำลังพิมพ์ (ตัวสีเขียว)")]
        [SerializeField] private TextMeshProUGUI wordDisplay;
        
        [Tooltip("(ใส่หรือไม่ใส่ก็ได้) TextMeshProUGUI สำหรับแสดงคำจางๆ ด้านหลัง ให้ผู้เล่นพิมพ์ตาม")]
        [SerializeField] private TextMeshProUGUI backgroundWordDisplay;

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

            // เซ็ตค่า Background Text ตั้งแต่เริ่ม
            if (backgroundWordDisplay != null)
            {
                backgroundWordDisplay.text = targetWord;
                backgroundWordDisplay.color = untypedColor;
                wordDisplay.color = typedColor; // ให้ตัวหลักเป็นสีที่พิมพ์ถูก
            }

            UpdateDisplay();
        }

        private void Update()
        {
            if (_isCompleted) return;

            // จับ A–Z ผ่าน KeyCode (ใช้ได้ทั้ง Legacy และ New Input System)
            for (KeyCode key = KeyCode.A; key <= KeyCode.Z; key++)
            {
                if (Input.GetKeyDown(key))
                {
                    char c = (char)('A' + (key - KeyCode.A));
                    ProcessChar(c);
                    if (_isCompleted) return;
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

            if (backgroundWordDisplay != null)
            {
                // โหมด 2 Text: Text หลักโชว์เฉพาะตัวที่พิมพ์แล้ว
                wordDisplay.text = targetWord.Substring(0, _currentIndex);
                wordDisplay.color = typedColor;               // ยืนยันว่าใช้สีที่ถูก
                backgroundWordDisplay.color = untypedColor;   // รีเซ็ตสีพื้นหลังกลับมา (แก้บัคค้างสีแดงตอนพิมพ์ผิด)
            }
            else
            {
                // โหมด 1 Text: ใช้ Rich Text 
                wordDisplay.color = Color.white; // รีเซ็ต base color ก่อนใช้ Rich Text

                string typedHex = ColorUtility.ToHtmlStringRGB(typedColor);
                string typedAlpha = Mathf.RoundToInt(typedColor.a * 255).ToString("X2");

                string untypedHex = ColorUtility.ToHtmlStringRGB(untypedColor);
                string untypedAlpha = Mathf.RoundToInt(untypedColor.a * 255).ToString("X2");

                string typedPart = targetWord.Substring(0, _currentIndex);
                string untypedPart = targetWord.Substring(_currentIndex);

                wordDisplay.text = $"<color=#{typedHex}><alpha=#{typedAlpha}>{typedPart}</color><color=#{untypedHex}><alpha=#{untypedAlpha}>{untypedPart}</color>";
            }
        }

        private IEnumerator ShowErrorFlash()
        {
            if (wordDisplay == null) yield break;

            if (backgroundWordDisplay != null)
            {
                // เปลี่ยนสี background เป็นสีแดงชั่วคราว
                backgroundWordDisplay.color = errorColor;
                wordDisplay.text = ""; // ซ่อน text ที่พิมพ์อยู่
            }
            else
            {
                string errorHex = ColorUtility.ToHtmlStringRGB(errorColor);
                string errorAlpha = Mathf.RoundToInt(errorColor.a * 255).ToString("X2");
                wordDisplay.text = $"<color=#{errorHex}><alpha=#{errorAlpha}>{targetWord}</color>";
            }

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
            if (backgroundWordDisplay != null)
            {
                backgroundWordDisplay.gameObject.SetActive(false); // ซ่อนพื้นหลัง
                wordDisplay.text = targetWord;
                wordDisplay.color = typedColor;
            }
            else
            {
                string typedHex = ColorUtility.ToHtmlStringRGB(typedColor);
                string typedAlpha = Mathf.RoundToInt(typedColor.a * 255).ToString("X2");

                if (wordDisplay != null)
                    wordDisplay.text = $"<color=#{typedHex}><alpha=#{typedAlpha}>{targetWord}</color>";
            }

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
