# Odin's Eye: plan

One Valheim-style window (Home) for the game's debug and admin commands, instead of typing them in the F5 console.

## Rules
- **Every button runs the game's own console command** (`Terminal.TryRunCommand`). Same permission checks, same achievement warning, nothing breaks quietly when the game updates: a command that no longer exists hides its button. The footer shows the command and what the game answered, so you learn the console as you go.
- **Follow the game's rules.** Cheats need dev commands on and being the server or host (`Terminal.IsCheatsEnabled`), so: everything in single player or a world you host; on a dedicated server, even as an admin, only the admin commands (kick, ban, save). If a mod like Server devcommands grants more, the window follows, since it asks the game per command (`ConsoleCommand.IsValid`).
- **Achievements:** the game won't run a cheat until the character has accepted being "cheated" (`confirmcheats`). The window says so plainly and asks before the first cheat.
- **Destructive actions ask first** (kill all, reset map, delete).
- Separate mod from Norse Sense, which stays vanilla-balanced. UI code copied from Norse Sense so they look alike.

## Tabs
| Tab | Contents |
|---|---|
| **Player** | god, ghost, fly, no build cost, heal, set skill levels, add or clear status effects, clear inventory, game speed |
| **Spawn** | searchable item and creature browser with icons: amount, quality, star level, spawn where you look or into your inventory, starter item sets |
| **World** | time of day slider, skip time, weather list, wind, start and stop raids, kill enemies, remove drops, fish and birds, boss progress (world keys) |
| **Teleport** | click the map to go there, saved spots, find a location and jump to it, bring players to you |
| **Admin** | repair everything nearby, delete what you look at, set fuel, stop fire and smoke, map and portal rules, player list with kick and ban, save the world |
| **Inspect** | small: what's under the crosshair (prefab name, owner, health, position, saved data). Not a focus. |

## Steps
1. [x] **Skeleton:** repo, window with tabs, command runner, permission states, achievement warning, Player tab toggles as a first real use.
2. [x] **World tab:** time (presets, slider, real time, skip), weather list, wind, game speed, raids, clean-up with confirmation, no monster spawns, boss progress. Player tab extras (skills, status effects) still to do.
3. [x] **Spawn browser:** categories, search by name or prefab, icon grid (only visible rows drawn), amount, quality or stars, straight into inventory, creatures, premade item sets. Favourites still to do.
4. [x] **Teleport:** pick a spot on the large map, saved spots per world (BepInEx/config/OdinsEye.spots.txt), trader and boss shortcuts, all locations with search, nearest biome, go to or bring players.
5. [x] **Admin tab:** save, banned list, unban, players with kick and ban (by platform ID, admins only), difficulty scale, portals and map rules, reveal or forget the map, clear table data, delete around you with radius and name filter (asks first), fuel, fires and smoke, repair gear. The game has no "repair buildings" command, so that one isn't here.
6. [x] **Inspect:** what is in the middle of your view behind the window (the camera stands still while it is open): name, prefab, kind, position and distance, network id and owner, builder, health, creature level, faction, tamed, alerted and target. Copy the prefab name, spawn another.
7. **Polish:**
   - [x] Spawn favourites (kept in the config) and recently spawned (this session).
   - [x] The game's refusals show in red in the footer; console colour tags stripped so they stay readable.
   - [x] Log a note when a command mod (Server devcommands and the like) is installed.
   - [ ] Test in single player, a hosted world, and on a dedicated server as admin and not.

## Open
- Overlap with Server devcommands (JereKuusela) and other cheat menus: detect them and say so in the log; the window keeps working since it goes through the console.
- Gamepad: later.
