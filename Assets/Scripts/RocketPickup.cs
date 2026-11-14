using UnityEngine;

public sealed class RocketPickup : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other)
    {
        var baby = other.GetComponentInParent<BabyController>();
        if (baby == null) return;
        baby.UnlockRocket();
        gameObject.SetActive(false);
    }
}
