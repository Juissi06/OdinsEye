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
            new Toggle { Label = "No build cost", Command = "nocost", Hint = "no materials or workbench", IsOn = p => p.NoCostCheat() },
        };

        const float RowH = 34f, LeftW = 420f, Gap = 24f;
        const string Bypass = "yesiuseddevcommandsbutiwantmyachievementsanyway";

        internal static void Draw(Rect r)
        {
            var player = Player.m_localPlayer;
            if (player == null) return;
            DrawFun(new Rect(r.x + LeftW + Gap, r.y, r.width - LeftW - Gap, r.height));
            r.width = LeftW;
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
            GUI.Label(new Rect(r.x, y, r.width, 70f),
                $"<color={Ui.Muted}>The game's own option: while it's on you can earn achievements again, even on a character that used cheats. " +
                "The cheat mark stays and still shows in the achievements list.</color>", Widgets.Wrap);
        }

        // ---------- Fun: Odin's Eye's own cheats ----------

        static void DrawFun(Rect r)
        {
            float x = r.x, w = r.width, y = r.y;
            Ui.Label(new Rect(x, y, w, 26f), $"<color={Ui.Gold}>Fun</color>", Ui.Text);
            y += 30f;
            bool was = GUI.enabled;
            GUI.enabled = was && Cheats.Allowed;
            Slider(x, ref y, w, "Size", Plugin.FunSize, 0.25f, 4f, 0.05f);
            Slider(x, ref y, w, "Jump", Plugin.FunJump, 1f, 10f, 0.5f);
            Slider(x, ref y, w, "Speed", Plugin.FunSpeed, 1f, 5f, 0.25f);
            Slider(x, ref y, w, "Ship", Plugin.FunShip, 1f, 5f, 0.25f);
            GUI.enabled = was;
            y += 6f;

            float half = (w - 8f) * 0.5f;
            Switch(new Rect(x, y, half, 24f), "Infinite stamina", Plugin.FunStamina);
            Switch(new Rect(x + half + 8f, y, half, 24f), "No weight limit", Plugin.FunNoWeight);
            y += 28f;
            Switch(new Rect(x, y, half, 24f), "No wear", Plugin.FunNoWear);
            Switch(new Rect(x + half + 8f, y, half, 24f), "No fall damage", Plugin.FunNoFall);
            y += 28f;
            Switch(new Rect(x, y, half, 24f), "Night vision", Plugin.FunNightVision);
            Switch(new Rect(x + half + 8f, y, half, 24f), "One-hit kills", Plugin.FunOneHit);
            y += 28f;
            Switch(new Rect(x, y, half, 24f), "Super knockback", Plugin.FunKnockback);
            Switch(new Rect(x + half + 8f, y, half, 24f), "Instant crafting", Plugin.FunInstantCraft);
            y += 34f;

            string note = Cheats.Allowed
                ? "Odin's Eye's own cheats: they stop when dev commands are off. Others see your normal size."
                : "Odin's Eye's own cheats: they work once cheats are on.";
            GUI.Label(new Rect(x, y, w, r.yMax - y), $"<color={Ui.Muted}>{note}</color>", Widgets.Wrap);
        }

        // Label with the value, a slider snapped to steps (so the config isn't rewritten every pixel), and a reset.
        static void Slider(float x, ref float y, float w, string label, BepInEx.Configuration.ConfigEntry<float> entry, float min, float max, float step)
        {
            float value = entry.Value;
            Ui.Label(new Rect(x, y, 80f, 24f), $"{label} <color={Ui.Gold}>x{value:0.##}</color>", Ui.Small);
            float now = GUI.HorizontalSlider(new Rect(x + 84f, y + 6f, w - 84f - 58f, 16f), value, min, max);
            now = Mathf.Clamp(Mathf.Round(now / step) * step, min, max);
            if (GUI.Button(new Rect(x + w - 50f, y, 50f, 24f), new GUIContent("x1", Widgets.OwnCheat))) now = 1f;
            if (!Mathf.Approximately(now, value)) entry.Value = now;
            y += 30f;
        }

        static void Switch(Rect r, string label, BepInEx.Configuration.ConfigEntry<bool> entry)
        {
            bool now = Widgets.CheatSwitch(r, label, entry.Value);
            if (now != entry.Value) entry.Value = now;
        }
    }
}
