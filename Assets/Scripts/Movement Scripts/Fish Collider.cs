using UnityEngine;

/// <summary>
/// When a turtle collides with this fish, both turn around.
/// Attach to fish prefabs (they already have a Collider2D).
/// Uses a small cooldown to avoid rapid repeated flips.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class FishCollider : MonoBehaviour
{
    [Tooltip("Seconds between allowed flips to avoid rapid toggling")]
    public float flipCooldown = 0.25f;

    float lastFlipTime = -10f;

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (Time.time - lastFlipTime < flipCooldown) return;

        // If other object has TurtleMovementScript, flip both
        var turtle = collision.gameObject.GetComponent<TurtleMovementScript>();
        if (turtle != null)
        {
            var fish = GetComponent<FishMovementScript>();
            if (fish != null)
                fish.FlipDirection();

            turtle.FlipDirection();

            lastFlipTime = Time.time;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (Time.time - lastFlipTime < flipCooldown) return;

        var turtle = other.GetComponent<TurtleMovementScript>();
        if (turtle != null)
        {
            var fish = GetComponent<FishMovementScript>();
            if (fish != null)
                fish.FlipDirection();

            turtle.FlipDirection();

            lastFlipTime = Time.time;
        }
    }
}
