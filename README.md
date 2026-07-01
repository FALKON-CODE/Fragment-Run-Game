# FRAGMENT RUN

A simple 2D platformer built in Unity for a university project.

You play as **Fragment**, a cyber-being running through three short levels.
Move, jump, avoid the hazards and reach the exit door of each level.

## Controls

| Action | Key |
| ------ | --- |
| Move   | `A` / `D` or `←` / `→` |
| Jump   | `Space` / `W` / `↑` |
| Pause  | `Esc` |

## How to open

1. Install **Unity 2022.3 LTS** (the project was created with `2022.3.21f1`).
2. Open **Unity Hub → Add → select this project folder**.
3. Open the scene `Assets/Scenes/Level1.unity` and press **Play**,
   or open `Assets/Scenes/MainMenu.unity` to start from the main menu.

## Project structure

```
Assets/
  Scripts/      PlayerController, GameManager, LevelManager, UIManager
  Scenes/       MainMenu, Level1, Level2, Level3
  Resources/    Sprites and art loaded at runtime
Photos/         Source art used for backgrounds and characters
```

Levels are built from code in `LevelManager` to keep the scene files small and
easy to read. Each script has a single clear responsibility:

- **PlayerController** – movement, jumping and camera follow.
- **LevelManager** – builds the geometry for a level (ground, obstacles, exit).
- **GameManager** – game state: win, restart on fall, pause, scene loading.
- **UIManager** – main menu, pause menu and on-screen messages.

## Development phases (git history)

1. Core setup – player movement and basic physics.
2. Basic gameplay loop with level completion.
3. Levels 2 and 3 with obstacle progression.
4. Simple UI menus for navigation and game control.
5. Art integration and visual polish.
