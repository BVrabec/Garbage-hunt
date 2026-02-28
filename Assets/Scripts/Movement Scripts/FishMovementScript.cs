using UnityEngine;

public class FishMovementScript : MonoBehaviour
{
    [Header("Patrol")]
    public float patrolDistance = 3f;         // half-width of left/right patrol around start
    public float baseSpeed = 1f;              // nominal horizontal speed
    public float speedVariance = 0.4f;        // how much Perlin noise affects speed
    public bool startFacingRight = true;

    [Header("Sprite Orientation")]
    [Tooltip("Set true if the sprite/model faces right when localScale.x is positive")]
    public bool spriteFacesRight = true;

    [Header("Vertical / Bob")]
    public float bobAmplitude = 0.15f;        // vertical bob amplitude
    public float bobSpeed = 1.2f;             // vertical bob speed
    public float verticalSmoothTime = 0.15f;  // smoothing for vertical motion

    [Header("Rotation / Tilt")]
    public float maxTilt = 12f;               // degrees tilt when turning/moving
    public float tiltSmoothTime = 0.12f;

    [Header("Noise")]
    public float noiseScale = 0.7f;           // frequency for Perlin noise
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

        // Set initial facing based on sprite orientation and desired start direction
        UpdateFacing();
    }

    void Update()
    {
        float t = Time.time;

        // Perlin-based speed modulation to feel organic
        float n = (Mathf.PerlinNoise((t + randomOffset) * noiseScale, 0f) - 0.5f) * 2f;
        float speed = Mathf.Max(0f, baseSpeed + n * speedVariance);

        // Horizontal movement (patrol between startPos.x +/- patrolDistance)
        Vector3 pos = transform.position;
        pos.x += direction * speed * Time.deltaTime;

        // If we've reached/exceeded patrol bounds, flip direction smoothly
        float offsetFromStart = pos.x - startPos.x;
        if (Mathf.Abs(offsetFromStart) >= patrolDistance)
        {
            // clamp to exact bound to avoid small overshoot
            pos.x = startPos.x + Mathf.Sign(offsetFromStart) * patrolDistance;
            FlipDirection();
        }

        // Vertical bobbing (target Y driven by sine + subtle noise)
        float bobNoise = (Mathf.PerlinNoise(0f, (t + randomOffset) * noiseScale * 0.6f) - 0.5f) * 0.5f;
        targetY = startPos.y + Mathf.Sin((t + randomOffset) * bobSpeed) * bobAmplitude + bobNoise * bobAmplitude;
        pos.y = Mathf.SmoothDamp(transform.position.y, targetY, ref currentVerticalVelocity, verticalSmoothTime);

        transform.position = pos;

        // Tilt fish to match movement direction and speed (small roll/pitch)
        float speedFactor = Mathf.Clamp01(speed / (baseSpeed + speedVariance));
        targetTilt = -direction * maxTilt * (0.6f + 0.4f * speedFactor); // lean into direction

        // When localScale.x is negative the Z rotation visually mirrors — compensate so tilt still leans into travel direction
        float flipSign = (transform.localScale.x < 0f) ? -1f : 1f;
        float finalTargetTilt = targetTilt * flipSign;

        float currentZ = transform.eulerAngles.z;
        // Convert to signed angle for smooth damping
        float signedZ = NormalizeAngle(currentZ);
        float newZ = Mathf.SmoothDamp(signedZ, finalTargetTilt, ref currentTiltVelocity, tiltSmoothTime);
        transform.rotation = Quaternion.Euler(transform.eulerAngles.x, transform.eulerAngles.y, newZ);
    }

    public void FlipDirection()
    {
        direction *= -1;
        UpdateFacing();
    }

    private void UpdateFacing()
    {
        // Determine sign for localScale.x such that (localScale.x > 0) == spriteFacesRight
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