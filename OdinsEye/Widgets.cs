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

        internal static void Header(float x, ref float y, float w, string text)
        {
            Ui.Label(new Rect(x, y, w, HeaderH), $"<color={Ui.Gold}>{text}</color>", Ui.Text);
            y += HeaderH;
        }

        internal static void Note(float x, ref float y, float w, string text)
        {
            Ui.Label(new Rect(x, y, w, 20f), $"<color={Ui.Muted}>{text}</color>", Ui.Small);
            y += 22f;
        }

        // Runs line when clicked; greyed out unless the game accepts the command. confirm != null asks first.
        internal static bool Command(Rect r, string label, string line, string confirm = null)
        {
            string name = line.Split(' ')[0];
            if (!Commands.Exists(name)) return false;
            bool was = GUI.enabled;
            GUI.enabled = was && Commands.CanRun(name);
            bool clicked = GUI.Button(r, label);
            GUI.enabled = was;
            if (!clicked) return false;
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
            bool now = GUI.Toggle(r, on, label);
            GUI.enabled = was;
            if (now != on) Commands.Run(line);
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
        internal static bool Choice(Rect r, string label, bool selected)
        {
            return GUI.Button(r, selected ? $"<color={Ui.Gold}><b>{label}</b></color>" : label);
        }
    }
}
