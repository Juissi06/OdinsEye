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
| **Player** | god, ghost, fly, no build cost, heal, achievements switch |
| **Skills** | every skill: set its level, reset; learn faster, keep skills on death |
| **Buffs** | status effects with icons, guardian powers, food |
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
   - [x] Hover previews the command; drag the window by its title (saved); toggles no longer smear into a bar; footer shows ">" instead of "&gt;".
   - [x] Thunderstore icon and Nexus thumbnail (art/, rendered from art/*.html with headless Edge).
   - [ ] Test in single player, a hosted world, and on a dedicated server as admin and not.

## Roadmap
Two kinds of features. **From the console:** the game already has a command, so the window only needs buttons and
follows the game's rules for free. **New cheats:** Odin's Eye's own code. Same rule as everything else: they only work
where the game allows cheats (single player, or a world you host, with dev commands on and confirmed), and they switch
off with dev commands.

### Skills and XP (done, 0.2: Skills tab)
- [x] **Skill levels** (console): every skill with its level, set it with a slider or reset it (`raiseskill` by the difference, `resetskill`). Only the game's own skills: the console can't find skills added by other mods.
- [x] **XP multiplier** (new): learn x2 to x25 faster, for all skills (patch on `Skills.RaiseSkill`). Per skill: later, if asked.
- [x] **Keep skills on death** (new): skips `Skills.OnDeath`, and the wipe from the "skills reset on death" world modifier.

### Buffs and status effects (done, 0.2: Buffs tab)
- [x] **Buffs** (console): every status effect with an icon, with search; add one or clear all (`addstatus`, `clearstatus`). The game has no command to remove just one.
- [x] **Guardian power** (console): pick any boss power and reset its cooldown (`setpower`).
- [x] **Food** (console): what you've eaten and how long it lasts, empty your stomach (`puke`).

### Fun cheats (new; done in 0.2 on the Player tab)
- [x] **Giant and Tiny:** scale your character from 0.25x to 4x. Whether other players see it depends on whether the game syncs the player's scale; not checked yet.
- [x] **Super jump** (with **no fall damage** as its own switch), **super speed** (walk, run, crouch and swim), **super knockback** on your hits, **one-hit kills** (creatures only, never players).
- [x] **Infinite stamina**, **no carry weight limit**, **no wear** (everything you carry stays at full durability).
- [x] **Night vision:** brighter ambient light at night and in caves.
- [x] **Instant crafting:** no crafting time at the workbench and other stations. No materials: the game's own "no build cost" already covers crafting.

### Sailing (new; done in 0.2)
- [x] **Ship speed** slider in the Fun column: more sail and rowing force on the ship you steer, put back when you let go. Odin's Eye and Seaworthy are separate mods for separate purposes, so no special handling for Seaworthy.

### Creatures (console and new; done in 0.2, Creatures tab)
- [x] **Tame** (console: `tame`; the game tames every loaded tameable, not only nearby ones), **spawn creatures already tamed** (Spawn tab, not bosses).
- [x] **Calm or angry:** "Passive monsters" switch (the game's `PassiveMobs` world key) and "Make neutral creatures angry" (`aggravate`). **Freeze creatures** (new, session only).
- [x] **Spawn bosses** with a warning (Spawn tab asks first).
- [x] **Nearby list** (new): creatures within 40 m with stars, tamed, health and distance; tame, untame or kill each one.

### World (done in 0.2)
- [x] **World modifiers** (console): each modifier with its options, the presets, back to default (`setworldmodifier`, `setworldpreset`, `resetworldkeys`). Only the host. The window doesn't show which option is current yet.
- [x] **All world keys** (console): every current key with Remove, and set any key with matching names to click (`setkey`, `removekey`).
- [x] **Repair buildings around you** (new, Admin tab): the hammer's repair on every damaged piece within 30 m.

### Smaller extras (done in 0.2)
- [x] **Learning speed per skill**, on top of the speed for all skills ("Skill speed per skill" in the config).
- [x] **Modded skills** are listed and can be set; their buttons are Odin's Eye's own since the console can't find them.

### Player and character
- **Appearance** (console): change hair, beard and body model on the spot (`hair`, `beard`, `model`).
- **Bigger inventory** (console: `inventorysize`), with a warning that items may drop if you shrink it.
- [x] **Back** (Teleport tab and a hotkey): back to where you were before the last teleport, portals and dungeon doors included.

### The window itself
- [x] **Your own buttons** (Custom tab): any console line, or several separated by ";", as a named button. Kept in BepInEx/config/OdinsEye.buttons.txt.
- [x] **Hotkeys** for god, fly, ghost, no cost and Back, without opening the window. Unbound by default.
- [x] **Hide tabs** you don't use, and an option to skip the "are you sure" questions (config).
- **Compact mode:** a small always-on bar of your favourite switches while you play.

## Open
- Overlap with Server devcommands (JereKuusela) and other cheat menus: detect them and say so in the log; the window keeps working since it goes through the console.
- Gamepad: later.
