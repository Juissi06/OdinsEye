using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using UnityEngine;

namespace OdinsEye
{
    // Custom tab: your own buttons. Each one is a name and one or more console lines (separated by ";"), kept in
    // BepInEx/config/OdinsEye.buttons.txt, so "Get 50 wood" or a raid you test often is one click away.
    internal static class CustomTab
    {
        const float FormW = 280f, Gap = 14f, Cols = 3f, RowH = 34f;
        const string NameControl = "OdinsEye_ButtonName", LineControl = "OdinsEye_ButtonLine";

        static string _name = "", _line = "";
        static bool _editing;
        static Vector2 _scroll;

        internal static void Draw(Rect r)
        {
            DrawButtons(new Rect(r.x, r.y, r.width - FormW - Gap, r.height));
            DrawForm(new Rect(r.xMax - FormW, r.y, FormW, r.height));
            string focused = GUI.GetNameOfFocusedControl();
            Menu.SearchFocused = focused == NameControl || focused == LineControl;
        }

        static void DrawButtons(Rect r)
        {
            var all = CustomButtons.All;
            Ui.Label(new Rect(r.x, r.y, 200f, Widgets.HeaderH), $"<color={Ui.Gold}>Your buttons</color>", Ui.Text);
            if (all.Count > 0 && Widgets.Choice(new Rect(r.xMax - 110f, r.y, 110f, 24f), _editing ? "Done" : "Remove...", _editing))
                _editing = !_editing;
            float top = r.y + Widgets.HeaderH + 4f;
            var area = new Rect(r.x, top, r.width, r.yMax - top);
            Ui.Inset(area);
            var inner = new Rect(area.x + 6f, area.y + 6f, area.width - 12f, area.height - 12f);
            if (all.Count == 0)
            {
                GUI.Label(new Rect(inner.x + 4f, inner.y, inner.width - 8f, 60f),
                    $"<color={Ui.Muted}>No buttons yet. Make one on the right: give it a name and the console line it runs.</color>", Widgets.Wrap);
                return;
            }
            float bw = (inner.width - 20f - (Cols - 1f) * 6f) / Cols;
            int rows = Mathf.CeilToInt(all.Count / Cols);
            var view = new Rect(0f, 0f, inner.width - 20f, rows * RowH);
            _scroll = GUI.BeginScrollView(inner, _scroll, view);
            for (int i = 0; i < all.Count; i++)
            {
                var b = all[i];
                var cell = new Rect(i % (int)Cols * (bw + 6f), i / (int)Cols * RowH, bw, RowH - 6f);
                if (_editing)
                {
                    if (GUI.Button(cell, new GUIContent($"<color={Ui.Bad}>Remove</color> {b.Name}", "~" + b.Line)))
                    {
                        CustomButtons.Remove(b);
                        break;
                    }
                }
                else Run(cell, b.Name, b.Line);
            }
            GUI.EndScrollView();
        }

        // Like Widgets.Command, for one or more lines: greyed out unless the game would run every one of them.
        static void Run(Rect r, string label, string line)
        {
            var lines = Split(line);
            bool can = lines.Count > 0 && lines.All(l => Commands.CanRun(l.Split(' ')[0]));
            bool was = GUI.enabled;
            GUI.enabled = was && can;
            bool clicked = GUI.Button(r, new GUIContent(label, line));
            GUI.enabled = was;
            if (!clicked) return;
            GUIUtility.keyboardControl = 0;
            foreach (var l in lines) Commands.Run(l);
        }

        static List<string> Split(string line) =>
            line.Split(';').Select(l => l.Trim()).Where(l => l.Length > 0).ToList();

        static void DrawForm(Rect r)
        {
            Ui.Inset(r);
            float x = r.x + 10f, w = r.width - 20f, y = r.y + 8f;
            Widgets.Header(x, ref y, w, "New button");
            Ui.Label(new Rect(x, y, w, 20f), "Name", Ui.Small);
            y += 20f;
            GUI.SetNextControlName(NameControl);
            _name = GUI.TextField(new Rect(x, y, w, 26f), _name ?? "", 30);
            y += 32f;
            Ui.Label(new Rect(x, y, w, 20f), "Console line", Ui.Small);
            y += 20f;
            GUI.SetNextControlName(LineControl);
            _line = GUI.TextField(new Rect(x, y, w, 26f), _line ?? "", 200);
            y += 30f;
            GUI.Label(new Rect(x, y, w, 56f), $"<color={Ui.Muted}>Several at once: separate them with ;  e.g. spawn Wood 50; spawn Stone 50</color>", Widgets.Wrap);
            y += 58f;

            bool was = GUI.enabled;
            GUI.enabled = was && Commands.LastCommand != null;
            if (GUI.Button(new Rect(x, y, w, 26f), new GUIContent("Use the last command", "~" + (Commands.LastCommand ?? ""))))
            {
                _line = Commands.LastCommand;
                GUIUtility.keyboardControl = 0;
            }
            GUI.enabled = was && _name.Trim().Length > 0 && Split(_line).Count > 0;
            if (GUI.Button(new Rect(x, y + 32f, w, 30f), "Add button"))
            {
                CustomButtons.Add(_name.Trim(), string.Join("; ", Split(_line).ToArray()));
                _name = _line = "";
                GUIUtility.keyboardControl = 0;
            }
            GUI.enabled = was;
        }
    }

    // The buttons file: one "name<tab>line" per line.
    internal static class CustomButtons
    {
        internal class Button
        {
            internal string Name, Line;
        }

        static readonly string FilePath = Path.Combine(Paths.ConfigPath, "OdinsEye.buttons.txt");
        static List<Button> _all;

        internal static List<Button> All
        {
            get
            {
                if (_all != null) return _all;
                _all = new List<Button>();
                if (!File.Exists(FilePath)) return _all;
                foreach (var line in File.ReadAllLines(FilePath))
                {
                    var p = line.Split('\t');
                    if (p.Length == 2 && p[0].Length > 0 && p[1].Length > 0) _all.Add(new Button { Name = p[0], Line = p[1] });
                }
                return _all;
            }
        }

        internal static void Add(string name, string line)
        {
            All.Add(new Button { Name = name.Replace('\t', ' '), Line = line.Replace('\t', ' ') });
            Save();
        }

        internal static void Remove(Button button)
        {
            All.Remove(button);
            Save();
        }

        static void Save()
        {
            try
            {
                File.WriteAllLines(FilePath, All.Select(b => b.Name + "\t" + b.Line));
            }
            catch (IOException e)
            {
                Plugin.Log.LogWarning("Couldn't save your buttons: " + e.Message);
            }
        }
    }
}
