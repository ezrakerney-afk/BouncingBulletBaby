using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Fades the whole screen to black, loads a level, then fades back in. Lives across level loads.
public sealed class ScreenFade : MonoBehaviour
{
    const float FadeTime = 0.35f;
    static ScreenFade instance;
    float alpha;
    bool busy;

    public static bool Busy => instance != null && instance.busy;

    public static void LoadScene(int buildIndex)
    {
        if (instance == null)
        {
            var go = new GameObject("ScreenFade");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<ScreenFade>();
        }
        if (!instance.busy) instance.StartCoroutine(instance.Run(buildIndex));
    }

    IEnumerator Run(int buildIndex)
    {
        busy = true;
        for (float t = 0f; t < FadeTime; t += Time.unscaledDeltaTime)
        {
            alpha = t / FadeTime;
            yield return null;
        }
        alpha = 1f;
        yield return null;
        SceneManager.LoadScene(buildIndex);
        yield return null;
        yield return null;
        busy = false;
        for (float t = 0f; t < FadeTime; t += Time.unscaledDeltaTime)
        {
            alpha = 1f - t / FadeTime;
            yield return null;
        }
        alpha = 0f;
    }

    void OnGUI()
    {
        if (alpha <= 0f || Event.current.type != EventType.Repaint) return;
        GUI.depth = -1000;
        var previous = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, alpha);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = previous;
    }
}
