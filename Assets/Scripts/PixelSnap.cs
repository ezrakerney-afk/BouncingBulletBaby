using UnityEngine;

// Snaps a sprite to the pixel grid while its parent moves smoothly. Optional square trail.
public sealed class PixelSnap : MonoBehaviour
{
    public int pixelsPerUnit = 32;
    public int trailLength = 4;
    public int trailSpacing = 6; // px between trail squares
    Transform[] trail;
    Vector3 lastDrop;
    int nextSlot;

    void Start()
    {
        var source = GetComponent<SpriteRenderer>();
        if (source == null || trailLength <= 0) return;
        trail = new Transform[trailLength];
        for (int i = 0; i < trailLength; i++)
        {
            var go = new GameObject("BulletTrail");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = source.sprite;
            sr.sharedMaterial = source.sharedMaterial;
            sr.sortingOrder = source.sortingOrder - 1;
            var c = source.color; c.a = 0.6f - 0.12f * i;
            sr.color = c;
            go.transform.localScale = transform.lossyScale * 0.5f;
            go.SetActive(false);
            trail[i] = go.transform;
        }
        lastDrop = Snapped();
    }

    Vector3 Snapped()
    {
        Vector3 p = transform.parent != null ? transform.parent.position : transform.position;
        return new Vector3(Mathf.Round(p.x * pixelsPerUnit) / pixelsPerUnit,
                           Mathf.Round(p.y * pixelsPerUnit) / pixelsPerUnit, p.z);
    }

    void LateUpdate()
    {
        Vector3 snapped = Snapped();
        transform.position = snapped;
        if (trail == null) return;
        float gap = (float)trailSpacing / pixelsPerUnit;
        if ((snapped - lastDrop).sqrMagnitude < gap * gap) return;
        var t = trail[nextSlot];
        t.position = lastDrop;
        t.gameObject.SetActive(true);
        nextSlot = (nextSlot + 1) % trail.Length;
        for (int i = 0; i < trail.Length; i++)
        {
            int age = (nextSlot - 1 - i + 2 * trail.Length) % trail.Length;
            var sr = trail[i].GetComponent<SpriteRenderer>();
            var c = sr.color; c.a = 0.6f - 0.12f * age;
            sr.color = c;
        }
        lastDrop = snapped;
    }

    void OnDestroy()
    {
        if (trail == null) return;
        foreach (var t in trail) if (t != null) Destroy(t.gameObject);
    }
}
