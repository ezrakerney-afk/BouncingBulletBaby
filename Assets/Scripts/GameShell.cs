using UnityEngine;
using UnityEngine.InputSystem;

// Music, sound effects and the crosshair cursor. Esc goes to the menu, or quits from it.
public sealed class GameShell : MonoBehaviour
{
    public AudioClip music, gunshot, wallHit;
    public Texture2D cursor;
    const int CursorSize = 2; // game px per cursor px
    static AudioSource player, effects;
    static AudioClip shotClip, hitClip;

    void OnEnable() { if (cursor != null) Cursor.visible = false; }
    void OnDisable() { Cursor.visible = true; }

    void OnGUI()
    {
        var mouse = Mouse.current;
        if (cursor == null || mouse == null || Event.current.type != EventType.Repaint) return;
        GUI.depth = -2000; // on top of the fade
        int pixel = PixelUI.Scale * CursorSize;
        Vector2 p = mouse.position.ReadValue();
        float x = Mathf.Round(p.x - cursor.width * pixel / 2f);
        float y = Mathf.Round(Screen.height - p.y - cursor.height * pixel / 2f);
        GUI.DrawTexture(new Rect(x, y, cursor.width * pixel, cursor.height * pixel), cursor);
    }

    void Awake()
    {
        if (gunshot != null) shotClip = gunshot;
        if (wallHit != null) hitClip = wallHit;
        if (player != null) return;
        var go = new GameObject("Audio");
        DontDestroyOnLoad(go);
        // level cameras have no listener
        if (FindFirstObjectByType<AudioListener>() == null) go.AddComponent<AudioListener>();
        player = go.AddComponent<AudioSource>();
        player.clip = music;
        player.loop = true;
        player.volume = 0.5f;
        if (music != null) player.Play();
        effects = go.AddComponent<AudioSource>();
    }

    public static void PlayGunshot() { if (effects != null && shotClip != null) effects.PlayOneShot(shotClip, 0.8f); }

    public static void PlayWallHit() { if (effects != null && hitClip != null) effects.PlayOneShot(hitClip, 0.7f); }

    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null || !keyboard.escapeKey.wasPressedThisFrame || ScreenFade.Busy) return;
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == ScreenUI.MenuScene) Application.Quit();
        else ScreenFade.LoadScene(ScreenUI.MenuScene);
    }
}
