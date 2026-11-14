using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Level flow: shoot the target (ricochets allowed) -> rocket unlocks + exit opens ->
// reach the exit -> level complete. Out of shots with the target still up -> restart.
public sealed class LevelManager : MonoBehaviour
{
    [SerializeField] Revolver revolver;
    [SerializeField] BabyController baby;
    [SerializeField] Target target;
    [SerializeField] LevelExit exit;
    [SerializeField] bool restartWhenOutOfShots = true;
    [SerializeField] float restartDelay = 0.8f;
    [SerializeField] float killY = -15f;

    public bool TargetHit { get; private set; }
    public bool LevelComplete { get; private set; }
    public bool OutOfShots { get; private set; }
    public int ShotsLeft => revolver != null ? revolver.ShotsLeft : 0;
    public string LevelName => SceneManager.GetActiveScene().name;
    public bool HasNextLevel => SceneManager.GetActiveScene().buildIndex >= 0 &&
        SceneManager.GetActiveScene().buildIndex + 1 < SceneManager.sceneCountInBuildSettings;
    float restartAt = -1f;

    void Awake()
    {
        if (revolver == null) revolver = FindFirstObjectByType<Revolver>();
        if (baby == null) baby = FindFirstObjectByType<BabyController>();
        if (target == null) target = FindFirstObjectByType<Target>();
        if (exit == null) exit = FindFirstObjectByType<LevelExit>();
    }

    void OnEnable() { Target.OnHit += HandleTargetHit; LevelExit.OnReached += HandleExit; }
    void OnDisable() { Target.OnHit -= HandleTargetHit; LevelExit.OnReached -= HandleExit; }

    void HandleTargetHit(Target hit)
    {
        if (TargetHit || hit.gameObject.scene != gameObject.scene) return;
        TargetHit = true;
        if (baby != null) baby.UnlockRocket();
        if (revolver != null) revolver.CanShoot = false;
        if (exit == null) Complete(); // levels without an exit finish on the hit
    }

    void HandleExit(LevelExit reached)
    {
        if (reached.gameObject.scene == gameObject.scene && TargetHit) Complete();
    }

    void Complete()
    {
        if (LevelComplete) return;
        LevelComplete = true;
        if (baby != null) baby.ControlsEnabled = false;
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.rKey.wasPressedThisFrame) { RestartLevel(); return; }

        if (LevelComplete)
        {
            if (keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame))
            {
                if (HasNextLevel) LoadNextLevel(); else LoadFirstLevel();
            }
            return;
        }

        if (baby != null && baby.transform.position.y < killY) RestartLevel();

        // The last bullet must finish its ricochets before declaring failure.
        if (!TargetHit && restartWhenOutOfShots && revolver != null && ShotsLeft == 0 &&
            FindFirstObjectByType<RicochetBullet>() == null)
        {
            if (restartAt < 0f) { OutOfShots = true; restartAt = Time.time + restartDelay; }
            else if (Time.time >= restartAt) RestartLevel();
        }
    }

    public void RestartLevel() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    public void LoadNextLevel() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    public void LoadFirstLevel() => SceneManager.LoadScene(0);
}
