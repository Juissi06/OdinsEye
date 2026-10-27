# Odin's Eye

One window for Valheim's debug and admin commands. Press **Home** instead of typing commands in the F5 console.

Every button runs the game's own console command, so it follows the game's rules:
- Cheats work in single player and in a world you host, once dev commands are on.
- On a dedicated server, admins get the admin tools (kick, ban, save); cheats stay off, like in the console.
- Before the first cheat, the window warns that the character will count as cheated for achievements.

## Tabs
- **Player:** god, ghost, fly, no build cost, heal, and the game's own switch that turns achievements back on after using cheats. Fun cheats: giant or tiny, super jump, super speed, infinite stamina, no weight limit, no wear, no fall damage, night vision, one-hit kills, super knockback, instant crafting and ship speed.
- **Skills:** every skill with its level, modded skills too. Set any skill to any level, raise or reset them all. Learn skills up to 25 times faster, all of them or each skill on its own, and keep your skills when you die.
- **Buffs:** every status effect in the game with its icon, to add or clear. Pick any guardian power with its cooldown reset, and empty your stomach to try other food.
- **Spawn:** every item and creature with icons, categories and search. Pick an amount and quality (or stars for creatures) and spawn it in front of you or straight into your inventory, or creatures already tamed. Bosses ask first. Premade item sets too.
- **World:** time of day, skip time, weather, wind, game speed, raids, kill enemies and clean up drops (asks first), no monster spawns, grow every plant around you at once, boss progress, world modifiers and presets, unlock all recipes and building pieces, every world key.
- **Teleport:** pick a spot on the map, go back to where you were before your last teleport, saved spots for each world, shortcuts to Haldor, Hildir, the Bog Witch and the boss altars, every location with search, the nearest biome of each kind, go to or bring other players.
- **Creatures:** tame, make neutrals angry, kill tames, passive monsters, freeze every creature, and a list of creatures nearby to tame, untame or kill one by one.
- **Admin:** save the world, kick and ban players, banned list and unban (server admins), difficulty scale, portals and map on or off, reveal or forget the map, delete everything around you (asks first, no undo), set fuel, put out fires, repair your gear and the buildings around you.
- **Inspect:** what's in the middle of your view: name, prefab, position, owner, builder, health, creature level and what it's after. Copy the prefab name or spawn another.
- **Custom:** your own buttons: a name and one or more console lines, e.g. "Get 50 wood", with suggestions as you type.

Favourites and recent spawns live at the top of the Spawn tab. Search for any feature from the box in the title bar. Hover any button to see the console command it runs, and drag the window by its title to move it.

## Keys
| Key | Action |
|---|---|
| Home | Open or close the window |
| Esc | Close the window |

Hotkeys for god mode, ghost mode, fly, no build cost and Back are in the config, unbound until you set them.

## Installation
- Mod managers: automatic install
- Manual: put the DLL file in \Steam\steamapps\common\Valheim\BepInEx\plugins

## Requirements
- BepInEx
