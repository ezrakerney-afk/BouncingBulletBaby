using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public sealed class BabyController : MonoBehaviour
{
    [SerializeField] float moveSpeed = 6f;
    [SerializeField] float jumpSpeed = 10f;
    [SerializeField] LayerMask groundLayers = ~0;
    Rigidbody2D body;
    Collider2D shape;
    readonly RaycastHit2D[] groundHits = new RaycastHit2D[8];
    float move;
    bool jump;
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
        if (!ControlsEnabled || keyboard == null) return;
        move = (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1f : 0f)
             - (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1f : 0f);
        jump |= keyboard.spaceKey.wasPressedThisFrame;
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
        if (ControlsEnabled && jump && grounded) velocity.y = jumpSpeed;
        jump = false;
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
