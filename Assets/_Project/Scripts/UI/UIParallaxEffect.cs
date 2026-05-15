using UnityEngine;

namespace ITClash.UI
{
    public class UIParallaxEffect : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float parallaxIntensity = 20f;
        [SerializeField] private float smoothTime = 0.1f;
        [SerializeField] private bool invertX = false;
        [SerializeField] private bool invertY = false;

        private RectTransform _rectTransform;
        private Vector2 _initialPosition;
        private Vector2 _currentVelocity;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            if (_rectTransform != null)
            {
                _initialPosition = _rectTransform.anchoredPosition;
            }
        }

        private void Update()
        {
            if (_rectTransform == null) return;

            // Get mouse position in range [-1, 1] relative to screen center
            Vector2 mousePos = Input.mousePosition;
            float screenWidth = Screen.width;
            float screenHeight = Screen.height;

            float mouseX = (mousePos.x - (screenWidth / 2f)) / (screenWidth / 2f);
            float mouseY = (mousePos.y - (screenHeight / 2f)) / (screenHeight / 2f);

            // Apply inversion
            if (invertX) mouseX *= -1;
            if (invertY) mouseY *= -1;

            // Target position based on parallax intensity
            Vector2 targetOffset = new Vector2(mouseX * parallaxIntensity, mouseY * parallaxIntensity);
            Vector2 targetPosition = _initialPosition + targetOffset;

            // Smooth movement
            _rectTransform.anchoredPosition = Vector2.SmoothDamp(
                _rectTransform.anchoredPosition, 
                targetPosition, 
                ref _currentVelocity, 
                smoothTime
            );
        }
    }
}
