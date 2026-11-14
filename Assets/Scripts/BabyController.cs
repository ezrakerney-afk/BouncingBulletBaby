using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public sealed class BabyController : MonoBehaviour
{
    [SerializeField] float moveSpeed = 6f;
    [SerializeField] float jumpSpeed = 10f;
    [SerializeField, Min(0f)] float coyoteTime = 0.1f;
    [SerializeField, Min(0f)] float jumpBufferTime = 0.12f;
    [SerializeField] LayerMask groundLayers = ~0;
    Rigidbody2D body;
    Collider2D shape;
    readonly RaycastHit2D[] groundHits = new RaycastHit2D[8];
    float move;
    float jumpBufferedUntil = float.NegativeInfinity;
    float lastGroundedTime = float.NegativeInfinity;
    bool jumpConsumed;
    public bool ControlsEnabled { get; set; } = true;
    public bool RocketUnlocked { get; private set; }
    public UnityEngine.Events.UnityEvent onRocketUnlocked = new UnityEngine.Events.UnityEvent();

    void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        shape = GetComponent<Collider2D>();
        body.freezeRotation = true;
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        move = 0f;
        if (!ControlsEnabled || keyboard == null)
        {
            jumpBufferedUntil = float.NegativeInfinity;
            lastGroundedTime = float.NegativeInfinity;
            return;
        }
        move = (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1f : 0f)
             - (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1f : 0f);
        if (keyboard.spaceKey.wasPressedThisFrame)
            jumpBufferedUntil = Time.time + jumpBufferTime;
    }

    void FixedUpdate()
    {
        var velocity = body.linearVelocity;
        velocity.x = ControlsEnabled ? move * moveSpeed : 0f;
        var filter = new ContactFilter2D();
        filter.SetLayerMask(groundLayers);
        filter.useTriggers = false;
        int count = shape.Cast(Vector2.down, filter, groundHits, 0.08f);
        bool grounded = false;
        for (int i = 0; i < count; i++) grounded |= groundHits[i].normal.y > 0.5f;
        if (grounded && velocity.y <= 0f)
        {
            lastGroundedTime = Time.time;
            jumpConsumed = false;
        }
        if (ControlsEnabled && !jumpConsumed && Time.time <= jumpBufferedUntil
            && Time.time - lastGroundedTime <= coyoteTime)
        {
            velocity.y = jumpSpeed;
            jumpConsumed = true;
            jumpBufferedUntil = float.NegativeInfinity;
        }
        body.linearVelocity = velocity;
    }

    public void UnlockRocket()
    {
        if (RocketUnlocked) return;
        RocketUnlocked = true;
        onRocketUnlocked.Invoke();
    }

    public void ApplyImpulse(Vector2 impulse) => body.AddForce(impulse, ForceMode2D.Impulse);
}
