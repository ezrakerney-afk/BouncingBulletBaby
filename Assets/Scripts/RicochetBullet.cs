using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public sealed class RicochetBullet : MonoBehaviour
{
    [SerializeField, Min(0.1f)] float speed = 16f;
    [SerializeField, Min(0)] int maxBounces = 12;
    [SerializeField, Min(0.1f)] float lifetime = 8f;
    Rigidbody2D body;
    Vector2 direction;
    BabyController shooter;
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
        shooter = owner != null ? owner.GetComponentInParent<BabyController>() : null;
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
            target.Hit(shooter);
            Destroy(gameObject);
            return;
        }
        if (bounces >= maxBounces || collision.contactCount == 0)
        {
            Destroy(gameObject);
            return;
        }
        direction = Vector2.Reflect(direction, collision.GetContact(0).normal).normalized;
        body.linearVelocity = direction * speed;
        bounces++;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        var target = other.GetComponentInParent<Target>();
        if (target == null) return;
        target.Hit(shooter);
        Destroy(gameObject);
    }
}
