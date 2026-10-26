# Odin's Eye

One window for Valheim's debug and admin commands. Press **Home** instead of typing commands in the F5 console.

Every button runs the game's own console command, so it follows the game's rules:
- Cheats work in single player and in a world you host, once dev commands are on.
- On a dedicated server, admins get the admin tools (kick, ban, save); cheats stay off, like in the console.
- Before the first cheat, the window warns that the character will count as cheated for achievements.

## Status
Early. Working tabs:
- **Player:** god, ghost, fly, no build cost, heal.
- **Spawn:** every item and creature with icons, categories and search. Pick an amount and quality (or stars for creatures) and spawn it in front of you or straight into your inventory. Premade item sets too.
- **World:** time of day, skip time, weather, wind, game speed, raids, kill enemies and clean up drops (asks first), no monster spawns, boss progress.

Teleport, Admin and Inspect are next. See docs/PLAN.md.

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
