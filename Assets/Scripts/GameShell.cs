using UnityEngine;
using UnityEngine.InputSystem;

// Lives across level loads: loops the soundtrack. Esc goes back to the home screen, or quits from there.
public sealed class GameShell : MonoBehaviour
{
    public AudioClip music;
    static AudioSource player;

    void Awake()
    {
        if (player != null || music == null) return;
        var go = new GameObject("Music");
        DontDestroyOnLoad(go);
        // The level cameras have no AudioListener, so the music object carries the one listener for the whole game.
        if (FindFirstObjectByType<AudioListener>() == null) go.AddComponent<AudioListener>();
        player = go.AddComponent<AudioSource>();
        player.clip = music;
        player.loop = true;
        player.volume = 0.6f;
        player.Play();
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null || !keyboard.escapeKey.wasPressedThisFrame || ScreenFade.Busy) return;
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == ScreenUI.MenuScene) Application.Quit();
        else ScreenFade.LoadScene(ScreenUI.MenuScene);
    }
}
