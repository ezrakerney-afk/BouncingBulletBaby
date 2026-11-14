using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider2D))]
public sealed class Target : MonoBehaviour
{
    public static event System.Action<Target> OnHit;
    [SerializeField] BabyController baby;
    public UnityEvent onHit = new UnityEvent();
    public bool IsHit { get; private set; }
    [Min(0)] public int requiredBounces;
    public bool requiresCeilingBounce;
    public bool penetrate;

    public bool AcceptsBullet(int bounces, bool ceiling) =>
        !IsHit && bounces >= requiredBounces && (!requiresCeilingBounce || ceiling);

    public void Hit(BabyController shooter = null)
    {
        if (IsHit) return;
        IsHit = true;
        var recipient = shooter != null ? shooter : baby;
        if (recipient != null) recipient.UnlockRocket();
        onHit.Invoke();
        OnHit?.Invoke(this);
        gameObject.SetActive(false);
    }
}
