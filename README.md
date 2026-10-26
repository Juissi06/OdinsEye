# Odin's Eye

One window for Valheim's debug and admin commands. Press **Home** instead of typing commands in the F5 console.

Every button runs the game's own console command, so it follows the game's rules:
- Cheats work in single player and in a world you host, once dev commands are on.
- On a dedicated server, admins get the admin tools (kick, ban, save); cheats stay off, like in the console.
- Before the first cheat, the window warns that the character will count as cheated for achievements.

## Status
Early: the window, the rules above and the Player tab (god, ghost, fly, no build cost, heal). Spawn, World, Teleport, Admin and Inspect tabs are next. See docs/PLAN.md.

## Keys
| Key | Action |
|---|---|
| Home | Open or close the window |
| Esc | Close the window |

## Installation
- Mod managers: automatic install
- Manual: put the DLL file in \Steam\steamapps\common\Valheim\BepInEx\plugins

## Requirements
- BepInEx
