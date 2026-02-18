using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class TrashColliderScript : MonoBehaviour
{
    [Header("Underwater physics")]
    [Tooltip("0 = no bounce, 1 = perfect elastic")]
    public float bounceCoefficient = 0.6f;
    [Tooltip("Built-in Rigidbody2D linear drag to simulate water resistance")]
    public float linearDrag = 1.2f;
    [Tooltip("Angular drag for underwater rotational damping")]
    public float angularDrag = 2f;
    [Tooltip("Small constant upward force to simulate buoyancy")]
    public float buoyancyForce = 0.5f;
    [Tooltip("Minimum speed under which we stop tiny bounces")]
    public float minBounceSpeed = 0.05f;
    [Tooltip("Add a small random spin on collision for organic tumble")]
    public float collisionSpin = 80f;

    Rigidbody2D rb;
    Collider2D col;

    void Awake()
    {
        col = GetComponent<Collider2D>();

        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            // Add a Rigidbody2D if the prefab doesn't have one so collisions are handled by physics.
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        // Configure underwater-like physics defaults
        rb.gravityScale = 0f; // buoyancy handled manually
        rb.linearDamping = linearDrag;
        rb.angularDamping = angularDrag;

        // Allow rotation so objects can tumble when colliding
        rb.freezeRotation = false;
    }

    void FixedUpdate()
    {
        // Apply gentle buoyancy each physics step (force scaled by mass so different masses behave reasonably).
        rb.AddForce(Vector2.up * buoyancyForce * rb.mass, ForceMode2D.Force);

        // Safety: clamp very tiny velocities to zero to avoid jitter.
        if (rb.linearVelocity.sqrMagnitude < minBounceSpeed * minBounceSpeed)
            rb.linearVelocity = Vector2.zero;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (rb == null) return;

        // Process each contact to produce a realistic reflection based on the contact normal.
        // Use the first contact for a primary normal (common, stable behavior).
        if (collision.contactCount == 0) return;
        ContactPoint2D contact = collision.GetContact(0);
        Vector2 normal = contact.normal.normalized;

        // If the other object has a Rigidbody2D, consider relative velocity for a realistic bounce.
        Rigidbody2D otherRb = collision.rigidbody;
        Vector2 myVel = rb.linearVelocity;
        Vector2 otherVel = otherRb != null ? otherRb.linearVelocity : Vector2.zero;
        Vector2 relVel = myVel - otherVel;

        // Reflect relative velocity around normal
        Vector2 reflectedRel = Vector2.Reflect(relVel, normal) * bounceCoefficient;

        // Compose final velocity (give some weight back from other body so heavier movers influence the result)
        Vector2 final = reflectedRel + otherVel * 0.25f;

        // Prevent creating very small oscillating velocities
        if (final.sqrMagnitude < minBounceSpeed * minBounceSpeed)
            final = Vector2.zero;

        rb.linearVelocity = final;

        // Add a small random angular impulse so objects spin/tumble on impact
        float spin = Random.Range(-1f, 1f) * collisionSpin * (1f - bounceCoefficient);
        rb.AddTorque(spin, ForceMode2D.Impulse);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // If colliders are set as triggers (overlap events), push objects slightly apart rather than doing nothing.
        if (rb == null) return;
        Vector2 away = (transform.position - other.transform.position).normalized;
        if (away == Vector2.zero) away = Random.insideUnitCircle.normalized;
        // Give a small bounce outward but weaker than collision response.
        rb.linearVelocity = away * Mathf.Max(rb.linearVelocity.magnitude * bounceCoefficient * 0.6f, minBounceSpeed);
        rb.AddTorque(Random.Range(-1f, 1f) * collisionSpin * 0.25f, ForceMode2D.Impulse);
    }
}