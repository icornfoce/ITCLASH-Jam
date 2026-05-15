using UnityEngine;

namespace ITClash.UI
{
    public class UITiltEffect : MonoBehaviour
    {
        [Header("Tilt Settings")]
        [SerializeField] private float maxTiltAngle = 10f;
        [SerializeField] private float smoothTime = 0.1f;
        
        [Header("Movement (Optional Parallax)")]
        [SerializeField] private float moveIntensity = 5f;

        private RectTransform _rectTransform;
        private Quaternion _initialRotation;
        private Vector2 _initialPosition;
        private Vector3 _currentRotationVelocity;
        private Vector2 _currentMoveVelocity;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _initialRotation = _rectTransform.localRotation;
            _initialPosition = _rectTransform.anchoredPosition;
        }

        private void Update()
        {
            if (_rectTransform == null) return;

            // Get normalized mouse position (-1 to 1)
            Vector2 mousePos = Input.mousePosition;
            float mouseX = (mousePos.x - (Screen.width / 2f)) / (Screen.width / 2f);
            float mouseY = (mousePos.y - (Screen.height / 2f)) / (Screen.height / 2f);

            // Calculate Target Rotation (Tilt)
            // Note: X rotation is driven by Y mouse pos, Y rotation by X mouse pos
            float tiltX = -mouseY * maxTiltAngle;
            float tiltY = mouseX * maxTiltAngle;
            Quaternion targetRotation = _initialRotation * Quaternion.Euler(tiltX, tiltY, 0f);

            // Calculate Target Position (Micro-parallax)
            Vector2 targetPos = _initialPosition + new Vector2(mouseX * moveIntensity, mouseY * moveIntensity);

            // Apply Smooth Rotation
            transform.localRotation = Quaternion.Slerp(
                transform.localRotation, 
                targetRotation, 
                1f - Mathf.Exp(-smoothTime * 100f * Time.deltaTime) // Fast responsive lerp
            );

            // Apply Smooth Position
            _rectTransform.anchoredPosition = Vector2.SmoothDamp(
                _rectTransform.anchoredPosition,
                targetPos,
                ref _currentMoveVelocity,
                smoothTime
            );
        }
    }
}
