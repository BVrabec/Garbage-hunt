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

    [Header("Impact scaling (reduces bounce for soft collisions)")]
    [Tooltip("Collision relative velocity magnitude that maps to a full impact (tune to scene)")]
    public float impactForceNormalization = 1.5f;
    [Tooltip("Minimum impact factor applied even for very soft collisions (0..1)")]
    [Range(0f, 1f)]
    public float minImpactFactor = 0.08f;

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
        if (collision.contactCount == 0) return;

        // Use first contact as main normal
        ContactPoint2D contact = collision.GetContact(0);
        Vector2 normal = contact.normal.normalized;

        // Other rigidbody & velocities
        Rigidbody2D otherRb = collision.rigidbody;
        Vector2 otherVel = otherRb != null ? otherRb.linearVelocity : Vector2.zero;
        Vector2 myVel = rb.linearVelocity;
        Vector2 relVel = myVel - otherVel;

        // Compute impact strength from collision.relativeVelocity if available (best), fallback to relVel
        float impactMag = collision.relativeVelocity.magnitude;
        if (impactMag <= 0f)
            impactMag = relVel.magnitude;

        // Map impact magnitude to an impact factor in [minImpactFactor..1]
        float impactFactor = Mathf.Clamp01(impactMag / Mathf.Max(0.0001f, impactForceNormalization));
        impactFactor = Mathf.Lerp(minImpactFactor, 1f, impactFactor);

        // Reflect relative velocity and scale by bounce adjusted to impact factor
        Vector2 reflectedRel = Vector2.Reflect(relVel, normal) * (bounceCoefficient * impactFactor);

        // Compose final velocity (give some weight back from other body so heavier movers influence the result)
        Vector2 final = reflectedRel + otherVel * 0.25f;

        // Prevent creating very small oscillating velocities
        if (final.sqrMagnitude < minBounceSpeed * minBounceSpeed)
            final = Vector2.zero;

        rb.linearVelocity = final;

        // Scale spin by impact factor so soft contacts spin less
        float spin = Random.Range(-1f, 1f) * collisionSpin * impactFactor * (1f - bounceCoefficient);
        rb.AddTorque(spin, ForceMode2D.Impulse);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // If colliders are set as triggers (overlap events), push objects slightly apart rather than doing nothing.
        if (rb == null) return;
        Vector2 away = (transform.position - other.transform.position).normalized;
        if (away == Vector2.zero) away = Random.insideUnitCircle.normalized;

        // estimate impact magnitude for trigger using current velocity
        float impactMag = rb.linearVelocity.magnitude;
        float impactFactor = Mathf.Clamp01(impactMag / Mathf.Max(0.0001f, impactForceNormalization));
        impactFactor = Mathf.Lerp(minImpactFactor, 1f, impactFactor);

        // Give a small bounce outward but weaker than collision response, scaled by impactFactor
        rb.linearVelocity = away * Mathf.Max(rb.linearVelocity.magnitude * bounceCoefficient * 0.6f * impactFactor, minBounceSpeed);

        float spin = Random.Range(-1f, 1f) * collisionSpin * 0.25f * impactFactor;
        rb.AddTorque(spin, ForceMode2D.Impulse);
    }

    // --- Utility used by the spawner: ensure spawned clones (and children with colliders) have this script ---
    /// <summary>
    /// Ensure that every child GameObject under root that has a Collider2D also has a TrashColliderScript.
    /// The spawner should call this on newly created objects so spawned clones always get bounce behavior.
    /// </summary>
    public static void EnsureOn(GameObject root)
    {
        if (root == null) return;
        // add to root if it has a Collider2D and no script
        var cols = root.GetComponentsInChildren<Collider2D>(true);
        foreach (var c in cols)
        {
            if (c == null) continue;
            if (c.GetComponent<TrashColliderScript>() == null)
            {
                // attach to the GameObject that owns the collider
                c.gameObject.AddComponent<TrashColliderScript>();
            }
        }
    }
}