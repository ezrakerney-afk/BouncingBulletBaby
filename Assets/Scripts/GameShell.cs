using UnityEngine;
using UnityEngine.InputSystem;

// Lives across level loads: loops the soundtrack and lets Esc quit the standalone game.
public sealed class GameShell : MonoBehaviour
{
    public AudioClip music;
    static AudioSource player;

    void Awake()
    {
        if (player != null || music == null) return;
        var go = new GameObject("Music");
        DontDestroyOnLoad(go);
        player = go.AddComponent<AudioSource>();
        player.clip = music;
        player.loop = true;
        player.volume = 0.6f;
        player.Play();
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) Application.Quit();
    }
}
