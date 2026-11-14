using UnityEngine;

// Picks the baby frame closest to the aim angle, flipped when aiming left.
public sealed class BabyAimSprite : MonoBehaviour
{
    public SpriteRenderer body;
    public Transform gun;
    public Sprite[] frames;
    public float[] angles;

    void LateUpdate()
    {
        if (body == null || gun == null || frames == null || angles == null || frames.Length == 0) return;
        float radians = gun.eulerAngles.z * Mathf.Deg2Rad;
        body.flipX = Mathf.Cos(radians) < 0f;
        float elevation = Mathf.Max(0f, Mathf.Asin(Mathf.Sin(radians)) * Mathf.Rad2Deg);
        int best = 0;
        for (int i = 1; i < frames.Length && i < angles.Length; i++)
            if (Mathf.Abs(angles[i] - elevation) < Mathf.Abs(angles[best] - elevation)) best = i;
        if (body.sprite != frames[best]) body.sprite = frames[best];
    }
}
