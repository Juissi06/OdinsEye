using System;
using UnityEngine;

namespace OdinsEye
{
    // Player tab: the everyday cheats as on/off switches that show the current state.
    internal static class PlayerTab
    {
        // Label, console command, and how to read the current state from the player.
        class Toggle
        {
            internal string Label, Command, Hint;
            internal Func<Player, bool> IsOn;
        }

        static readonly Toggle[] Toggles =
        {
            new Toggle { Label = "God mode", Command = "god", Hint = "no damage", IsOn = p => p.InGodMode() },
            new Toggle { Label = "Ghost mode", Command = "ghost", Hint = "creatures ignore you", IsOn = p => p.InGhostMode() },
            new Toggle { Label = "Fly", Command = "fly", Hint = "Space up, Ctrl down", IsOn = p => p.IsDebugFlying() },
            new Toggle { Label = "No build cost", Command = "nocost", Hint = "build without materials or workbench", IsOn = p => p.NoCostCheat() },
        };

        const float RowH = 34f;
        const string Bypass = "yesiuseddevcommandsbutiwantmyachievementsanyway";

        internal static void Draw(Rect r)
        {
            var player = Player.m_localPlayer;
            if (player == null) return;
            float y = r.y;

            Ui.Label(new Rect(r.x, y, r.width, 26f), $"<color={Ui.Gold}>Cheats</color>", Ui.Text);
            y += 30f;
            foreach (var t in Toggles)
            {
                if (!Commands.Exists(t.Command)) continue;
                bool on = t.IsOn(player);
                bool can = Commands.CanRun(t.Command);
                GUI.enabled = can;
                bool clicked = GUI.Toggle(new Rect(r.x, y + 6f, 220f, 24f), on, t.Label);
                GUI.enabled = true;
                Ui.Label(new Rect(r.x + 230f, y, r.width - 230f, RowH), $"<color={Ui.Muted}>{t.Hint}</color>", Ui.Small);
                if (clicked != on) Commands.Run(t.Command);
                y += RowH;
            }

            y += 10f;
            GUI.enabled = Commands.CanRun("heal");
            if (Commands.Exists("heal") && GUI.Button(new Rect(r.x, y, 220f, 30f), "Heal to full")) Commands.Run("heal");
            GUI.enabled = true;
            y += 44f;

            // The game's own escape hatch (PlayerProfile.s_bypassCheatChecks, saved on the character). While it's on,
            // Achievements.CanGetAchievements lets achievements through even for a cheated character or world.
            if (!Commands.Exists(Bypass)) return;
            Ui.Label(new Rect(r.x, y, r.width, 26f), $"<color={Ui.Gold}>Achievements</color>", Ui.Text);
            y += 30f;
            bool bypass = PlayerProfile.s_bypassCheatChecks;
            Widgets.Switch(new Rect(r.x, y + 6f, 300f, 24f), "Achievements even with cheats", bypass, Bypass + " 1", Bypass + " 0");
            y += RowH;
            GUI.Label(new Rect(r.x, y, r.width, 40f),
                $"<color={Ui.Muted}>The game's own option: while it's on you can earn achievements again, even on a character that used cheats. " +
                "The cheat mark stays and still shows in the achievements list.</color>", Widgets.Wrap);
        }
    }
}
