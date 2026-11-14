using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public sealed class Revolver : MonoBehaviour
{
    [SerializeField] RicochetBullet bulletPrefab;
    [SerializeField] Transform muzzle;
    [SerializeField] Transform owner;
    [SerializeField] Camera aimCamera;
    [SerializeField, Min(1)] int capacity = 6;
    [SerializeField, Min(0f)] float shotInterval = 0.2f;
    public UnityEvent<int> onShotsChanged = new UnityEvent<int>();
    public UnityEvent onFired = new UnityEvent();
    public int ShotsLeft { get; private set; }
    public bool CanShoot { get; set; } = true;
    float nextShot;

    void Awake()
    {
        if (owner == null) owner = transform.root;
        if (aimCamera == null) aimCamera = Camera.main;
        Reload();
    }

    void Update()
    {
        var mouse = Mouse.current;
        if (mouse == null || aimCamera == null) return;
        Ray ray = aimCamera.ScreenPointToRay(mouse.position.ReadValue());
        var plane = new Plane(Vector3.forward, transform.position);
        if (!plane.Raycast(ray, out float distance)) return;
        Vector2 direction = ray.GetPoint(distance) - transform.position;
        if (direction.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        if (mouse.leftButton.wasPressedThisFrame) TryFire(direction);
    }

    public bool TryFire(Vector2 direction)
    {
        if (!CanShoot || ShotsLeft <= 0 || Time.time < nextShot || bulletPrefab == null || direction.sqrMagnitude < 0.001f) return false;
        var bullet = Instantiate(bulletPrefab, SpawnPoint(), Quaternion.identity);
        bullet.Launch(direction.normalized, owner);
        ShotsLeft--;
        nextShot = Time.time + shotInterval;
        onShotsChanged.Invoke(ShotsLeft);
        onFired.Invoke();
        return true;
    }

    // The bullet starts at the muzzle, unless a wall sits between the gun and the muzzle:
    // then it starts at the gun itself so it can never spawn inside a wall.
    Vector2 SpawnPoint()
    {
        if (muzzle == null) return transform.position;
        foreach (var hit in Physics2D.LinecastAll(transform.position, muzzle.position))
        {
            if (hit.collider == null || hit.collider.isTrigger) continue;
            if (owner != null && hit.collider.transform.IsChildOf(owner)) continue;
            return transform.position;
        }
        return muzzle.position;
    }

    public void Reload()
    {
        ShotsLeft = Mathf.Max(1, capacity);
        onShotsChanged.Invoke(ShotsLeft);
    }
}
