using UnityEngine;

// Goal flag. Locked until the level's target is hit; then the baby has to reach it
// (usually with the diaper rocket) to finish the level.
[RequireComponent(typeof(Collider2D))]
public sealed class LevelExit : MonoBehaviour
{
    public static event System.Action<LevelExit> OnReached;
    [SerializeField] SpriteRenderer flag;
    [SerializeField] Color lockedColor = new Color(0.35f, 0.35f, 0.35f);
    [SerializeField] Color openColor = new Color(0.3f, 1f, 0.45f);
    public bool Open { get; private set; }

    void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
        if (flag == null) flag = GetComponentInChildren<SpriteRenderer>();
        SetOpen(false);
    }

    void OnEnable() => Target.OnHit += HandleTargetHit;
    void OnDisable() => Target.OnHit -= HandleTargetHit;
    void HandleTargetHit(Target t) { if (t.gameObject.scene == gameObject.scene) SetOpen(true); }

    void SetOpen(bool open)
    {
        Open = open;
        if (flag != null) flag.color = open ? openColor : lockedColor;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!Open || other.GetComponentInParent<BabyController>() == null) return;
        OnReached?.Invoke(this);
    }
}
