using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using System.Collections;

namespace ITClash.UI
{
    public class ITClashUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [Header("--- Fade In ---")]
        public bool fadeInOnStart = true;
        public float fadeDuration = 1f;

        [Header("--- Hover Animation ---")]
        public bool useHoverScale = true;
        public Vector3 hoverScale = new Vector3(1.1f, 1.1f, 1.1f);
        public bool useHoverColor = true;
        public Color hoverColor = Color.white;
        public float animationSmoothTime = 0.1f;

        [Header("--- Parallax & Tilt ---")]
        public bool useParallax = false;
        public float parallaxIntensity = 20f;
        public bool useTilt = false;
        public float maxTiltAngle = 10f;

        [Header("--- Navigation ---")]
        public string targetSceneName;
        public GameObject panelToOpen;
        public GameObject panelToClose;
        public float navigationDelay = 0.3f;

        [Header("--- Audio ---")]
        public AudioSource audioSource;
        public AudioClip hoverSFX;
        public AudioClip clickSFX;

        // Internal State
        private RectTransform _rectTransform;
        private CanvasGroup _canvasGroup;
        private Graphic _graphic;
        
        private Vector3 _initialScale;
        private Vector2 _initialAnchoredPos;
        private Color _initialColor;
        private Quaternion _initialRotation;

        private Vector3 _targetScale;
        private Color _targetColor;
        private Vector3 _scaleVelocity;
        private Vector2 _parallaxVelocity;
        private Vector2 _currentMoveVelocity;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _graphic = GetComponent<Graphic>();
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null && fadeInOnStart) _canvasGroup = gameObject.AddComponent<CanvasGroup>();

            _initialScale = transform.localScale;
            _initialRotation = transform.localRotation;
            if (_rectTransform) _initialAnchoredPos = _rectTransform.anchoredPosition;
            if (_graphic) _initialColor = _graphic.color;

            _targetScale = _initialScale;
            if (_graphic) _targetColor = _initialColor;

            if (audioSource == null) audioSource = GetComponentInParent<AudioSource>();
        }

        private void Start()
        {
            if (fadeInOnStart && _canvasGroup) StartCoroutine(FadeInRoutine());
        }

        private IEnumerator FadeInRoutine()
        {
            float elapsed = 0f;
            _canvasGroup.alpha = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                _canvasGroup.alpha = elapsed / fadeDuration;
                yield return null;
            }
            _canvasGroup.alpha = 1f;
        }

        private void Update()
        {
            // 1. Hover Scale
            transform.localScale = Vector3.SmoothDamp(transform.localScale, _targetScale, ref _scaleVelocity, animationSmoothTime);

            // 2. Hover Color
            if (_graphic && useHoverColor)
                _graphic.color = Color.Lerp(_graphic.color, _targetColor, Time.deltaTime * (1f / animationSmoothTime));

            // 3. Parallax & Tilt
            HandleParallaxAndTilt();
        }

        private void HandleParallaxAndTilt()
        {
            if (!useParallax && !useTilt) return;

            Vector2 mousePos = Input.mousePosition;
            float mouseX = (mousePos.x - (Screen.width / 2f)) / (Screen.width / 2f);
            float mouseY = (mousePos.y - (Screen.height / 2f)) / (Screen.height / 2f);

            if (useParallax && _rectTransform)
            {
                Vector2 targetPos = _initialAnchoredPos + new Vector2(mouseX * parallaxIntensity, mouseY * parallaxIntensity);
                _rectTransform.anchoredPosition = Vector2.SmoothDamp(_rectTransform.anchoredPosition, targetPos, ref _parallaxVelocity, animationSmoothTime);
            }

            if (useTilt)
            {
                float tiltX = -mouseY * maxTiltAngle;
                float tiltY = mouseX * maxTiltAngle;
                Quaternion targetRot = _initialRotation * Quaternion.Euler(tiltX, tiltY, 0f);
                transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRot, 1f - Mathf.Exp(-animationSmoothTime * 100f * Time.deltaTime));
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (useHoverScale) _targetScale = hoverScale;
            if (useHoverColor) _targetColor = hoverColor;
            if (audioSource && hoverSFX) audioSource.PlayOneShot(hoverSFX);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _targetScale = _initialScale;
            _targetColor = _initialColor;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (audioSource && clickSFX) audioSource.PlayOneShot(clickSFX);
            StartCoroutine(NavigationRoutine());
        }

        private IEnumerator NavigationRoutine()
        {
            yield return new WaitForSeconds(navigationDelay);

            if (!string.IsNullOrEmpty(targetSceneName))
                SceneManager.LoadScene(targetSceneName);

            if (panelToOpen) panelToOpen.SetActive(true);
            if (panelToClose) panelToClose.SetActive(false);
        }
    }
}
