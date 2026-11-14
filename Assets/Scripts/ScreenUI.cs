using UnityEngine;
using UnityEngine.InputSystem;

// The three full screens around the levels: home (play button), game over, and the end screen.
// Play and Exit are Ezra's button art; click them, or press Enter for Play.
public sealed class ScreenUI : MonoBehaviour
{
    public enum Kind { Menu, GameOver, End }
    public const string MenuScene = "Menu", GameOverScene = "GameOver", EndScene = "End";
    const int FirstLevel = 1; // build index of Level01; the menu is index 0
    const int ButtonSize = 6, FaceSize = 4;

    // Level to go back to when Play is pressed on the game over screen.
    public static int RetryScene = FirstLevel;

    public Kind kind;
    public Texture2D face, playButton, exitButton;

    Rect PlayBox => ButtonBox(playButton, kind == Kind.Menu ? 356 : 380);
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
        p.y = Screen.height - p.y; // GUI space has y pointing down
        return PixelUI.ToScreen(box.x, box.y, box.width, box.height).Contains(p);
    }

    void Update()
    {
        if (ScreenFade.Busy) return;
        var mouse = Mouse.current;
        var keyboard = Keyboard.current;
        bool click = mouse != null && mouse.leftButton.wasPressedThisFrame;
        bool enter = keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame);
        if (enter || (click && Hover(PlayBox))) Play();
        else if (click && Hover(ExitBox)) Exit();
    }

    void Play()
    {
        if (kind == Kind.GameOver) ScreenFade.LoadScene(RetryScene);
        else ScreenFade.LoadScene(FirstLevel);
    }

    void Exit()
    {
        // On the home screen Exit closes the game; on the other screens it goes back home.
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
            PixelUI.TextCentered(22, "Bouncing Bullet Baby", 3, paper);
            PixelUI.Image(face, faceX, 104, FaceSize, Color.white);
            PixelUI.TextCentered(280, "Bank your shots off the walls", 2, paper);
        }
        else if (kind == Kind.GameOver)
        {
            PixelUI.TextCentered(60, "Game Over", 5, paper);
            PixelUI.TextCentered(200, "Out of bullets", 3, paper);
            PixelUI.TextCentered(290, "Play tries the level again", 2, paper);
        }
        else
        {
            PixelUI.TextCentered(16, "You beat it", 4, paper);
            PixelUI.Image(face, faceX, 108, FaceSize, Color.white);
            PixelUI.TextCentered(274, "Thanks for playing", 2, paper);
            PixelUI.TextCentered(322, "Art by Ezra    Music by Noah", 2, paper);
        }

        Button(playButton, PlayBox);
        Button(exitButton, ExitBox);
    }

    static void Button(Texture2D tex, Rect box)
    {
        bool hover = Hover(box);
        // A hovered button lifts by two game pixels and brightens.
        PixelUI.Image(tex, box.x, box.y - (hover ? 2 : 0), ButtonSize, hover ? Color.white : new Color(0.88f, 0.88f, 0.88f));
    }
}
