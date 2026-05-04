using UnityEngine;

public enum GemType
{
    Common,
    Uncommon,
    Rare
}

public class ExpGem : MonoBehaviour
{
    [Header("Gem Settings")]
    public GemType gemType = GemType.Common;
    [Tooltip("Base EXP ปริมาณ EXP พื้นฐานก่อนคูณ Growth")]
    public float baseExpValue = 1f;

    [Header("Dimension Setting")]
    public bool is2DGame = false; // ติ๊กถูกถ้าฉากเป็นแกน XY (2D), เอาออกถ้าฉากเป็นประนาบ XZ (3D)
    
    [Header("Scatter Settings")]
    public float minDropRadius = 0.5f;
    public float maxDropRadius = 2f;
    public float scatterDuration = 0.5f;
    [Tooltip("รูปทรงโค้งการกระเด็น")]
    public AnimationCurve scatterCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("Magnet / Pull Settings")]
    public float pullRadius = 3.5f;
    public float initialPullSpeed = 1f;
    public float pullAcceleration = 15f;
    public float maxPullSpeed = 30f;

    [Header("Merge Settings")]
    public float timeToMerge = 30f; // เวลาที่ทิ้งไว้บนพื้นก่อนจะรวมร่าง

    private float _timeOnGround = 0f;
    private bool _isScattering = true;
    private bool _isPulled = false;
    private float _currentPullSpeed = 0f;
    private Vector3 _scatterTargetPos;
    private Vector3 _startPos;
    private float _scatterTimer = 0f;
    private Transform _targetTransform;

    public bool IsReadyToMerge => !_isPulled && !_isScattering && _timeOnGround >= timeToMerge;

    private void OnEnable()
    {
        _isScattering = true;
        _isPulled = false;
        _timeOnGround = 0f;
        _scatterTimer = 0f;
        _startPos = transform.position;
        
        // สุ่มจุดตกรอบๆ
        Vector2 randomCircle = Random.insideUnitCircle.normalized * Random.Range(minDropRadius, maxDropRadius);
        Vector3 randomOffset = is2DGame ? new Vector3(randomCircle.x, randomCircle.y, 0f) : new Vector3(randomCircle.x, 0f, randomCircle.y);
        _scatterTargetPos = _startPos + randomOffset;

        if (PlayerExperience.Instance != null)
        {
            _targetTransform = PlayerExperience.Instance.gemCollectorPoint != null 
                ? PlayerExperience.Instance.gemCollectorPoint 
                : PlayerExperience.Instance.transform;
        }

        if (GemManager.Instance != null)
        {
            GemManager.Instance.RegisterGem(this);
        }
    }

    private void OnDisable()
    {
        if (GemManager.Instance != null)
        {
            GemManager.Instance.UnregisterGem(this);
        }
    }

    private void Update()
    {
        if (_isScattering)
        {
            HandleScatter();
        }
        else
        {
            HandlePull();
        }
    }

    private void HandleScatter()
    {
        _scatterTimer += Time.deltaTime;
        float percent = Mathf.Clamp01(_scatterTimer / scatterDuration);
        float curvePercent = scatterCurve.Evaluate(percent);

        transform.position = Vector3.Lerp(_startPos, _scatterTargetPos, curvePercent);

        if (percent >= 1f)
        {
            _isScattering = false;
        }
    }

    private void HandlePull()
    {
        if (_targetTransform == null) return;

        float distanceToTarget = Vector3.Distance(transform.position, _targetTransform.position);

        if (_isPulled || distanceToTarget <= pullRadius)
        {
            if (!_isPulled)
            {
                _isPulled = true;
                _currentPullSpeed = initialPullSpeed;
            }

            // v = u + at (ความเร่ง)
            _currentPullSpeed += pullAcceleration * Time.deltaTime;
            _currentPullSpeed = Mathf.Min(_currentPullSpeed, maxPullSpeed);

            // เคลื่อนที่แบบนุ่มนวล
            transform.position = Vector3.MoveTowards(transform.position, _targetTransform.position, _currentPullSpeed * Time.deltaTime);

            if (distanceToTarget < 0.2f) // ระยะชนเก็บ
            {
                CollectGem();
            }
        }
        else
        {
            // ถูกทิ้งไว้เฉยๆ บนพื้น
            _timeOnGround += Time.deltaTime;
        }
    }

    private void CollectGem()
    {
        if (PlayerExperience.Instance != null)
        {
            PlayerExperience.Instance.AddExperience(baseExpValue);
        }
        
        if (GemManager.Instance != null)
        {
            GemManager.Instance.ReturnToPool(this);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
