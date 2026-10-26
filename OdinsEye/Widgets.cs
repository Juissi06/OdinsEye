using UnityEngine;

namespace OdinsEye
{
    // Small building blocks the tabs share. Every command button greys out when the game wouldn't run that
    // command right now (Commands.CanRun), so the window never offers something that silently fails.
    internal static class Widgets
    {
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
