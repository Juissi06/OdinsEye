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
2. **Player and World tabs:** skills, status effects, game speed; time, weather, wind, raids, cleanup.
3. **Spawn browser:** the biggest piece. Icons, search, categories, favourites.
4. **Teleport:** map click, saved spots, locations, players.
5. **Admin tab:** area tools, player list with kick and ban, save.
6. **Inspect:** the small crosshair readout.
7. **Polish:** favourites, last used actions, test in single player, a hosted world and on a dedicated server (as admin and not).

## Open
- Overlap with Server devcommands (JereKuusela) and other cheat menus: detect them and say so in the log; the window keeps working since it goes through the console.
- Gamepad: later.
