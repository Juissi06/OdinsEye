using System.Collections.Generic;
using HarmonyLib;

namespace OdinsEye
{
    // Where the local player stands with the game's command rules (Terminal.IsCheatsEnabled and
    // ConsoleCommand.RunAction): cheats need dev commands on, being the server or host, and, the first time,
    // accepting that the character counts as cheated for achievements.
    internal enum Access { NoWorld, NotHost, DevCommandsOff, NeedsConfirm, Ready }

    internal static class Commands
    {
        static readonly AccessTools.FieldRef<Dictionary<string, Terminal.ConsoleCommand>> All =
            AccessTools.StaticFieldRefAccess<Dictionary<string, Terminal.ConsoleCommand>>(AccessTools.Field(typeof(Terminal), "commands"));

        static List<string> _captured;

        // The last command run from the window and what the game printed for it.
        internal static string LastCommand;
        internal static string LastOutput;

        internal static Access Access
        {
            get
            {
                if (ZNet.instance == null || Player.m_localPlayer == null || global::Console.instance == null) return Access.NoWorld;
                if (!ZNet.instance.IsServer()) return Access.NotHost;
                if (!Terminal.m_cheat) return Access.DevCommandsOff;
                if (!Achievements.IsCheatedAtAll()) return Access.NeedsConfirm;
                return Access.Ready;
            }
        }

        internal static bool IsAdmin => ZNet.instance != null && ZNet.instance.LocalPlayerIsAdminOrHost();

        // Whether the game would run this command right now: the same check the console uses, plus commands a
        // client sends to the server (kick, ban, save). Cheats also wait for the achievement confirmation.
        internal static bool CanRun(string name)
        {
            var console = global::Console.instance;
            if (console == null || !All().TryGetValue(name, out var command)) return false;
            if (command.IsCheat && !Achievements.IsCheatedAtAll()) return false;
            if (command.IsValid(console)) return true;
            return command.RemoteCommand && ZNet.instance != null && !ZNet.instance.IsServer() && IsAdmin;
        }

        // A command this game version doesn't have hides its button instead of failing.
        internal static bool Exists(string name) => All().ContainsKey(name);

        // Runs a console line exactly as if typed, and keeps what the game printed for the window's status line.
        internal static void Run(string line)
        {
            var console = global::Console.instance;
            if (console == null) return;
            _captured = new List<string>();
            try
            {
                console.TryRunCommand(line);
            }
            finally
            {
                LastCommand = line;
                LastOutput = _captured.Count > 0 ? string.Join("  ", _captured) : "done";
                _captured = null;
            }
            Plugin.Log.LogInfo($"{line} -> {LastOutput}");
        }

        internal static void Capture(string text) => _captured?.Add(text);
    }

    // The console prints each command's result; while the window runs one, keep a copy.
    [HarmonyPatch(typeof(Terminal), nameof(Terminal.AddString), typeof(string))]
    static class TerminalOutputPatch
    {
        static void Prefix(string text) => Commands.Capture(text);
    }
}
