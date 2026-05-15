using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using System.Collections;

namespace ITClash.UI
{
    public class UIStartButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [Header("Scene Navigation")]
        [SerializeField] private string targetSceneName;
        [SerializeField] private float delayBeforeLoad = 0.5f;

        [Header("Hover Animation")]
        [SerializeField] private Vector3 hoverScale = new Vector3(1.1f, 1.1f, 1.1f);
        [SerializeField] private Color hoverColor = Color.white;
        [SerializeField] private float animationSmoothTime = 0.1f;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip hoverSFX;
        [SerializeField] private AudioClip clickSFX;

        private Graphic _graphic;
        private Vector3 _initialScale;
        private Color _initialColor;
        
        private Vector3 _targetScale;
        private Color _targetColor;
        private Vector3 _scaleVelocity;

        private void Awake()
        {
            _graphic = GetComponent<Graphic>();
            _initialScale = transform.localScale;
            _targetScale = _initialScale;

            if (_graphic != null)
            {
                _initialColor = _graphic.color;
                _targetColor = _initialColor;
            }

            if (audioSource == null)
            {
                audioSource = GetComponentInParent<AudioSource>();
            }
        }

        private void Update()
        {
            // Smooth Scale Transition
            transform.localScale = Vector3.SmoothDamp(
                transform.localScale, 
                _targetScale, 
                ref _scaleVelocity, 
                animationSmoothTime
            );

            // Smooth Color Transition
            if (_graphic != null)
            {
                _graphic.color = Color.Lerp(_graphic.color, _targetColor, Time.deltaTime * (1f / animationSmoothTime));
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _targetScale = hoverScale;
            _targetColor = hoverColor;

            if (audioSource != null && hoverSFX != null)
            {
                audioSource.PlayOneShot(hoverSFX);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _targetScale = _initialScale;
            _targetColor = _initialColor;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (audioSource != null && clickSFX != null)
            {
                audioSource.PlayOneShot(clickSFX);
            }

            if (!string.IsNullOrEmpty(targetSceneName))
            {
                StartCoroutine(LoadSceneRoutine());
            }
        }

        private IEnumerator LoadSceneRoutine()
        {
            // Wait for sound/animation to breathe
            yield return new WaitForSeconds(delayBeforeLoad);
            SceneManager.LoadScene(targetSceneName);
        }
    }
}
