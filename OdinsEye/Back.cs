using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace OdinsEye
{
    // "Back": where you were before each teleport, newest last. Every teleport goes through Player.TeleportTo
    // (goto, findtp, portals, dungeon doors), so they're all remembered. Going back uses the game's "goto".
    internal static class Back
    {
        const int Max = 10;
        static readonly List<Vector3> History = new List<Vector3>();
        static Player _for;
        static bool _goingBack;

        internal static bool Any => Valid() && History.Count > 0;

        internal static Vector3 Last => History[History.Count - 1];

        static bool Valid()
        {
            if (Player.m_localPlayer != _for)
            {
                _for = Player.m_localPlayer;
                History.Clear();
            }
            return _for != null;
        }

        internal static void Remember(Vector3 from, Vector3 to)
        {
            if (_goingBack || !Valid() || Vector3.Distance(from, to) < 10f) return;
            History.Add(from);
            if (History.Count > Max) History.RemoveAt(0);
        }

        internal static string Line => Any ? $"goto {Last.x:0} {Last.z:0}" : null;

        internal static void Go()
        {
            if (!Any) return;
            string line = Line;
            _goingBack = true;
            try
            {
                Commands.Run(line);
            }
            finally
            {
                _goingBack = false;
            }
            // Only forget the spot once the game actually took us there.
            if (Commands.LastOutput == "done") History.RemoveAt(History.Count - 1);
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.TeleportTo))]
    static class TeleportHistoryPatch
    {
        static void Prefix(Player __instance, out Vector3 __state) => __state = __instance.transform.position;

        static void Postfix(Player __instance, Vector3 pos, bool __result, Vector3 __state)
        {
            if (__result && __instance == Player.m_localPlayer) Back.Remember(__state, pos);
        }
    }
}
