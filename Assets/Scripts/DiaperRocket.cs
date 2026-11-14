using UnityEngine;
using UnityEngine.InputSystem;

// Run after BabyController so walking does not erase horizontal boost velocity.
[DefaultExecutionOrder(100)]
[RequireComponent(typeof(BabyController), typeof(Rigidbody2D))]
public sealed class DiaperRocket : MonoBehaviour
{
    [SerializeField, Min(0.01f)] float boostSpeed = 15f;
    [SerializeField, Min(0.01f)] float boostDuration = 0.2f;
    [SerializeField, Min(0f)] float cooldown = 0.8f;
    [SerializeField] Camera aimCamera;
    BabyController baby;
    Rigidbody2D body;
    Vector2 boostDirection;
    float remaining;
    float nextBoost;
    public bool Unlocked => baby != null && baby.RocketUnlocked;
    public bool IsBoosting => remaining > 0f;

    void Awake()
    {
        baby = GetComponent<BabyController>();
        body = GetComponent<Rigidbody2D>();
        if (aimCamera == null) aimCamera = Camera.main;
    }

    void Update()
    {
        var mouse = Mouse.current;
        if (mouse == null || !mouse.rightButton.wasPressedThisFrame) return;
        Vector2 direction = Vector2.up;
        if (aimCamera != null)
        {
            var ray = aimCamera.ScreenPointToRay(mouse.position.ReadValue());
            var plane = new Plane(Vector3.forward, transform.position);
            if (plane.Raycast(ray, out float distance))
                direction = ray.GetPoint(distance) - transform.position;
        }
        TryBoost(direction);
    }

    public bool TryBoost(Vector2 direction)
    {
        if (!Unlocked || !baby.ControlsEnabled || Time.time < nextBoost ||
            direction.sqrMagnitude < 0.001f) return false;
        boostDirection = direction.normalized;
        remaining = boostDuration;
        nextBoost = Time.time + boostDuration + cooldown;
        return true;
    }

    void FixedUpdate()
    {
        if (remaining <= 0f) return;
        if (!baby.ControlsEnabled) { remaining = 0f; return; }
        body.linearVelocity = boostDirection * boostSpeed;
        remaining -= Time.fixedDeltaTime;
    }

    void OnDisable() => remaining = 0f;
}
