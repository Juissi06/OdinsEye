using System.Collections.Generic;
using UnityEngine;

namespace OdinsEye
{
    // Buffs tab: every status effect the game has, with its icon ("addstatus", "clearstatus"), the guardian powers
    // ("setpower", which also resets the cooldown) and what you've eaten ("puke").
    internal static class BuffsTab
    {
        class Entry
        {
            internal StatusEffect Effect;
            internal string Name, Search;
        }

        const float DetailW = 300f, Cell = 46f, Gap = 14f, SearchH = 28f;
        const string SearchControl = "OdinsEye_BuffSearch";

        static readonly List<Entry> Entries = new List<Entry>();
        static readonly List<Entry> Powers = new List<Entry>();
        static readonly List<Entry> Shown = new List<Entry>();
        static ObjectDB _builtFor;
        static string _search = "", _shownSearch;
        static Entry _selected;
        static Vector2 _scroll;

        internal static void Draw(Rect r)
        {
            var player = Player.m_localPlayer;
            if (player == null) return;
            Build();
            DrawGrid(new Rect(r.x, r.y, r.width - DetailW - Gap, r.height), player);
            DrawSide(new Rect(r.xMax - DetailW, r.y, DetailW, r.height), player);
        }

        // ---------- Data ----------

        // Effects with an icon are the ones a player can see in their status bar; the rest are hidden helpers.
        // Names with spaces can't be typed into the console, so they're left out.
        static void Build()
        {
            var db = ObjectDB.instance;
            if (db == null || db == _builtFor) return;
            _builtFor = db;
            Entries.Clear();
            Powers.Clear();
            _selected = null;
            _shownSearch = null;
            foreach (var se in db.m_StatusEffects)
            {
                if (se == null || se.m_icon == null || se.name.Contains(" ")) continue;
                string name = Localization.instance.Localize(se.m_name);
                if (string.IsNullOrEmpty(name) || name.StartsWith("$")) name = se.name;
                var e = new Entry { Effect = se, Name = name, Search = (name + " " + se.name).ToLowerInvariant() };
                (se.name.StartsWith("GP_") ? Powers : Entries).Add(e);
            }
            Entries.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
        }

        static void Filter()
        {
            if (_shownSearch == _search) return;
            _shownSearch = _search;
            Shown.Clear();
            string q = _search.Trim().ToLowerInvariant();
            foreach (var e in Entries)
                if (q.Length == 0 || e.Search.Contains(q)) Shown.Add(e);
            _scroll = Vector2.zero;
        }

        // ---------- Left: search and icon grid ----------

        static void DrawGrid(Rect r, Player player)
        {
            GUI.SetNextControlName(SearchControl);
            _search = GUI.TextField(new Rect(r.x, r.y, r.width, SearchH), _search ?? "", 40);
            Menu.SearchFocused = GUI.GetNameOfFocusedControl() == SearchControl;
            if (string.IsNullOrEmpty(_search) && !Menu.SearchFocused)
                Ui.Label(new Rect(r.x + 8f, r.y, r.width, SearchH), $"<color={Ui.Muted}>Search status effects...</color>", Ui.Small);
            Filter();

            var area = new Rect(r.x, r.y + SearchH + 6f, r.width, r.height - SearchH - 6f);
            Ui.Inset(area);
            var inner = new Rect(area.x + 4f, area.y + 4f, area.width - 8f, area.height - 8f);
            int cols = Mathf.Max(1, Mathf.FloorToInt((inner.width - 18f) / Cell));
            int rows = (Shown.Count + cols - 1) / cols;
            var view = new Rect(0f, 0f, cols * Cell, rows * Cell);
            var seman = player.GetSEMan();
            _scroll = GUI.BeginScrollView(inner, _scroll, view);
            for (int i = 0; i < Shown.Count; i++)
            {
                var e = Shown[i];
                var cell = new Rect(i % cols * Cell, i / cols * Cell, Cell - 4f, Cell - 4f);
                if (GUI.Button(cell, new GUIContent("", "~" + e.Name))) _selected = e;
                Ui.Icon(new Rect(cell.x + 6f, cell.y + 6f, cell.width - 12f, cell.height - 12f), e.Effect.m_icon);
                // Gold: picked. Green: you have it right now.
                if (e == _selected) Ui.Accent(new Rect(cell.x + 3f, cell.yMax - 5f, cell.width - 6f, 2f), Ui.GoldColor);
                else if (seman.HaveStatusEffect(e.Effect.NameHash()))
                    Ui.Accent(new Rect(cell.x + 3f, cell.yMax - 5f, cell.width - 6f, 2f), Ui.GoodColor);
            }
            GUI.EndScrollView();
            if (Shown.Count == 0)
                Ui.Label(new Rect(inner.x + 8f, inner.y + 4f, inner.width, 24f), $"<color={Ui.Muted}>Nothing matches.</color>", Ui.Small);
        }

        // ---------- Right: picked effect, guardian power, food ----------

        static void DrawSide(Rect r, Player player)
        {
            float x = r.x, w = r.width, y = r.y;

            if (_selected == null)
            {
                GUI.Label(new Rect(x, y, w, 40f), $"<color={Ui.Muted}>Pick a status effect to add it. Green ones are on you now.</color>", Widgets.Wrap);
                y += 44f;
            }
            else
            {
                var se = _selected.Effect;
                Ui.Icon(new Rect(x, y, 36f, 36f), se.m_icon);
                Ui.Label(new Rect(x + 44f, y - 2f, w - 44f, 22f), $"<b>{_selected.Name}</b>", Ui.Text);
                string length = se.m_ttl > 0f ? $"{Mathf.CeilToInt(se.m_ttl / 60f)} min" : "until removed";
                Ui.Label(new Rect(x + 44f, y + 18f, w - 44f, 20f), $"<color={Ui.Muted}>{se.name}, {length}</color>", Ui.Small);
                y += 40f;
                string tip = Ui.StripColor(Localization.instance.Localize(se.m_tooltip)).Replace("\n", " ");
                if (!string.IsNullOrEmpty(tip))
                {
                    GUI.Label(new Rect(x, y, w, 52f), $"<color={Ui.Muted}>{tip}</color>", Widgets.Wrap);
                    y += 54f;
                }
                Widgets.Command(new Rect(x, y, w * 0.5f - 3f, Widgets.RowH - 4f), "Add to me", "addstatus " + se.name);
            }
            Widgets.Command(new Rect(x + w * 0.5f + 3f, y, w * 0.5f - 3f, Widgets.RowH - 4f), "Clear all effects", "clearstatus");
            y += Widgets.RowH + 4f;

            DrawPowers(x, ref y, w, player);
            DrawFood(x, ref y, w, player);
        }

        static void DrawPowers(float x, ref float y, float w, Player player)
        {
            if (Powers.Count == 0) return;
            string current = player.GetGuardianPowerName();
            string state = string.IsNullOrEmpty(current) ? "none"
                : player.m_guardianPowerCooldown > 0f ? $"cooldown {Mathf.CeilToInt(player.m_guardianPowerCooldown / 60f)} min" : "ready";
            Ui.Label(new Rect(x, y, w, Widgets.HeaderH), $"<color={Ui.Gold}>Guardian power</color> <color={Ui.Muted}>({state})</color>", Ui.Text);
            y += Widgets.HeaderH;
            float size = Mathf.Min(40f, (w - (Powers.Count - 1) * 4f) / Powers.Count);
            for (int i = 0; i < Powers.Count; i++)
            {
                var p = Powers[i];
                var cell = new Rect(x + i * (size + 4f), y, size, size);
                Widgets.Command(cell, "", "setpower " + p.Effect.name);
                Ui.Icon(new Rect(cell.x + 5f, cell.y + 5f, cell.width - 10f, cell.height - 10f), p.Effect.m_icon);
                if (p.Effect.name == current) Ui.Accent(new Rect(cell.x + 3f, cell.yMax - 4f, cell.width - 6f, 2f), Ui.GoldColor);
            }
            y += size + 4f;
            Widgets.Note(x, ref y, w, "Picking one also resets its cooldown.");
        }

        static void DrawFood(float x, ref float y, float w, Player player)
        {
            Ui.Label(new Rect(x, y, w, Widgets.HeaderH), $"<color={Ui.Gold}>Food</color>", Ui.Text);
            y += Widgets.HeaderH;
            var foods = player.GetFoods();
            float fx = x;
            if (foods.Count == 0) Ui.Label(new Rect(x, y, 140f, 32f), $"<color={Ui.Muted}>Nothing eaten.</color>", Ui.Small);
            foreach (var food in foods)
            {
                Ui.Icon(new Rect(fx, y + 2f, 28f, 28f), food.m_item?.GetIcon());
                Ui.Label(new Rect(fx + 30f, y, 30f, 32f), $"<color={Ui.Muted}>{Mathf.CeilToInt(food.m_time / 60f)}m</color>", Ui.Small);
                fx += 60f;
            }
            Widgets.Command(new Rect(x + w - 120f, y + 2f, 120f, 28f), "Empty stomach", "puke");
            y += 36f;
        }
    }
}
