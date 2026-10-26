using HarmonyLib;
using UnityEngine;

namespace OdinsEye
{
    internal enum Tab { Player, Skills, Buffs, Spawn, World, Teleport, Admin, Inspect }

    // The Odin's Eye window: a wood panel with tabs, a line saying what the game currently allows, the tab's
    // content, and the last command with what the game answered.
    internal class Menu : MonoBehaviour
    {
        const float W = 840f, H = 560f, Pad = 18f, TitleH = 34f, TabsH = 32f, BannerH = 34f, FooterH = 26f;

        static readonly Tab[] Tabs = { Tab.Player, Tab.Skills, Tab.Buffs, Tab.Spawn, Tab.World, Tab.Teleport, Tab.Admin, Tab.Inspect };

        internal static bool IsOpen { get; private set; }
        internal static bool SearchFocused; // set by tabs with a search box, so the menu key can be typed there

        static Tab _tab;
        static string _askText, _askLine; // a destructive command waiting for yes or no

        internal static void Toggle()
        {
            if (IsOpen) Close();
            else Open();
        }

        internal static void Open()
        {
            IsOpen = true;
            GUIUtility.keyboardControl = 0;
            _tab = Plugin.LastTab.Value;
            _offset = Plugin.WindowOffset.Value;
        }

        internal static void Close()
        {
            IsOpen = false;
            SearchFocused = false;
            _askText = _askLine = null;
        }

        // Asks before running a destructive command (kill all, drop items and so on).
        internal static void Ask(string question, string line)
        {
            _askText = question;
            _askLine = line;
        }

        void OnGUI()
        {
            if (!IsOpen) return;
            GUISkin oldSkin = GUI.skin;
            GUI.skin = Ui.Skin;
            Matrix4x4 oldMatrix = Ui.BeginScaled(out float sw, out float sh);
            try
            {
                if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
                {
                    Event.current.Use();
                    if (_askText != null) _askText = _askLine = null; // Esc answers "no" first
                    else Close();
                    return;
                }
                var window = new Rect((sw - W) * 0.5f + _offset.x, (sh - H) * 0.5f + _offset.y, W, H);
                bool asking = _askText != null;
                if (!asking) Drag(new Rect(window.x, window.y, window.width - Pad - 40f, Pad + TitleH), sw, sh);
                GUI.enabled = !asking;
                DrawWindow(window);
                GUI.enabled = true;
                if (asking) DrawAsk(window);
            }
            finally
            {
                GUI.matrix = oldMatrix;
                GUI.skin = oldSkin;
            }
        }

        void DrawWindow(Rect r)
        {
            Ui.Window(r);
            float x = r.x + Pad, y = r.y + Pad, inner = r.width - Pad * 2f;

            Ui.Label(new Rect(x, y, inner, TitleH), "Odin's Eye", Ui.Title);
            if (GUI.Button(new Rect(r.xMax - Pad - 30f, y + 2f, 30f, 26f), "X")) Close();
            y += TitleH;

            DrawTabs(new Rect(x, y, inner, TabsH));
            y += TabsH + 6f;

            DrawBanner(new Rect(x, y, inner, BannerH));
            y += BannerH + 8f;

            var content = new Rect(x, y, inner, r.yMax - Pad - FooterH - 8f - y);
            Ui.Inset(content);
            DrawTab(new Rect(content.x + 12f, content.y + 10f, content.width - 24f, content.height - 20f));

            DrawFooter(new Rect(x, r.yMax - Pad - FooterH, inner, FooterH));
        }

        static bool _dragging;
        static Vector2 _dragFrom, _offset; // offset lives here while dragging; the config is written on release

        // Drag the window by its title bar; the spot is saved, and the window can't leave the screen.
        static void Drag(Rect handle, float sw, float sh)
        {
            var e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 0 && handle.Contains(e.mousePosition))
            {
                _dragging = true;
                _dragFrom = e.mousePosition - _offset;
                e.Use();
            }
            else if (_dragging && e.type == EventType.MouseDrag)
            {
                Vector2 offset = e.mousePosition - _dragFrom;
                float maxX = (sw - W) * 0.5f, maxY = (sh - H) * 0.5f;
                offset.x = Mathf.Clamp(offset.x, -maxX, maxX);
                offset.y = Mathf.Clamp(offset.y, -maxY, maxY);
                _offset = offset;
                e.Use();
            }
            else if (_dragging && (e.type == EventType.MouseUp || e.rawType == EventType.MouseUp))
            {
                _dragging = false;
                Plugin.WindowOffset.Value = _offset;
                e.Use();
            }
        }

        static void DrawAsk(Rect window)
        {
            const float w = 420f, h = 130f;
            var r = new Rect(window.center.x - w * 0.5f, window.center.y - h * 0.5f, w, h);
            Ui.Panel(r);
            GUI.Label(new Rect(r.x + 16f, r.y + 14f, w - 32f, 54f), _askText, Widgets.Wrap);
            Ui.Label(new Rect(r.x + 16f, r.y + 62f, w - 32f, 20f), $"<color={Ui.Muted}>> {_askLine}</color>", Ui.Small);
            if (GUI.Button(new Rect(r.x + 16f, r.yMax - 40f, 180f, 28f), "Yes, do it"))
            {
                string line = _askLine;
                _askText = _askLine = null;
                Commands.Run(line);
            }
            if (GUI.Button(new Rect(r.xMax - 196f, r.yMax - 40f, 180f, 28f), "Cancel")) _askText = _askLine = null;
        }

        static void DrawTabs(Rect r)
        {
            float w = (r.width - (Tabs.Length - 1) * 6f) / Tabs.Length;
            for (int i = 0; i < Tabs.Length; i++)
            {
                var tab = Tabs[i];
                string label = tab == _tab ? $"<color={Ui.Gold}><b>{tab}</b></color>" : tab.ToString();
                if (GUI.Button(new Rect(r.x + i * (w + 6f), r.y, w, r.height), label) && tab != _tab)
                {
                    _tab = tab;
                    SearchFocused = false;
                    Plugin.LastTab.Value = tab;
                }
            }
        }

        // What the game allows right now, and the one step to get further when there is one.
        static void DrawBanner(Rect r)
        {
            const float buttonW = 250f;
            var button = new Rect(r.xMax - buttonW, r.y + 3f, buttonW, r.height - 6f);
            var text = new Rect(r.x, r.y, r.width - buttonW - 12f, r.height);
            switch (Commands.Access)
            {
                case Access.NoWorld:
                    Ui.Label(text, $"<color={Ui.Muted}>Load into a world to use Odin's Eye.</color>", Ui.Text);
                    break;
                case Access.NotHost:
                    Ui.Label(text, Commands.IsAdmin
                        ? $"<color={Ui.Warn}>Cheats only work in single player or a world you host.</color> Admin tools work here."
                        : $"<color={Ui.Warn}>Cheats only work in single player or a world you host.</color> You're not an admin here.", Ui.Text);
                    break;
                case Access.DevCommandsOff:
                    Ui.Label(text, "Dev commands are off.", Ui.Text);
                    if (GUI.Button(button, "Turn on dev commands")) Commands.Run("devcommands");
                    break;
                case Access.NeedsConfirm:
                    Ui.Label(text, $"<color={Ui.Bad}>Using a cheat marks this character as cheated: no more achievements.</color>", Ui.Small);
                    if (GUI.Button(button, "I understand, allow cheats")) Commands.Run("confirmcheats");
                    break;
                case Access.Ready:
                    Ui.Label(text, $"<color={Ui.Good}>Cheats on.</color> <color={Ui.Muted}>Every button runs the game's own console command.</color>", Ui.Text);
                    if (GUI.Button(button, "Turn off dev commands")) Commands.Run("devcommands");
                    break;
            }
        }

        static void DrawTab(Rect r)
        {
            switch (_tab)
            {
                case Tab.Player: PlayerTab.Draw(r); break;
                case Tab.Skills: SkillsTab.Draw(r); break;
                case Tab.Buffs: BuffsTab.Draw(r); break;
                case Tab.Spawn: SpawnTab.Draw(r); break;
                case Tab.World: WorldTab.Draw(r); break;
                case Tab.Teleport: TeleportTab.Draw(r); break;
                case Tab.Admin: AdminTab.Draw(r); break;
                case Tab.Inspect: InspectTab.Draw(r); break;
            }
        }

        static void Planned(Rect r, string what)
        {
            Ui.Label(new Rect(r.x, r.y, r.width, 26f), $"<color={Ui.Gold}>Coming next</color>", Ui.Text);
            GUI.Label(new Rect(r.x, r.y + 30f, r.width, r.height - 30f), what, Widgets.Wrap);
        }

        static void DrawFooter(Rect r)
        {
            // Hovering a command button shows what it will run, so the console is learnt along the way.
            if (!string.IsNullOrEmpty(GUI.tooltip) && GUI.tooltip[0] == '~')
            {
                Ui.Label(r, $"<color={Ui.Muted}>{GUI.tooltip.Substring(1)}</color>", Ui.Small);
                return;
            }
            if (!string.IsNullOrEmpty(GUI.tooltip))
            {
                Ui.Label(r, $"<color={Ui.Muted}>Runs:</color> <color={Ui.Gold}>{GUI.tooltip}</color>", Ui.Small);
                return;
            }
            if (Commands.LastCommand == null)
            {
                Ui.Label(r, $"<color={Ui.Muted}>{Ui.KeyName(Plugin.MenuKey.Value)} or Esc to close</color>", Ui.Small);
                return;
            }
            string output = Commands.LastOutput ?? "";
            // The game's own refusals ("not valid in the current context", "Error executing command") in red.
            bool failed = output.Contains("not valid") || output.Contains("Error") || output.Contains("not a recognized") ||
                          output.Contains("Missing object");
            Ui.Label(r, $"<color={Ui.Gold}>> {Commands.LastCommand}</color>   <color={(failed ? Ui.Bad : Ui.Muted)}>{Ui.StripColor(output)}</color>", Ui.Small);
        }
    }

    // While the window is open, tell the game a text box is open: frees the cursor and blocks movement, the
    // camera, the map key and the menu key, the same way typing on a sign does.
    [HarmonyPatch(typeof(TextInput), nameof(TextInput.IsVisible))]
    static class TextInputVisiblePatch
    {
        static void Postfix(ref bool __result)
        {
            if (Menu.IsOpen) __result = true;
        }
    }

    // The inventory key isn't blocked by open text boxes, so block it here.
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Show))]
    static class InventoryShowPatch
    {
        static bool Prefix() => !Menu.IsOpen;
    }
}
