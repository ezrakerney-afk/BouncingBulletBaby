using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider2D))]
public sealed class Target : MonoBehaviour
{
    public static event System.Action<Target> OnHit;
    public UnityEvent onHit = new UnityEvent();
    public bool IsHit { get; private set; }

    public void Hit()
    {
        if (IsHit) return;
        IsHit = true;
        onHit.Invoke();
        OnHit?.Invoke(this);
        gameObject.SetActive(false);
    }
}
