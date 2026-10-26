using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace OdinsEye
{
    // World tab: time, weather, wind, game speed, raids, clean-up and boss progress. Two columns in a scroll view.
    internal static class WorldTab
    {
        const float Gap = 24f, ListH = 104f;

        // Boss keys in progression order: the world keys that unlock raids, the trader's items and so on.
        static readonly string[] BossKeys =
            { "defeated_eikthyr", "defeated_gdking", "defeated_bonemass", "defeated_dragon", "defeated_goblinking", "defeated_queen", "defeated_fader" };
        static readonly string[] BossNames = { "Eikthyr", "The Elder", "Bonemass", "Moder", "Yagluth", "The Queen", "Fader" };

        static Vector2 _scroll, _envScroll, _eventScroll;
        static float _tod = 0.5f, _windAngle, _windStrength = 0.5f, _speed = 1f;
        static float _contentH = 600f;

        internal static void Draw(Rect r)
        {
            var view = new Rect(0f, 0f, r.width - 20f, _contentH);
            _scroll = GUI.BeginScrollView(r, _scroll, view);
            float colW = (view.width - Gap) * 0.5f;
            float left = Left(0f, colW), right = Right(colW + Gap, colW);
            _contentH = Mathf.Max(left, right) + 8f;
            GUI.EndScrollView();
        }

        // ---------- Time, weather, wind ----------

        static float Left(float x, float w)
        {
            float y = 0f;
            var env = EnvMan.instance;

            Widgets.Header(x, ref y, w, "Time");
            if (env != null)
            {
                float f = env.GetDayFraction();
                int minutes = Mathf.FloorToInt(f * 24f * 60f);
                Widgets.Note(x, ref y, w, $"Day {env.GetDay()}, {minutes / 60:00}:{minutes % 60:00}" +
                                          (env.m_debugTimeOfDay ? "  (time frozen by tod)" : ""));
            }
            Widgets.CommandRow(x, ref y, w, new[] { "Morning", "Noon", "Evening", "Midnight" },
                new[] { "tod 0.27", "tod 0.5", "tod 0.72", "tod 0" });
            _tod = GUI.HorizontalSlider(new Rect(x, y + 8f, w - 190f, 16f), _tod, 0f, 1f);
            int todMin = Mathf.FloorToInt(_tod * 24f * 60f);
            Widgets.Command(new Rect(x + w - 180f, y, 90f, Widgets.RowH - 4f), $"{todMin / 60:00}:{todMin % 60:00}", "tod " + F(_tod));
            Widgets.Command(new Rect(x + w - 84f, y, 84f, Widgets.RowH - 4f), "Real time", "tod -1");
            y += Widgets.RowH;
            long day = env != null ? env.m_dayLengthSec : 1200L;
            Widgets.CommandRow(x, ref y, w, new[] { "+1 hour", "+6 hours", "+1 day" },
                new[] { "skiptime " + day / 24, "skiptime " + day / 4, "skiptime " + day });

            y += 6f;
            Widgets.Header(x, ref y, w, "Weather");
            if (env != null)
            {
                string current = env.GetCurrentEnvironment()?.m_name ?? "?";
                Widgets.Note(x, ref y, w, string.IsNullOrEmpty(env.m_debugEnv) ? $"Now: {current}" : $"Forced: {env.m_debugEnv}");
                y = List(x, y, w, ref _envScroll, env.m_environments.Count, i => env.m_environments[i].m_name, name => "env " + name,
                    name => name == env.m_debugEnv);
            }
            Widgets.CommandRow(x, ref y, w, new[] { "Back to normal weather" }, new[] { "resetenv" });

            y += 6f;
            Widgets.Header(x, ref y, w, "Wind");
            Ui.Label(new Rect(x, y, 80f, 22f), $"Direction {_windAngle:0}°", Ui.Small);
            _windAngle = GUI.HorizontalSlider(new Rect(x + 100f, y + 6f, w - 100f, 16f), _windAngle, 0f, 360f);
            y += 24f;
            Ui.Label(new Rect(x, y, 80f, 22f), $"Strength {_windStrength * 100f:0}%", Ui.Small);
            _windStrength = GUI.HorizontalSlider(new Rect(x + 100f, y + 6f, w - 100f, 16f), _windStrength, 0f, 1f);
            y += 26f;
            Widgets.CommandRow(x, ref y, w, new[] { "Set wind", "Normal wind" },
                new[] { $"wind {F(_windAngle)} {F(_windStrength)}", "resetwind" });

            y += 6f;
            y = Modifiers(x, y, w);
            return y;
        }

        // ---------- Speed, raids, clean-up, bosses ----------

        static float Right(float x, float w)
        {
            float y = 0f;

            Widgets.Header(x, ref y, w, "Game speed");
            _speed = GUI.HorizontalSlider(new Rect(x, y + 8f, w - 190f, 16f), _speed, 0.1f, 3f);
            Widgets.Command(new Rect(x + w - 180f, y, 90f, Widgets.RowH - 4f), $"{_speed:0.0}x", "timescale " + F(_speed));
            Widgets.Command(new Rect(x + w - 84f, y, 84f, Widgets.RowH - 4f), "Normal", "timescale 1");
            y += Widgets.RowH + 6f;

            Widgets.Header(x, ref y, w, "Raids");
            var events = RandEventSystem.instance;
            if (events != null)
            {
                var active = events.GetActiveEvent();
                Widgets.Note(x, ref y, w, active != null ? $"Active: {active.m_name}" : "No raid right now");
                y = List(x, y, w, ref _eventScroll, events.m_events.Count, i => events.m_events[i].m_name, name => "event " + name,
                    name => active != null && active.m_name == name);
            }
            Widgets.CommandRow(x, ref y, w, new[] { "Random raid", "Stop raid" }, new[] { "randomevent", "stopevent" });

            y += 6f;
            Widgets.Header(x, ref y, w, "Clean up nearby");
            float half = (w - 6f) * 0.5f;
            Widgets.Command(new Rect(x, y, half, Widgets.RowH - 4f), "Kill enemies", "killenemies", "Kill every enemy nearby?");
            Widgets.Command(new Rect(x + half + 6f, y, half, Widgets.RowH - 4f), "Kill all creatures", "killall", "Kill every creature nearby, tames included?");
            y += Widgets.RowH;
            Widgets.CommandRow(x, ref y, w, new[] { "Remove drops", "Remove fish", "Remove birds" },
                new[] { "removedrops", "removefish", "removebirds" });
            Widgets.Switch(new Rect(x, y + 4f, w, 24f), "No monster spawns", SpawnSystem.m_nospawn, "nospawn on", "nospawn off");
            y += Widgets.RowH + 6f;

            Widgets.Header(x, ref y, w, "Boss progress");
            Widgets.Note(x, ref y, w, "World keys: unlock raids and the trader's items");
            var zones = ZoneSystem.instance;
            for (int i = 0; i < BossKeys.Length; i++)
            {
                bool on = zones != null && zones.GetGlobalKey(BossKeys[i]);
                float cx = x + (i % 2) * (half + 6f);
                Widgets.Switch(new Rect(cx, y + 4f, half, 24f), BossNames[i], on, "setkey " + BossKeys[i], "removekey " + BossKeys[i]);
                if (i % 2 == 1 || i == BossKeys.Length - 1) y += 28f;
            }

            // The game's own world settings that unlock everything, as on the world creation screen.
            y += 6f;
            Widgets.Header(x, ref y, w, "Unlocks");
            bool recipes = zones != null && zones.GetGlobalKey(GlobalKeys.AllRecipesUnlocked);
            bool pieces = zones != null && zones.GetGlobalKey(GlobalKeys.AllPiecesUnlocked);
            Widgets.Switch(new Rect(x, y + 4f, half, 24f), "All recipes", recipes, "setkey AllRecipesUnlocked", "removekey AllRecipesUnlocked");
            Widgets.Switch(new Rect(x + half + 6f, y + 4f, half, 24f), "All building pieces", pieces, "setkey AllPiecesUnlocked", "removekey AllPiecesUnlocked");
            y += 30f;
            Widgets.Note(x, ref y, w, "For everyone in this world while on. Crafting still needs the right station.");

            y += 6f;
            y = Keys(x, y, w);
            return y;
        }

        // ---------- World modifiers (as on the world creation screen) ----------

        // Each modifier with the options the world creation screen offers for it, easiest to hardest.
        static readonly string[][] ModifierOptions =
        {
            new[] { "Combat", "VeryEasy", "Easy", "Default", "Hard", "VeryHard" },
            new[] { "DeathPenalty", "Casual", "VeryEasy", "Easy", "Default", "Hard", "Hardcore" },
            new[] { "Resources", "MuchLess", "Less", "Default", "More", "MuchMore", "Most" },
            new[] { "Raids", "None", "MuchLess", "Less", "Default", "More", "MuchMore" },
            new[] { "Portals", "Casual", "Default", "Hard", "VeryHard" },
        };
        static readonly string[] Presets = { "Normal", "Casual", "Easy", "Hard", "Hardcore", "Immersive", "Hammer" };

        static float Modifiers(float x, float y, float w)
        {
            Widgets.Header(x, ref y, w, "World modifiers");
            Widgets.Note(x, ref y, w, "Like the world creation screen. Only the host.");
            foreach (var m in ModifierOptions)
            {
                Ui.Label(new Rect(x, y, w, 20f), Spaced(m[0]), Ui.Small);
                y += 20f;
                int n = m.Length - 1;
                float bw = (w - (n - 1) * 4f) / n;
                for (int i = 1; i <= n; i++)
                    Widgets.Command(new Rect(x + (i - 1) * (bw + 4f), y, bw, 24f), Short(m[i]), $"setworldmodifier {m[0]} {m[i]}",
                        style: Widgets.SmallButton);
                y += 28f;
            }
            Ui.Label(new Rect(x, y, w, 20f), "Presets", Ui.Small);
            y += 20f;
            float pw = (w - 3 * 4f) / 4f;
            for (int i = 0; i < Presets.Length; i++)
                Widgets.Command(new Rect(x + i % 4 * (pw + 4f), y + i / 4 * 28f, pw, 24f), Presets[i], "setworldpreset " + Presets[i],
                    $"Reset every world modifier to the {Presets[i]} preset?");
            y += 56f;
            Widgets.Command(new Rect(x, y, w, 24f), "All modifiers back to default", "resetworldkeys", "Reset every world modifier to default?");
            return y + 30f;
        }

        static string Short(string option)
        {
            switch (option)
            {
                case "VeryEasy": return "V. easy";
                case "VeryHard": return "V. hard";
                case "MuchLess": return "Much less";
                case "MuchMore": return "Much more";
                case "Default": return "Normal";
                default: return option;
            }
        }

        static string Spaced(string name) => name == "DeathPenalty" ? "Death penalty" : name;

        // ---------- World keys ----------

        static readonly List<string> AllKeys = new List<string>();
        static string _keyText = "";
        static Vector2 _keyScroll;
        const string KeyControl = "OdinsEye_WorldKey";

        // Every global key the world has right now (boss kills, world modifiers, events), and any key to add.
        static float Keys(float x, float y, float w)
        {
            Widgets.Header(x, ref y, w, "World keys");
            var zones = ZoneSystem.instance;
            var keys = zones != null ? zones.GetGlobalKeys() : new List<string>();
            const float itemH = 24f;
            var area = new Rect(x, y, w, ListH);
            Ui.Inset(area);
            var view = new Rect(0f, 0f, w - 20f, Mathf.Max(1, keys.Count) * itemH);
            _keyScroll = GUI.BeginScrollView(new Rect(area.x + 2f, area.y + 2f, area.width - 4f, area.height - 4f), _keyScroll, view);
            if (keys.Count == 0) Ui.Label(new Rect(6f, 0f, view.width, itemH), $"<color={Ui.Muted}>No world keys yet.</color>", Ui.Small);
            for (int i = 0; i < keys.Count; i++)
            {
                var row = new Rect(0f, i * itemH, view.width, itemH - 2f);
                Ui.Label(new Rect(row.x + 6f, row.y, row.width - 80f, row.height), keys[i], Ui.Small);
                string name = keys[i].Split(' ')[0];
                Widgets.Command(new Rect(row.xMax - 74f, row.y, 74f, row.height), "Remove", "removekey " + name);
            }
            GUI.EndScrollView();
            y += ListH + 6f;

            _keyText = Widgets.SearchField(new Rect(x, y, w - 80f, 24f), _keyText, KeyControl, "Key and value, e.g. EnemyDamage 200");
            bool was = GUI.enabled;
            GUI.enabled = was && _keyText.Trim().Length > 0;
            if (Widgets.Command(new Rect(x + w - 74f, y, 74f, 24f), "Set", "setkey " + _keyText.Trim())) _keyText = "";
            GUI.enabled = was;
            y += 28f;

            // Matching names from the game's list, to click instead of typing.
            string q = _keyText.Trim().Split(' ')[0].ToLowerInvariant();
            if (q.Length > 0)
            {
                if (AllKeys.Count == 0)
                    foreach (var k in System.Enum.GetNames(typeof(GlobalKeys)))
                        if (k != "NonServerOption" && k != "Count") AllKeys.Add(k);
                float bx = x;
                foreach (var k in Widgets.Matches(AllKeys, q, 20, false))
                {
                    float kw = GUI.skin.button.CalcSize(new GUIContent(k)).x + 8f;
                    if (bx + kw > x + w) break;
                    if (GUI.Button(new Rect(bx, y, kw, 22f), new GUIContent(k, "~Fill in " + k))) _keyText = k + " ";
                    bx += kw + 4f;
                }
                y += 26f;
            }
            return y;
        }

        // A short scrolling list of names; clicking one runs its command, the selected one is gold.
        static float List(float x, float y, float w, ref Vector2 scroll, int count, System.Func<int, string> name,
            System.Func<string, string> line, System.Func<string, bool> selected)
        {
            const float itemH = 24f;
            var area = new Rect(x, y, w, ListH);
            Ui.Inset(area);
            var view = new Rect(0f, 0f, w - 20f, count * itemH);
            scroll = GUI.BeginScrollView(new Rect(area.x + 2f, area.y + 2f, area.width - 4f, area.height - 4f), scroll, view);
            for (int i = 0; i < count; i++)
            {
                string n = name(i);
                if (string.IsNullOrEmpty(n)) continue;
                var row = new Rect(0f, i * itemH, view.width, itemH - 2f);
                if (row.yMax < scroll.y || row.y > scroll.y + ListH) continue; // only draw what's visible
                Widgets.Command(row, selected(n) ? $"<color={Ui.Gold}><b>{n}</b></color>" : n, line(n));
            }
            GUI.EndScrollView();
            return y + ListH + 6f;
        }

        static string F(float v) => v.ToString("0.###", CultureInfo.InvariantCulture);
    }
}
