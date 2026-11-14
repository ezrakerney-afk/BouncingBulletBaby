using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Level flow: hit every target (ricochets allowed) and the screen fades to black into the next level.
// Out of shots with a target still up restarts the level.
public sealed class LevelManager : MonoBehaviour
{
    [SerializeField] Revolver revolver;
    [SerializeField] BabyController baby;
    [SerializeField] bool restartWhenOutOfShots = true;
    [SerializeField] float restartDelay = 0.8f;
    [SerializeField] float nextLevelDelay = 0.4f;
    [SerializeField] float endScreenDelay = 3f;
    [SerializeField] float killY = -15f;

    public bool LevelComplete { get; private set; }
    public bool OutOfShots { get; private set; }
    public int ShotsLeft => revolver != null ? revolver.ShotsLeft : 0;
    public int LevelNumber => SceneManager.GetActiveScene().buildIndex + 1;
    public bool HasNextLevel => SceneManager.GetActiveScene().buildIndex >= 0 &&
        SceneManager.GetActiveScene().buildIndex + 1 < SceneManager.sceneCountInBuildSettings;
    public int TargetsRemaining { get; private set; }
    public string title;
    float restartAt = -1f;
    float nextAt = -1f;

    void Awake()
    {
        if (revolver == null) revolver = FindFirstObjectByType<Revolver>();
        if (baby == null) baby = FindFirstObjectByType<BabyController>();
        var targets = FindObjectsByType<Target>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var t in targets) if (t.gameObject.scene == gameObject.scene) TargetsRemaining++;
    }

    void OnEnable() { Target.OnHit += HandleTargetHit; }
    void OnDisable() { Target.OnHit -= HandleTargetHit; }

    void HandleTargetHit(Target hit)
    {
        if (LevelComplete || hit.gameObject.scene != gameObject.scene) return;
        TargetsRemaining--;
        if (TargetsRemaining > 0) return;
        LevelComplete = true;
        if (revolver != null) revolver.CanShoot = false;
        nextAt = Time.time + (HasNextLevel ? nextLevelDelay : endScreenDelay);
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        if (ScreenFade.Busy) return;
        if (keyboard != null && keyboard.rKey.wasPressedThisFrame) { RestartLevel(); return; }

        if (LevelComplete)
        {
            if (Time.time >= nextAt && !ScreenFade.Busy)
            {
                nextAt = float.PositiveInfinity;
                ScreenFade.LoadScene(HasNextLevel ? SceneManager.GetActiveScene().buildIndex + 1 : 0);
            }
            return;
        }

        if (baby != null && baby.transform.position.y < killY) RestartLevel();

        // The last bullet must finish its ricochets before declaring failure.
        if (restartWhenOutOfShots && revolver != null && ShotsLeft == 0 &&
            FindFirstObjectByType<RicochetBullet>() == null)
        {
            if (restartAt < 0f) { OutOfShots = true; restartAt = Time.time + restartDelay; }
            else if (Time.time >= restartAt) RestartLevel();
        }
    }

    public void RestartLevel() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
}
