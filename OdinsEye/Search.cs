using System.Collections.Generic;
using UnityEngine;

namespace OdinsEye
{
    // The search box in the title bar: finds any feature by name (or a word for it) and jumps to its tab.
    internal static class Search
    {
        class Entry
        {
            internal Tab Tab;
            internal string Name, Words;
        }

        const float BoxW = 220f, ResultH = 26f, MaxResults = 9;
        internal const string Control = "OdinsEye_Search";

        internal static string Text = "";
        static readonly List<Entry> Found = new List<Entry>();
        static string _foundFor;

        static readonly Entry[] All =
        {
            E(Tab.Player, "God mode", "invincible damage"), E(Tab.Player, "Ghost mode", "invisible creatures ignore"),
            E(Tab.Player, "Fly", "flying debugfly"), E(Tab.Player, "No build cost", "nocost free materials workbench"),
            E(Tab.Player, "Heal to full", "health"), E(Tab.Player, "Achievements even with cheats", "achievement"),
            E(Tab.Player, "Size: giant or tiny", "scale big small"), E(Tab.Player, "Super jump", "jump height"),
            E(Tab.Player, "Speed", "run walk swim fast"), E(Tab.Player, "Ship speed", "sailing boat rowing"),
            E(Tab.Player, "Infinite stamina", "stamina"), E(Tab.Player, "No weight limit", "carry encumbered overweight"),
            E(Tab.Player, "No wear", "durability repair break"), E(Tab.Player, "No fall damage", "falling"),
            E(Tab.Player, "Night vision", "dark light cave"), E(Tab.Player, "One-hit kills", "kill damage"),
            E(Tab.Player, "Super knockback", "push stagger"), E(Tab.Player, "Instant crafting", "craft fast upgrade"),
            E(Tab.Player, "Fun cheats back to normal", "reset default"),
            E(Tab.Skills, "Skill levels", "raiseskill set level"), E(Tab.Skills, "Reset skills", "resetskill"),
            E(Tab.Skills, "Learn faster", "xp experience multiplier skill speed"), E(Tab.Skills, "Learning speed per skill", "xp experience"),
            E(Tab.Skills, "Keep skills when you die", "death penalty skill loss"),
            E(Tab.Buffs, "Status effects", "buffs addstatus rested wet burning mead"), E(Tab.Buffs, "Clear all effects", "clearstatus debuff"),
            E(Tab.Buffs, "Guardian power", "setpower boss power forsaken cooldown"), E(Tab.Buffs, "Empty stomach", "puke food"),
            E(Tab.Spawn, "Spawn items", "give item"), E(Tab.Spawn, "Spawn creatures", "monster animal"),
            E(Tab.Spawn, "Spawn tamed", "tame pet"), E(Tab.Spawn, "Spawn bosses", "boss"), E(Tab.Spawn, "Item sets", "itemset gear"),
            E(Tab.Spawn, "Favourites and recent spawns", "favorite"),
            E(Tab.World, "Time of day", "tod morning night clock"), E(Tab.World, "Skip time", "skiptime sleep"),
            E(Tab.World, "Weather", "env rain storm"), E(Tab.World, "Wind", "direction"), E(Tab.World, "Game speed", "timescale slow"),
            E(Tab.World, "Raids", "event randomevent stopevent"), E(Tab.World, "Kill enemies", "killall clean up"),
            E(Tab.World, "Remove drops", "removedrops items fish birds"), E(Tab.World, "No monster spawns", "nospawn"),
            E(Tab.World, "Boss progress", "keys defeated"), E(Tab.World, "World modifiers", "difficulty combat resources raids portals death penalty"),
            E(Tab.World, "World presets", "casual hardcore immersive hammer reset default"), E(Tab.World, "World keys", "setkey removekey global key"),
            E(Tab.World, "Unlock all recipes and building pieces", "unlock recipes pieces buildings crafting known"),
            E(Tab.Teleport, "Pick a spot on the map", "goto map teleport"), E(Tab.Teleport, "Back", "return previous teleport"),
            E(Tab.Teleport, "Saved spots", "waypoint home"), E(Tab.Teleport, "Traders", "haldor hildir bog witch vendor"),
            E(Tab.Teleport, "Boss altars", "findtp boss location"), E(Tab.Teleport, "Locations", "findtp dungeon"),
            E(Tab.Teleport, "Nearest biome", "findbiometp"), E(Tab.Teleport, "Go to or bring players", "recall"),
            E(Tab.Creatures, "Tame creatures", "tame"), E(Tab.Creatures, "Make neutrals angry", "aggravate"),
            E(Tab.Creatures, "Kill tamed creatures", "killtame"), E(Tab.Creatures, "Passive monsters", "passivemobs calm peaceful"),
            E(Tab.Creatures, "Freeze creatures", "freeze ai stop"), E(Tab.Creatures, "Creatures nearby", "list untame kill"),
            E(Tab.Admin, "Save the world", "save"), E(Tab.Admin, "Kick and ban players", "kick ban"),
            E(Tab.Admin, "Banned list and unban", "unban"), E(Tab.Admin, "Difficulty scale", "players difficulty"),
            E(Tab.Admin, "Portals and map rules", "noportals nomap"), E(Tab.Admin, "Reveal or forget the map", "explore resetmap"),
            E(Tab.Admin, "Delete around you", "forcedelete remove"), E(Tab.Admin, "Fuel", "setfuel fires"),
            E(Tab.Admin, "Put out fires", "stopfire smoke"), E(Tab.Admin, "Repair your gear", "repairall"),
            E(Tab.Admin, "Repair buildings", "wear damage"),
            E(Tab.Inspect, "Inspect what you look at", "prefab owner health"),
            E(Tab.Custom, "Your own buttons", "custom command bind"),
        };

        static Entry E(Tab tab, string name, string words) => new Entry { Tab = tab, Name = name, Words = (name + " " + words + " " + tab).ToLowerInvariant() };

        internal static Rect Box(Rect title) => new Rect(title.xMax - BoxW, title.y + 2f, BoxW, title.height - 6f);

        internal static void DrawBox(Rect r)
        {
            GUI.SetNextControlName(Control);
            Text = GUI.TextField(r, Text ?? "", 30);
            if (string.IsNullOrEmpty(Text) && GUI.GetNameOfFocusedControl() != Control)
                Ui.Label(new Rect(r.x + 8f, r.y, r.width - 8f, r.height), $"<color={Ui.Muted}>Search features...</color>", Ui.Small);
        }

        internal static bool Focused => GUI.GetNameOfFocusedControl() == Control;

        internal static bool Showing => !string.IsNullOrEmpty(Text?.Trim());

        // The results drop down under the box, over the tab's content. Picking one opens its tab.
        internal static void DrawResults(Rect box)
        {
            string q = Text.Trim().ToLowerInvariant();
            if (q != _foundFor)
            {
                _foundFor = q;
                Found.Clear();
                foreach (var e in All)
                    if (Matches(e.Words, q) && Menu.IsShown(e.Tab)) Found.Add(e);
            }
            int shown = Mathf.Min(Found.Count, (int)MaxResults);
            var panel = ResultsRect(box);
            Ui.Panel(panel);
            if (Found.Count == 0)
            {
                Ui.Label(new Rect(panel.x + 10f, panel.y + 6f, panel.width - 20f, ResultH), $"<color={Ui.Muted}>Nothing found.</color>", Ui.Small);
                return;
            }
            for (int i = 0; i < shown; i++)
            {
                var e = Found[i];
                var row = new Rect(panel.x + 6f, panel.y + 6f + i * ResultH, panel.width - 12f, ResultH - 2f);
                if (GUI.Button(row, GUIContent.none))
                {
                    Menu.Show(e.Tab);
                    Text = "";
                    GUIUtility.keyboardControl = 0;
                }
                Ui.Label(new Rect(row.x + 8f, row.y, row.width - 100f, row.height), e.Name, Ui.Small);
                Ui.Label(new Rect(row.xMax - 96f, row.y, 90f, row.height), $"<color={Ui.Gold}>{e.Tab}</color>", Ui.SmallRight);
            }
        }

        internal static Rect ResultsRect(Rect box)
        {
            int shown = Mathf.Clamp(Found.Count, 1, (int)MaxResults);
            return new Rect(box.x - 120f, box.yMax + 4f, box.width + 120f, shown * ResultH + 12f);
        }

        // Every word typed must appear somewhere in the entry's name or words.
        static bool Matches(string words, string query)
        {
            foreach (var part in query.Split(' '))
                if (part.Length > 0 && !words.Contains(part)) return false;
            return true;
        }
    }
}
