using UnityEngine;

public class Trash : MonoBehaviour
{
    [Header("Motion")]
    [Tooltip("How quickly the object moves toward its computed target (higher = snappier)")]
    public float floatSpeed = 2.0f;
    [Tooltip("Horizontal sway amplitude (world units)")]
    public float swayAmount = 0.25f;
    [Tooltip("Vertical bob amplitude (world units)")]
    public float bobHeight = 0.12f;
    [Tooltip("Vertical bob frequency")]
    public float bobSpeed = 0.9f;
    [Tooltip("Perlin noise frequency for organic motion")]
    public float noiseScale = 0.6f;
    [Tooltip("How far the object slowly drifts from its start position")]
    public float driftDistance = 0.6f;
    [Tooltip("Prevent object from wandering too far from start position")]
    public float maxDistanceFromStart = 3f;
    [Tooltip("Minimum allowed distance below start Y (prevents falling under map)")]
    public float minYBelowStart = 0.6f;

    [Header("Misc")]
    [Tooltip("If true and a Rigidbody2D is present, movement will nudge the rigidbody velocity instead of setting transform.position")]
    public bool useRigidbodyIfPresent = true;

    Vector3 startPos;
    Vector3 driftTarget;
    float randomOffset;
    Rigidbody2D rb;

    void Start()
    {
        startPos = transform.position;
        randomOffset = Random.Range(0f, 1000f);
        rb = GetComponent<Rigidbody2D>();

        // initial drift target so items don't all drift identically
        driftTarget = startPos + new Vector3(
            Random.Range(-driftDistance, driftDistance),
            Random.Range(-driftDistance * 0.25f, driftDistance * 0.25f),
            0f
        );
    }

    void Update()
    {
        float t = Time.time;

        // Perlin noise for lateral, subtle variation in vertical
        float nX = (Mathf.PerlinNoise((t + randomOffset) * noiseScale, 0f) - 0.5f) * 2f;
        float nY = (Mathf.PerlinNoise(0f, (t + randomOffset) * noiseScale) - 0.5f) * 2f;

        // Sway: horizontal (X)
        float swayX = nX * swayAmount;

        // Bob: vertical (Y) = sine + small perlin contribution
        float bob = Mathf.Sin((t + randomOffset) * bobSpeed) * bobHeight + nY * (bobHeight * 0.35f);

        // Occasionally choose a new slow drift target so items meander
        if (Random.value < 0.003f)
        {
            driftTarget = startPos + new Vector3(
                Random.Range(-driftDistance, driftDistance),
                Random.Range(-driftDistance * 0.25f, driftDistance * 0.25f),
                0f
            );
        }
        // gently move driftTarget back toward start if it wanders too far
        if (Vector2.Distance(driftTarget, startPos) > driftDistance)
            driftTarget = Vector3.Lerp(driftTarget, startPos, Time.deltaTime * 0.5f);

        // Compose desired world-space target position
        Vector3 targetPos = new Vector3(
            driftTarget.x + swayX,
            driftTarget.y + bob,
            transform.position.z
        );

        // enforce vertical floor relative to startPos so objects don't sink below map
        float minY = startPos.y - minYBelowStart;
        if (targetPos.y < minY)
            targetPos.y = minY;

        // clamp overall distance from start
        Vector3 offsetFromStart = targetPos - startPos;
        if (offsetFromStart.magnitude > maxDistanceFromStart)
            targetPos = startPos + offsetFromStart.normalized * maxDistanceFromStart;

        // Apply movement:
        // - If a non-kinematic Rigidbody2D exists and useRigidbodyIfPresent is true, nudge its velocity
        // - Otherwise, smoothly set transform.position (prevents objects being pulled down by external forces)
        if (useRigidbodyIfPresent && rb != null && rb.bodyType != RigidbodyType2D.Kinematic)
        {
            // Proportional controller toward target (keeps physics interactions possible)
            Vector2 toTarget = (Vector2)(targetPos - transform.position);
            Vector2 desiredVel = toTarget * floatSpeed;
            // Smoothly blend current velocity toward desired velocity so we don't abruptly override physics
            rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, desiredVel, Mathf.Clamp01(Time.deltaTime * 6f));
        }
        else
        {
            // Smoothly move transform toward target � LateUpdate/other scripts can still apply additive impulses afterwards
            transform.position = Vector3.Lerp(transform.position, targetPos, 1f - Mathf.Exp(-floatSpeed * Time.deltaTime));
        }
    }
}