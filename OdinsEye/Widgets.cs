using System.Collections.Generic;
using UnityEngine;

namespace OdinsEye
{
    // Small building blocks the tabs share. Every command button greys out when the game wouldn't run that
    // command right now (Commands.CanRun), so the window never offers something that silently fails.
    internal static class Widgets
    {
        // A search box with a grey placeholder; while it has focus the menu key can be typed into it.
        internal static string SearchField(Rect r, string text, string control, string placeholder)
        {
            GUI.SetNextControlName(control);
            text = GUI.TextField(r, text ?? "", 40);
            Menu.SearchFocused = GUI.GetNameOfFocusedControl() == control;
            if (string.IsNullOrEmpty(text) && !Menu.SearchFocused)
                Ui.Label(new Rect(r.x + 8f, r.y, r.width, r.height), $"<color={Ui.Muted}>{placeholder}</color>", Ui.Small);
            return text;
        }

        // Everyone in the world but you.
        internal static List<ZNet.PlayerInfo> OtherPlayers()
        {
            var list = new List<ZNet.PlayerInfo>();
            var players = ZNet.instance != null ? ZNet.instance.GetPlayerList() : null;
            string me = Player.m_localPlayer != null ? Player.m_localPlayer.GetPlayerName() : null;
            if (players != null)
                foreach (var info in players)
                    if (info.m_name != me) list.Add(info);
            return list;
        }

        // Names matching what's typed, for suggestions: those starting with it first, then those containing it,
        // each shortest first. The exact name itself is only kept when asked (so Tab can just add the space).
        internal static List<string> Matches(IEnumerable<string> names, string typed, int max, bool keepExact)
        {
            string q = (typed ?? "").ToLowerInvariant();
            var starts = new List<string>();
            var contains = new List<string>();
            string exact = null;
            if (q.Length == 0) return starts;
            foreach (var n in names)
            {
                if (n == null) continue; // the game's own option lists skip empty entries too
                string l = n.ToLowerInvariant();
                if (l == q) exact = n;
                else if (l.StartsWith(q)) starts.Add(n);
                else if (l.Contains(q)) contains.Add(n);
            }
            System.Comparison<string> order = (a, b) => a.Length != b.Length ? a.Length.CompareTo(b.Length) : string.CompareOrdinal(a, b);
            starts.Sort(order);
            contains.Sort(order);
            var result = new List<string>();
            if (keepExact && exact != null) result.Add(exact);
            foreach (var n in starts) if (result.Count < max) result.Add(n);
            foreach (var n in contains) if (result.Count < max) result.Add(n);
            return result;
        }

        internal const float RowH = 30f, HeaderH = 26f;

        static GUIStyle _wrap;

        internal static GUIStyle Wrap
        {
            get
            {
                if (_wrap == null) _wrap = new GUIStyle(Ui.Small) { wordWrap = true, alignment = TextAnchor.UpperLeft };
                return _wrap;
            }
        }

        static GUIStyle _smallButton;
        static GUISkin _smallButtonFor;

        // The window's button with a smaller font, for rows of many narrow buttons.
        internal static GUIStyle SmallButton
        {
            get
            {
                if (_smallButton == null || _smallButtonFor != GUI.skin)
                {
                    _smallButtonFor = GUI.skin;
                    _smallButton = new GUIStyle(GUI.skin.button) { fontSize = 12, padding = new RectOffset(2, 2, 2, 2) };
                }
                return _smallButton;
            }
        }

        static GUIStyle _suggestion;

        // A list row that's just text: left-aligned, one line, cut off at the end, gold on hover.
        internal static GUIStyle SuggestionStyle
        {
            get
            {
                if (_suggestion == null)
                {
                    _suggestion = new GUIStyle(Ui.Small) { wordWrap = false, clipping = TextClipping.Clip, padding = new RectOffset(6, 4, 1, 1) };
                    _suggestion.hover.textColor = Ui.GoldColor;
                }
                return _suggestion;
            }
        }

        internal static void Header(float x, ref float y, float w, string text)
        {
            Ui.Label(new Rect(x, y, w, HeaderH), $"<color={Ui.Gold}>{text}</color>", Ui.Text);
            y += HeaderH;
        }

        // Muted note under a control; wraps onto more lines when it doesn't fit the column.
        internal static void Note(float x, ref float y, float w, string text)
        {
            var content = new GUIContent($"<color={Ui.Muted}>{text}</color>");
            float h = Mathf.Max(20f, Wrap.CalcHeight(content, w));
            GUI.Label(new Rect(x, y, w, h), content, Wrap);
            y += h + 2f;
        }

        // Runs line when clicked; greyed out unless the game accepts the command. confirm != null asks first.
        // adminOnly: commands the client always accepts but the server refuses unless you're an admin
        // (kick, ban, save), so grey them out for everyone else instead of sending a request that fails.
        internal static bool Command(Rect r, string label, string line, string confirm = null, bool adminOnly = false, GUIStyle style = null)
        {
            string name = line.Split(' ')[0];
            if (!Commands.Exists(name)) return false;
            bool was = GUI.enabled;
            GUI.enabled = was && Commands.CanRun(name) && (!adminOnly || Commands.IsAdmin);
            bool clicked = GUI.Button(r, new GUIContent(label, line), style ?? GUI.skin.button); // tooltip: the footer previews the command
            GUI.enabled = was;
            if (!clicked) return false;
            // Unity keeps showing the tooltip of the focused control, so drop focus or the footer sticks on this command.
            GUIUtility.keyboardControl = 0;
            if (confirm != null) Menu.Ask(confirm, line);
            else Commands.Run(line);
            return true;
        }

        // A switch showing the current state; clicking runs onLine or offLine.
        internal static void Switch(Rect r, string label, bool on, string onLine, string offLine)
        {
            string line = on ? offLine : onLine;
            string name = line.Split(' ')[0];
            if (!Commands.Exists(name)) return;
            bool was = GUI.enabled;
            GUI.enabled = was && Commands.CanRun(name);
            bool now = GUI.Toggle(r, on, new GUIContent(label, line));
            GUI.enabled = was;
            if (now == on) return;
            GUIUtility.keyboardControl = 0;
            Commands.Run(line);
        }

        // Row of equal-width command buttons: labels[i] runs lines[i].
        internal static void CommandRow(float x, ref float y, float w, string[] labels, string[] lines)
        {
            float bw = (w - (labels.Length - 1) * 6f) / labels.Length;
            for (int i = 0; i < labels.Length; i++)
                Command(new Rect(x + i * (bw + 6f), y, bw, RowH - 4f), labels[i], lines[i]);
            y += RowH;
        }

        // Plain button (no command), for choices inside the window.
        internal static bool Choice(Rect r, string label, bool selected, string tooltip = null)
        {
            return GUI.Button(r, new GUIContent(selected ? $"<color={Ui.Gold}><b>{label}</b></color>" : label, tooltip));
        }

        // Tooltips starting with "~" are shown as they are in the footer, without "Runs:".
        internal const string OwnCheat = "~Odin's Eye's own cheat: there's no console command for this.";

        // Switch for one of Odin's Eye's own cheats (Cheats): greyed out where the game wouldn't allow a cheat.
        internal static bool CheatSwitch(Rect r, string label, bool on)
        {
            bool was = GUI.enabled;
            GUI.enabled = was && Cheats.Allowed;
            bool now = GUI.Toggle(r, on, new GUIContent(label, OwnCheat));
            GUI.enabled = was;
            if (now != on) GUIUtility.keyboardControl = 0;
            return now;
        }
    }
}
