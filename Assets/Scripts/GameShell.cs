using UnityEngine;
using UnityEngine.InputSystem;

// Lives across level loads: loops the soundtrack and plays the sound effects.
// Esc goes back to the home screen, or quits from there.
public sealed class GameShell : MonoBehaviour
{
    public AudioClip music, gunshot, wallHit;
    static AudioSource player, effects;
    static AudioClip shotClip, hitClip;

    void Awake()
    {
        if (gunshot != null) shotClip = gunshot;
        if (wallHit != null) hitClip = wallHit;
        if (player != null) return;
        var go = new GameObject("Audio");
        DontDestroyOnLoad(go);
        // The level cameras have no AudioListener, so this object carries the one listener for the whole game.
        if (FindFirstObjectByType<AudioListener>() == null) go.AddComponent<AudioListener>();
        player = go.AddComponent<AudioSource>();
        player.clip = music;
        player.loop = true;
        player.volume = 0.5f;
        if (music != null) player.Play();
        effects = go.AddComponent<AudioSource>();
    }

    // Gunshot: firing the revolver, and clicking a menu button.
    public static void PlayGunshot() { if (effects != null && shotClip != null) effects.PlayOneShot(shotClip, 0.8f); }

    // Wall hit: a bullet bouncing off a wall, and the mouse moving onto a menu button.
    public static void PlayWallHit() { if (effects != null && hitClip != null) effects.PlayOneShot(hitClip, 0.7f); }

    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null || !keyboard.escapeKey.wasPressedThisFrame || ScreenFade.Busy) return;
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == ScreenUI.MenuScene) Application.Quit();
        else ScreenFade.LoadScene(ScreenUI.MenuScene);
    }
}
