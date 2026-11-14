# Bouncing Bullet Baby

A wild west ricochet puzzle game made for a game jam.

You are a baby with a revolver, standing your ground in the same spot every level. Bullets bounce off walls. Bank your shots around the room to hit every target, and the screen fades to black into the next level.

## Controls

- Mouse: aim, left click to shoot
- R: restart the level
- Esc: back to the home screen (quits from there)

## Project notes

- Unity 6000.6.9f1. Run **Bouncing Bullet Baby > Build Levels** once to generate the scenes.
- Levels are text maps in `Assets/Levels` (30 x 18 tiles): first line `NAME|SHOTS`, then `#` wall, `.` empty, `B` baby, `T` target.
- `python3 tools/levels.py` checks every level is beatable.
- 32 px per unit, 960x540 view. Art goes in `Assets/Art`.

## Credits

- Development: Noah and Ezra
- Art: Ezra
- Music: Leif Andresen
