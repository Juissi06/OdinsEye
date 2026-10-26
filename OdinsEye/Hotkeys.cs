using BepInEx.Configuration;
using UnityEngine;

namespace OdinsEye
{
    // Keys for the most used switches, so they work without opening the window. All unbound by default; set them
    // in the config. They run the same console commands as the buttons and say what happened in the top left.
    internal class Hotkeys : MonoBehaviour
    {
        void Update()
        {
            var player = Player.m_localPlayer;
            if (player == null || Plugin.IsTyping()) return;
            Press(Plugin.KeyGod, "God mode", "god");
            Press(Plugin.KeyGhost, "Ghost mode", "ghost");
            Press(Plugin.KeyFly, "Fly", "fly");
            Press(Plugin.KeyNoCost, "No build cost", "nocost");
            if (Plugin.KeyBack.Value.IsDown())
            {
                if (!Back.Any) player.Message(MessageHud.MessageType.TopLeft, "Odin's Eye: nowhere to go back to");
                else if (!Commands.CanRun("goto")) player.Message(MessageHud.MessageType.TopLeft, "Odin's Eye: cheats aren't on");
                else Back.Go();
            }
        }

        static void Press(ConfigEntry<KeyboardShortcut> key, string label, string line)
        {
            if (!key.Value.IsDown() || !Commands.Exists(line)) return;
            var player = Player.m_localPlayer;
            if (!Commands.CanRun(line))
            {
                player.Message(MessageHud.MessageType.TopLeft, "Odin's Eye: cheats aren't on");
                return;
            }
            Commands.Run(line);
            player.Message(MessageHud.MessageType.TopLeft, $"{label}: {State(line, player)}");
        }

        static string State(string line, Player player)
        {
            switch (line)
            {
                case "god": return player.InGodMode() ? "on" : "off";
                case "ghost": return player.InGhostMode() ? "on" : "off";
                case "fly": return player.IsDebugFlying() ? "on" : "off";
                case "nocost": return player.NoCostCheat() ? "on" : "off";
                default: return Ui.StripColor(Commands.LastOutput);
            }
        }
    }
}
