using HarmonyLib;
using UnityEngine;

namespace OdinsEye
{
    internal enum Tab { Player, Spawn, World, Teleport, Admin, Inspect }

    // The Odin's Eye window: a wood panel with tabs, a line saying what the game currently allows, the tab's
    // content, and the last command with what the game answered.
    internal class Menu : MonoBehaviour
    {
        const float W = 840f, H = 560f, Pad = 18f, TitleH = 34f, TabsH = 32f, BannerH = 34f, FooterH = 26f;

        static readonly Tab[] Tabs = { Tab.Player, Tab.Spawn, Tab.World, Tab.Teleport, Tab.Admin, Tab.Inspect };

        internal static bool IsOpen { get; private set; }
        internal static bool SearchFocused; // set by tabs with a search box, so the menu key can be typed there

        static Tab _tab;

        internal static void Toggle()
        {
            if (IsOpen) Close();
            else Open();
        }

        internal static void Open()
        {
            IsOpen = true;
            _tab = Plugin.LastTab.Value;
        }

        internal static void Close()
        {
            IsOpen = false;
            SearchFocused = false;
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
                    Close();
                    return;
                }
                DrawWindow(new Rect((sw - W) * 0.5f, (sh - H) * 0.5f, W, H));
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
                case Tab.Spawn: Planned(r, "Items and creatures with icons and search: amount, quality, star level, spawn where you look or into your inventory, starter item sets."); break;
                case Tab.World: Planned(r, "Time of day, skip time, weather, wind, start and stop raids, kill enemies, remove drops, boss progress."); break;
                case Tab.Teleport: Planned(r, "Click the map to go there, saved spots, find a location and jump to it, bring players to you."); break;
                case Tab.Admin: Planned(r, "Repair everything nearby, delete what you look at, set fuel, stop fire and smoke, map and portal rules, player list with kick and ban, save the world."); break;
                case Tab.Inspect: Planned(r, "What's under your crosshair: prefab name, owner, health, position and its saved data."); break;
            }
        }

        static void Planned(Rect r, string what)
        {
            Ui.Label(new Rect(r.x, r.y, r.width, 26f), $"<color={Ui.Gold}>Coming next</color>", Ui.Text);
            var style = new GUIStyle(Ui.Text) { wordWrap = true, alignment = TextAnchor.UpperLeft };
            GUI.Label(new Rect(r.x, r.y + 30f, r.width, r.height - 30f), what, style);
        }

        static void DrawFooter(Rect r)
        {
            if (Commands.LastCommand == null)
            {
                Ui.Label(r, $"<color={Ui.Muted}>{Ui.KeyName(Plugin.MenuKey.Value)} or Esc to close</color>", Ui.Small);
                return;
            }
            Ui.Label(r, $"<color={Ui.Gold}>&gt; {Commands.LastCommand}</color>   <color={Ui.Muted}>{Commands.LastOutput}</color>", Ui.Small);
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
