# Bouncing Bullet Baby

Bouncing Bullet Baby is a small ricochet puzzle game that Noah Puckett and Ezra Kerney made for a game jam. You play a baby in a cowboy hat who has somehow gotten hold of a revolver. The baby never leaves its spot in the corner of the room, so the only way to reach the targets is to bounce bullets off the walls.

## How it plays

Each level is a single room with one or more targets tucked behind walls, inside boxes or down narrow gaps. You aim with the mouse and click to fire, and the bullet keeps bouncing until it hits a target or runs out of steam. You only get a handful of bullets per level, and if you use them all while a target is still standing, the game is over and you can try that level again.

Hitting the last target takes you to the next room. If you clear a room without wasting a single bullet, the game tells you the run was perfect. There are ten rooms, and the later ones need three or four bounces to reach a target.

## Controls

You aim with the mouse and shoot with the left button. Pressing R restarts the current level, and Escape takes you back to the menu.

## Building it yourself

The project uses Unity 6000.6.9f1. The level scenes are not stored in the repository. They are generated from the text files in `Assets/Levels`, so after opening the project you need to choose **Bouncing Bullet Baby > Build Levels** from the menu bar once before pressing Play.

Each level file is a 30 by 18 grid of characters, where `#` is a wall, `B` is the baby and `T` is a target. This makes it quick to sketch a new room in a text editor. Since it is easy to draw a room that cannot actually be solved, there is a small Python script that fires a bullet at every angle and reports whether each target can be hit. You can run it with `python3 tools/levels.py`.

## Credits

Noah Puckett and Ezra Kerney designed and programmed the game. Ezra drew all of the art, including the font, and Leif Andresen wrote the music.

## License

The code is released under the MIT license. The full text is in the [LICENSE](LICENSE) file.
