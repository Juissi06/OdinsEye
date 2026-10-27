# Changelog

## Unreleased

## 0.3.0
- World: "Speed up within 30 m": grow plants (crops, saplings and seedlings finish growing; ones that can't grow where they stand are left alone and counted), finish production (smelters, kilns, furnaces, spinning wheels, windmills and refineries hand out everything queued, fermenters are ready to tap), cook or burn the food on cooking stations and ovens, fill beehives and sap collectors, regrow picked berries, mushrooms, thistle and flax.
- Creatures: grow up young wolves, boars, lox and askvins, and heal every tame nearby.
- Inspect: set the stars (0 to 4) of the creature you look at; your coordinates, a tape measure between two marked spots, and a count of pieces, items, creatures and objects around you.
- Admin: undo terrain changes in a radius (digging, raising, paths, cultivation) and clear snow off pieces. Repairing buildings also covers ships and carts.

## 0.2.1
- All building pieces: the build menu updates right away when it's switched on or off, instead of after you re-equip the hammer.
- Custom tab: a command typed in full stays the first suggestion, so Tab adds the space instead of turning "setkey" into "setkeyplayer" or "time" into "timescale".
- Custom tab: commands work in any case ("Spawn Wood"), as in the game's console.
- Custom tab: a misspelled command earlier on the line is still pointed out while you type the next one, and "Add button" stays off until every command on the line exists.
- Custom tab: suggestions say when a command needs dev commands or can't run here; names containing what you typed show after the ones starting with it.
- Custom tab: no slowdown after "spawn " while thousands of names match, no errors from a command with an empty option, and the cursor no longer jumps to the end later on.
- World keys: suggestions sorted the same way as the console line's.
- Delete around you: the question now names exactly what will be deleted. The game's forcedelete takes one word and removes every object whose name contains it, so "wood wall" is sent as "wood_wall" instead of just "wood".
- Bring only brings that player: it's greyed out when the game's recall can't pick them out because another player's name contains theirs.
- Back keeps the height, so it returns you inside a dungeon, and only forgets the spot once the teleport really happened (pressing it twice quickly or right after a portal no longer loses it).
- Custom buttons with capitals ("Spawn Wood") can be clicked.
- Scrolling a list in the window no longer zooms the camera behind it.
- Ship speed works when someone else came aboard first: the one steering takes the ship over.
- Tame works on creatures another player nearby was controlling.
- Teleport tab: the location list is only filtered again when the search changes.

## 0.2.0
New tabs
- Skills: every skill with its level, modded skills too. Set any level, step it up or down, max or reset it, or all skills at once.
- Buffs: every status effect with its icon and search, add one or clear them all. Guardian powers with the cooldown reset, your food and "empty stomach".
- Creatures: tame, make neutrals angry, kill tames, passive monsters, freeze creatures, and the creatures nearby to tame, untame or kill one by one.
- Custom: your own buttons from any console line, or several separated by ";". Suggestions while you type: commands with what they do, then their options (items, skills, biomes...). Tab takes the first one.

Odin's Eye's own cheats (only while the game allows cheats; "Back to normal" turns them all off)
- Fun: size (x0.25 to x4), jump, speed, ship speed, infinite stamina, no weight limit, no wear, no fall damage, night vision, one-hit kills, super knockback, instant crafting.
- Learn faster (x2 to x25), for all skills or each skill on its own, and keep skills when you die.

More
- Search box in the title bar: type any feature ("fly", "weather", "tame") and jump to its tab.
- World tab: world modifiers and presets like the world creation screen, unlock all recipes and building pieces, and every world key to see, set or remove.
- Spawn creatures already tamed; bosses ask before spawning.
- Teleport tab: Back to where you were before your last teleport (portals and dungeon doors too).
- Admin tab: repair every building within 30 m.
- Hotkeys for god mode, ghost mode, fly, no build cost and Back (unbound until you set them in the config).
- Config: hide tabs you don't use, and turn off the "are you sure" questions.
- Inspect names the ground "Terrain" with its biome.

Fixes
- The mouse wheel no longer zooms the camera while the window is open.
- Switches that are on show green, and no longer leave a grey bar after a click.
- The footer no longer keeps an old hint after you click a button, and clicked buttons don't stay highlighted.
- Notes wrap instead of getting cut off; tabs size to their names.
- Custom buttons that can't run say why in the footer.

## 0.1.1
- The achievements switch is now called "Achievements even with cheats" and says what it really does: while it's on you can earn achievements again, even on a character that used cheats.

## 0.1.0
- Player tab: switch for the game's "yesiuseddevcommandsbutiwantmyachievementsanyway", so cheated items and pieces count for achievements again.
- Hover a button to see the console command it runs.
- Drag the window by its title; it remembers where you put it.
- Inspect tab: what's in the middle of your view, with its prefab, owner, health and more.
- Spawn favourites and recent spawns.
- The game's refusals show in red in the footer.
- Teleport tab: pick a spot on the map, saved spots per world, trader and boss shortcuts, location search, nearest biome, go to or bring players.
- Admin tab: save, kick, ban, unban, banned list, difficulty scale, portal and map rules, reveal or forget the map, delete around you, fuel, fires and smoke, repair gear.
- Spawn tab: items and creatures with icons, categories and search; amount, quality or stars; into your inventory or in front of you; premade item sets.
- World tab: time presets and slider, real time, skip time, weather, wind, game speed, raids, clean-up, no monster spawns, boss progress.
- Destructive actions ask first.
- First version: the Odin's Eye window (Home) with tabs, the game's command rules and achievement warning, and the Player tab: god, ghost, fly, no build cost, heal.
