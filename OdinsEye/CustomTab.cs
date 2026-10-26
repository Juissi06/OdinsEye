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
            // A greyed-out button says why in the footer: a misspelt command, or one the game won't run right now.
            string unknown = lines.Select(l => l.Split(' ')[0]).FirstOrDefault(c => !Commands.Exists(c));
            string tip = unknown != null ? $"~\"{unknown}\" isn't a console command" : can ? line : $"~The game won't run this right now: {line}";
            bool was = GUI.enabled;
            GUI.enabled = was && can;
            bool clicked = GUI.Button(r, new GUIContent(label, tip));
            GUI.enabled = was;
            if (!clicked) return;
            GUIUtility.keyboardControl = 0;
            foreach (var l in lines) Commands.Run(l);
        }

        // ---------- Suggestions while typing the console line ----------

        const float SuggestH = 124f, SuggestRowH = 20f;
        const int MaxSuggestions = 5;
        static bool _moveToEnd;
        static int _moveToEndFrame;
        static string _suggestedFor;
        static List<Suggestion> _suggested = new List<Suggestion>();

        class Suggestion
        {
            internal string Text, Note;
        }

        // What fits the word being typed: command names first, then that command's options (item and creature
        // names for spawn, skills for raiseskill...), the same list the game's console offers on Tab.
        // Worked out once per change of the line, not on every GUI event: "spawn " alone matches thousands of names.
        static List<Suggestion> Suggestions(string line)
        {
            if (line == _suggestedFor) return _suggested;
            _suggestedFor = line;
            _suggested = FindSuggestions(line);
            return _suggested;
        }

        static List<Suggestion> FindSuggestions(string line)
        {
            var list = new List<Suggestion>();
            string part = line.Substring(line.LastIndexOf(';') + 1).TrimStart();
            int space = part.IndexOf(' ');
            if (space < 0)
            {
                // A command typed in full stays first, so Tab adds the space instead of swapping it for a longer
                // one ("setkey" isn't "setkeyplayer"). Ones that can't run right now say so.
                var commands = new Dictionary<string, Terminal.ConsoleCommand>();
                foreach (var c in Commands.Visible()) commands[c.Command] = c;
                foreach (var name in Widgets.Matches(commands.Keys, part, MaxSuggestions, true))
                {
                    var c = commands[name];
                    string note = c.Description;
                    if (!Commands.CanRun(c.Command)) note = (c.IsCheat ? "(needs dev commands) " : "(can't run here) ") + note;
                    list.Add(new Suggestion { Text = c.Command, Note = note });
                }
                return list;
            }
            // Only the first word after the command has options in the game's list.
            string command = part.Substring(0, space);
            string rest = part.Substring(space + 1);
            if (rest.Contains(" ") || Commands.Find(command) == null) return list;
            foreach (var option in Widgets.Matches(Commands.Options(command), rest, MaxSuggestions, false))
                list.Add(new Suggestion { Text = option });
            return list;
        }

        // A command earlier on the line (before the last ;) that the console doesn't know. The part still being
        // typed doesn't count: that's what the suggestions are for.
        static string UnknownEarlier(string line)
        {
            var parts = Split(line);
            bool typing = !line.TrimEnd().EndsWith(";");
            for (int i = 0; i < parts.Count - (typing ? 1 : 0); i++)
            {
                string c = parts[i].Split(' ')[0];
                if (!Commands.Exists(c)) return c;
            }
            return null;
        }

        // Replaces the word being typed with the suggestion and a space.
        static void Accept(string text)
        {
            string line = _line ?? "";
            int cut = Mathf.Max(line.LastIndexOf(' '), line.LastIndexOf(';')) + 1;
            _line = line.Substring(0, cut) + text + " ";
            GUI.FocusControl(LineControl);
            _moveToEnd = true;
            _moveToEndFrame = Time.frameCount;
        }

        static void DrawSuggestions(Rect r)
        {
            string line = _line ?? "";
            var found = Suggestions(line);
            string misspelt = UnknownEarlier(line);
            if (found.Count > 0 && misspelt != null)
            {
                // Still said while suggesting for the next command on the line.
                GUI.Label(new Rect(r.x, r.y, r.width, 18f), $"<color={Ui.Bad}>\"{misspelt}\" isn't a console command.</color>", Widgets.Wrap);
                r = new Rect(r.x, r.y + 20f, r.width, r.height - 20f);
            }
            if (found.Count == 0)
            {
                // No word to complete: the command's own syntax as a reminder, or a problem, or the basics.
                string part = line.Substring(line.LastIndexOf(';') + 1).Trim();
                var command = Commands.Find(part.Split(' ')[0]);
                string unknown = Split(line).Select(l => l.Split(' ')[0]).FirstOrDefault(c => !Commands.Exists(c));
                string help = unknown != null ? $"<color={Ui.Bad}>\"{unknown}\" isn't a console command.</color>"
                    : command != null ? $"<color={Ui.Gold}>{command.Command}</color> <color={Ui.Muted}>{command.Description}</color>"
                    : $"<color={Ui.Muted}>Start typing a console command: suggestions show up here. Tab takes the first one. Several at once: separate them with ;</color>";
                GUI.Label(r, help, Widgets.Wrap);
                return;
            }
            Ui.Inset(r);
            for (int i = 0; i < found.Count; i++)
            {
                var s = found[i];
                var row = new Rect(r.x + 4f, r.y + 4f + i * (SuggestRowH + 3f), r.width - 8f, SuggestRowH);
                string text = s.Note != null ? $"{s.Text}  <color={Ui.Muted}>{s.Note}</color>" : s.Text;
                if (GUI.Button(row, new GUIContent(text, "~Tab or click to use it"), Widgets.SuggestionStyle)) Accept(s.Text);
            }
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
            // Tab takes the first suggestion, like the game's console (and stops Tab moving focus away).
            var e = Event.current;
            if (e.type == EventType.KeyDown && (e.keyCode == KeyCode.Tab || e.character == '	') && GUI.GetNameOfFocusedControl() == LineControl)
            {
                // Unity sends Tab as a key event and again as a typed character; only the key event completes.
                if (e.keyCode == KeyCode.Tab)
                {
                    var first = Suggestions(_line ?? "");
                    if (first.Count > 0) Accept(first[0].Text);
                }
                e.Use();
            }
            GUI.SetNextControlName(LineControl);
            _line = GUI.TextField(new Rect(x, y, w, 26f), _line ?? "", 200);
            // A suggestion clicked just before the tab changed or the window closed mustn't move the cursor later.
            if (_moveToEnd && Time.frameCount > _moveToEndFrame + 5) _moveToEnd = false;
            if (_moveToEnd && GUI.GetNameOfFocusedControl() == LineControl)
            {
                // After a suggestion the cursor goes to the end, ready for the next word.
                _moveToEnd = false;
                var editor = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
                editor.text = _line;
                editor.MoveTextEnd();
            }
            y += 30f;
            DrawSuggestions(new Rect(x, y, w, SuggestH));
            y += SuggestH + 4f;

            bool was = GUI.enabled;
            GUI.enabled = was && Commands.LastCommand != null;
            if (GUI.Button(new Rect(x, y, w, 26f), new GUIContent("Use the last command", "~" + (Commands.LastCommand ?? ""))))
            {
                _line = Commands.LastCommand;
                GUIUtility.keyboardControl = 0;
            }
            // Not with a command the console doesn't know anywhere on the line.
            var parts = Split(_line);
            GUI.enabled = was && _name.Trim().Length > 0 && parts.Count > 0 && parts.All(p => Commands.Exists(p.Split(' ')[0]));
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
