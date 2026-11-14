using UnityEngine;

// Flies in a straight line and reflects off walls. Every wall sits on the tile grid,
// so the bounce normal is squared to the nearest axis for clean, predictable ricochets.
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public sealed class RicochetBullet : MonoBehaviour
{
    [SerializeField, Min(0.1f)] float speed = 16f;
    [SerializeField, Min(0)] int maxBounces = 12;
    [SerializeField, Min(0.1f)] float lifetime = 8f;
    Rigidbody2D body;
    Vector2 direction;
    int bounces;

    void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        body.gravityScale = 0;
        body.freezeRotation = true;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    public void Launch(Vector2 heading, Transform owner)
    {
        direction = heading.normalized;
        bounces = 0;
        if (owner != null)
            foreach (var ownCollider in owner.GetComponentsInChildren<Collider2D>())
                foreach (var bulletCollider in GetComponentsInChildren<Collider2D>())
                    Physics2D.IgnoreCollision(bulletCollider, ownCollider);
        body.linearVelocity = direction * speed;
        Destroy(gameObject, lifetime);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        var target = collision.collider.GetComponentInParent<Target>();
        if (target != null)
        {
            target.Hit();
            Destroy(gameObject);
            return;
        }
        if (bounces >= maxBounces || collision.contactCount == 0)
        {
            Destroy(gameObject);
            return;
        }
        Vector2 normal = collision.GetContact(0).normal;
        normal = Mathf.Abs(normal.x) > Mathf.Abs(normal.y)
            ? new Vector2(Mathf.Sign(normal.x), 0f)
            : new Vector2(0f, Mathf.Sign(normal.y));
        // Only bounce off a face the bullet is actually flying into.
        if (Vector2.Dot(direction, normal) < 0f)
            direction = Vector2.Reflect(direction, normal).normalized;
        body.linearVelocity = direction * speed;
        bounces++;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        var target = other.GetComponentInParent<Target>();
        if (target == null || target.IsHit) return;
        target.Hit();
        Destroy(gameObject);
    }
}
