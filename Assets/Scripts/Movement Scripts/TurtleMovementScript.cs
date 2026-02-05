using UnityEngine;

public class TurtleMovementScript : MonoBehaviour
{
    [Header("Patrol")]
    public float patrolDistance = 4f;         // half-width of left/right patrol around start
    public float baseSpeed = 1.25f;           // nominal horizontal speed (a bit faster than Fish)
    public float speedVariance = 0.25f;       // how much Perlin noise affects speed
    public bool startFacingRight = true;

    [Header("Sprite Orientation")]
    [Tooltip("Set true if the sprite/model faces right when localScale.x is positive")]
    public bool spriteFacesRight = true;

    [Header("Vertical / Bob")]
    public float bobAmplitude = 0.08f;        // subtle vertical motion for a heavier swimmer
    public float bobSpeed = 0.6f;             // slower bob
    public float verticalSmoothTime = 0.25f;  // smoother vertical response

    [Header("Rotation / Tilt")]
    public float maxTilt = 8f;                // small tilt for heavier body
    public float tiltSmoothTime = 0.18f;

    [Header("Noise")]
    public float noiseScale = 0.55f;          // frequency for Perlin noise
    public float randomSeedRange = 1000f;

    private Vector3 startPos;
    private Vector3 initialLocalScale;
    private float randomOffset;
    private int direction = 1;                // 1 = right, -1 = left
    private float currentVerticalVelocity;
    private float currentTiltVelocity;
    private float targetY;
    private float targetTilt;

    void Start()
    {
        startPos = transform.position;
        initialLocalScale = transform.localScale;
        randomOffset = Random.Range(0f, randomSeedRange);
        direction = startFacingRight ? 1 : -1;

        // Ensure initial facing matches configuration
        UpdateFacing();
    }

    void Update()
    {
        float t = Time.time;

        // Organic speed via Perlin noise
        float n = (Mathf.PerlinNoise((t + randomOffset) * noiseScale, 0f) - 0.5f) * 2f;
        float speed = Mathf.Max(0f, baseSpeed + n * speedVariance);

        // Horizontal patrol movement
        Vector3 pos = transform.position;
        pos.x += direction * speed * Time.deltaTime;

        // Flip at patrol bounds
        float offsetFromStart = pos.x - startPos.x;
        if (Mathf.Abs(offsetFromStart) >= patrolDistance)
        {
            pos.x = startPos.x + Mathf.Sign(offsetFromStart) * patrolDistance;
            FlipDirection();
        }

        // Vertical bobbing (smoother, smaller amplitude than fish)
        float bobNoise = (Mathf.PerlinNoise(0f, (t + randomOffset) * noiseScale * 0.6f) - 0.5f) * 0.5f;
        targetY = startPos.y + Mathf.Sin((t + randomOffset) * bobSpeed) * bobAmplitude + bobNoise * bobAmplitude;
        pos.y = Mathf.SmoothDamp(transform.position.y, targetY, ref currentVerticalVelocity, verticalSmoothTime);

        transform.position = pos;

        // Tilt slightly into movement; compensate for mirrored scale like Fish script
        float speedFactor = Mathf.Clamp01(speed / (baseSpeed + speedVariance));
        targetTilt = -direction * maxTilt * (0.5f + 0.5f * speedFactor);

        float flipSign = (transform.localScale.x < 0f) ? -1f : 1f;
        float finalTargetTilt = targetTilt * flipSign;

        float currentZ = transform.eulerAngles.z;
        float signedZ = NormalizeAngle(currentZ);
        float newZ = Mathf.SmoothDamp(signedZ, finalTargetTilt, ref currentTiltVelocity, tiltSmoothTime);
        transform.rotation = Quaternion.Euler(transform.eulerAngles.x, transform.eulerAngles.y, newZ);
    }

    private void FlipDirection()
    {
        direction *= -1;
        UpdateFacing();
    }

    private void UpdateFacing()
    {
        // Keep consistent with sprite orientation: (localScale.x > 0) == spriteFacesRight
        bool wantPositive = (direction > 0) == spriteFacesRight;
        float sign = wantPositive ? 1f : -1f;

        Vector3 s = initialLocalScale;
        s.x = Mathf.Abs(initialLocalScale.x) * sign;
        transform.localScale = s;
    }

    // Normalize 0..360 to -180..180
    private float NormalizeAngle(float a)
    {
        if (a > 180f) a -= 360f;
        return a;
    }
}