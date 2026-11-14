using UnityEngine;

// Simple placeholder motion shared by the mobile, teddy and its shields.
public sealed class GreyboxMotion : MonoBehaviour
{
    public Vector2 amplitude;
    public float frequency = 1f;
    public float offset;
    public float rotationSpeed;
    public int phase;
    public bool throwBlocks;
    public GameObject blockPrefab;
    Vector3 origin;
    LevelManager level;
    float nextThrow;
    void Start() { origin = transform.localPosition; level = FindFirstObjectByType<LevelManager>(); }
    void Update()
    {
        bool active = phase == 0 || (level != null && level.BossPhase == phase);
        foreach (var sr in GetComponentsInChildren<SpriteRenderer>()) sr.enabled = active;
        foreach (var col in GetComponentsInChildren<Collider2D>()) col.enabled = active;
        if (!active) return;
        transform.localPosition = origin + (Vector3)(amplitude * Mathf.Sin(Time.time * frequency + offset));
        transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);
        if (throwBlocks && blockPrefab != null && Time.time >= nextThrow)
        {
            nextThrow = Time.time + 2.5f;
            var block = Instantiate(blockPrefab, transform.position + Vector3.left * 4f + Vector3.down * 2f, Quaternion.identity);
            block.SetActive(true);
            block.GetComponent<Rigidbody2D>().linearVelocity = new Vector2(-7, 3);
            Destroy(block, 5f);
        }
    }
}
