using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Hit every target to clear the level. Running dry with a target still up is game over.
public sealed class LevelManager : MonoBehaviour
{
    [SerializeField] Revolver revolver;
    [SerializeField] float gameOverDelay = 0.8f;
    [SerializeField] float nextLevelDelay = 1f;
    [SerializeField] float bannerTime = 0.9f;

    public string title;
    public bool LevelComplete { get; private set; }
    public int TargetsRemaining { get; private set; }
    public int ShotsLeft => revolver != null ? revolver.ShotsLeft : 0;
    public int LevelNumber => SceneManager.GetActiveScene().buildIndex; // menu is scene 0

    // "Bullseye" on a hit, "Perfect" when the level is cleared without a wasted bullet.
    public string Banner => Time.time < bannerUntil ? banner : null;

    string banner;
    float bannerUntil;
    int targetCount, startingShots;
    float gameOverAt = -1f, nextAt = -1f;

    void Awake()
    {
        if (revolver == null) revolver = FindFirstObjectByType<Revolver>();
        foreach (var t in FindObjectsByType<Target>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (t.gameObject.scene == gameObject.scene) targetCount++;
        TargetsRemaining = targetCount;
    }

    void Start() => startingShots = ShotsLeft;

    void OnEnable() => Target.OnHit += OnTargetHit;
    void OnDisable() => Target.OnHit -= OnTargetHit;

    void OnTargetHit(Target hit)
    {
        if (LevelComplete || hit.gameObject.scene != gameObject.scene) return;
        TargetsRemaining--;
        banner = "Bullseye";
        bannerUntil = Time.time + bannerTime;
        if (TargetsRemaining > 0) return;

        LevelComplete = true;
        if (revolver != null) revolver.CanShoot = false;
        if (startingShots - ShotsLeft == targetCount) banner = "Perfect";
        bannerUntil = Time.time + nextLevelDelay + 1f;
        nextAt = Time.time + nextLevelDelay;
    }

    void Update()
    {
        if (ScreenFade.Busy) return;
        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.rKey.wasPressedThisFrame)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            return;
        }

        if (LevelComplete)
        {
            if (Time.time >= nextAt)
            {
                nextAt = float.PositiveInfinity;
                ScreenFade.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
            }
            return;
        }

        // wait for the last bullet to stop bouncing before calling it
        if (ShotsLeft > 0 || FindFirstObjectByType<RicochetBullet>() != null) return;
        if (gameOverAt < 0f) gameOverAt = Time.time + gameOverDelay;
        else if (Time.time >= gameOverAt)
        {
            gameOverAt = float.PositiveInfinity;
            ScreenUI.RetryScene = SceneManager.GetActiveScene().buildIndex;
            ScreenFade.LoadScene(ScreenUI.GameOverScene);
        }
    }
}
