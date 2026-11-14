using UnityEngine;
using UnityEngine.InputSystem;

// Menu, game over and end screens.
public sealed class ScreenUI : MonoBehaviour
{
    public enum Kind { Menu, GameOver, End }
    public const string MenuScene = "Menu", GameOverScene = "GameOver", EndScene = "End";
    const int FirstLevel = 1; // menu is scene 0
    const int ButtonSize = 6, FaceSize = 4;

    public static int RetryScene = FirstLevel;

    int lastHovered;
    public Kind kind;
    public Texture2D face, playButton, exitButton;

    Rect PlayBox => ButtonBox(playButton, kind == Kind.Menu ? 356 : 386);
    Rect ExitBox => ButtonBox(exitButton, kind == Kind.Menu ? 418 : 442);

    static Rect ButtonBox(Texture2D tex, int y)
    {
        int width = (tex != null ? tex.width : 20) * ButtonSize, height = (tex != null ? tex.height : 8) * ButtonSize;
        return new Rect((PixelUI.RefWidth - width) / 2, y, width, height);
    }

    static bool Hover(Rect box)
    {
        var mouse = Mouse.current;
        if (mouse == null) return false;
        Vector2 p = mouse.position.ReadValue();
        p.y = Screen.height - p.y; // gui y is down
        return PixelUI.ToScreen(box.x, box.y, box.width, box.height).Contains(p);
    }

    void Update()
    {
        if (ScreenFade.Busy) return;
        var mouse = Mouse.current;
        var keyboard = Keyboard.current;
        bool click = mouse != null && mouse.leftButton.wasPressedThisFrame;
        bool enter = keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame);

        int hovered = Hover(PlayBox) ? 1 : Hover(ExitBox) ? 2 : 0;
        if (hovered != 0 && hovered != lastHovered) GameShell.PlayWallHit();
        lastHovered = hovered;

        if (enter || (click && hovered == 1)) { GameShell.PlayGunshot(); Play(); }
        else if (click && hovered == 2) { GameShell.PlayGunshot(); Exit(); }
    }

    void Play()
    {
        if (kind == Kind.GameOver) ScreenFade.LoadScene(RetryScene);
        else ScreenFade.LoadScene(FirstLevel);
    }

    void Exit()
    {
        if (kind == Kind.Menu) Application.Quit();
        else ScreenFade.LoadScene(MenuScene);
    }

    void OnGUI()
    {
        if (Event.current.type != EventType.Repaint) return;
        var paper = PixelUI.Paper;
        int faceWidth = face != null ? face.width * FaceSize : 0, faceHeight = face != null ? face.height * FaceSize : 0;
        int faceX = (PixelUI.RefWidth - faceWidth) / 2;

        if (kind == Kind.Menu)
        {
            PixelUI.TextCentered(22, "Bouncing Bullet Baby", 3, PixelUI.Ink, false);
            PixelUI.Image(face, faceX, 104, FaceSize, Color.white);
            PixelUI.TextCentered(280, "Bank your shots off the walls", 2, PixelUI.Ink, false);
        }
        else if (kind == Kind.GameOver)
        {
            PixelUI.TextCentered(60, "Game Over", 5, paper);
            PixelUI.TextCentered(200, "Out of bullets", 3, paper);
            PixelUI.TextCentered(290, "Play tries the level again", 2, paper);
        }
        else
        {
            PixelUI.TextCentered(8, "You beat it", 4, paper);
            PixelUI.Image(face, faceX, 96, FaceSize, Color.white);
            PixelUI.TextCentered(260, "Development by Noah and Ezra", 2, paper);
            PixelUI.TextCentered(298, "Art by Ezra", 2, paper);
            PixelUI.TextCentered(336, "Music by Leif Andresen", 2, paper);
        }

        Button(playButton, PlayBox);
        Button(exitButton, ExitBox);
    }

    static void Button(Texture2D tex, Rect box)
    {
        bool hover = Hover(box);
        PixelUI.Image(tex, box.x, box.y - (hover ? 2 : 0), ButtonSize, hover ? Color.white : new Color(0.88f, 0.88f, 0.88f));
    }
}
