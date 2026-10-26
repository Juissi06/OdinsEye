using System.Globalization;
using UnityEngine;

namespace OdinsEye
{
    // Admin tab: the server (save, players with kick and ban, banned list, difficulty), world rules (map,
    // portals), the map's exploration, and clean-up tools around you (delete, fuel, fire, gear repair).
    // Kick, ban and save work for server admins on a dedicated server; the rest follow the cheat rules.
    internal static class AdminTab
    {
        const float Gap = 24f, RowH = 26f;

        static Vector2 _scroll;
        static float _contentH = 600f, _deleteRadius = 5f, _fuel = 10f, _players;
        static string _deleteName = "", _unban = "";

        internal static void Draw(Rect r)
        {
            var view = new Rect(0f, 0f, r.width - 20f, _contentH);
            _scroll = GUI.BeginScrollView(r, _scroll, view);
            float colW = (view.width - Gap) * 0.5f;
            float left = Left(0f, colW), right = Right(colW + Gap, colW);
            _contentH = Mathf.Max(left, right) + 8f;
            GUI.EndScrollView();
        }

        // ---------- Server and players ----------

        static float Left(float x, float w)
        {
            float y = 0f;
            var net = ZNet.instance;

            Widgets.Header(x, ref y, w, "Server");
            if (net != null)
                Widgets.Note(x, ref y, w, $"World: {net.GetWorldName()}   " +
                                          (net.IsServer() ? "you are the host" : Commands.IsAdmin ? "you are an admin" : "you are not an admin"));
            float half = (w - 6f) * 0.5f;
            Widgets.Command(new Rect(x, y, half, Widgets.RowH - 4f), "Save world now", "save", adminOnly: true);
            Widgets.Command(new Rect(x + half + 6f, y, half, Widgets.RowH - 4f), "List banned", "banned", adminOnly: true);
            y += Widgets.RowH;
            GUI.SetNextControlName("OdinsEye_Unban");
            _unban = GUI.TextField(new Rect(x, y, w - 110f, Widgets.RowH - 4f), _unban ?? "", 64);
            bool was = GUI.enabled;
            GUI.enabled = was && !string.IsNullOrWhiteSpace(_unban);
            Widgets.Command(new Rect(x + w - 104f, y, 104f, Widgets.RowH - 4f), "Unban", "unban " + (_unban ?? "").Trim(), adminOnly: true);
            GUI.enabled = was;
            y += Widgets.RowH;
            Widgets.Note(x, ref y, w, "Unban takes a player ID or IP from the banned list");

            y += 6f;
            Widgets.Header(x, ref y, w, "Players");
            int shown = 0;
            foreach (var info in Widgets.OtherPlayers())
                {
                    shown++;
                    // Kick and ban by the player's platform ID: names can have spaces, the command reads one word.
                    string id = info.m_userInfo.m_id.ToString();
                    Ui.Label(new Rect(x, y, w - 170f, RowH - 2f), info.m_name, Ui.Small);
                    Widgets.Command(new Rect(x + w - 164f, y, 80f, RowH - 3f), "Kick", "kick " + id, $"Kick {info.m_name}?", adminOnly: true);
                    Widgets.Command(new Rect(x + w - 80f, y, 80f, RowH - 3f), "Ban", "ban " + id, $"Ban {info.m_name} from this server?", adminOnly: true);
                    y += RowH;
                }
            if (shown == 0) Widgets.Note(x, ref y, w, "No other players here.");

            y += 6f;
            Widgets.Header(x, ref y, w, "Difficulty");
            Widgets.Note(x, ref y, w, "Enemies scale as if this many players were near");
            _players = GUI.HorizontalSlider(new Rect(x, y + 8f, w - 100f, 16f), _players, 0f, 10f);
            int count = Mathf.RoundToInt(_players);
            Widgets.Command(new Rect(x + w - 90f, y, 90f, Widgets.RowH - 4f), count == 0 ? "Real" : $"{count} players", "players " + count);
            y += Widgets.RowH;
            return y;
        }

        // ---------- World rules, map, tools ----------

        static float Right(float x, float w)
        {
            float y = 0f;
            var zones = ZoneSystem.instance;
            float half = (w - 6f) * 0.5f;

            Widgets.Header(x, ref y, w, "World rules");
            bool noPortals = zones != null && zones.GetGlobalKey(GlobalKeys.NoPortals);
            Widgets.Switch(new Rect(x, y + 4f, half, 24f), "Portals off", noPortals, "noportals", "noportals");
            bool noMap = zones != null && zones.GetGlobalKey(GlobalKeys.NoMap);
            Widgets.Switch(new Rect(x + half + 6f, y + 4f, half, 24f), "Map off", noMap, "nomap", "nomap");
            y += Widgets.RowH + 4f;

            Widgets.Header(x, ref y, w, "Map");
            Widgets.Command(new Rect(x, y, half, Widgets.RowH - 4f), "Reveal whole map", "exploremap");
            Widgets.Command(new Rect(x + half + 6f, y, half, Widgets.RowH - 4f), "Forget explored map", "resetmap", "Forget everything you've explored on the map?");
            y += Widgets.RowH;
            Widgets.Command(new Rect(x, y, w, Widgets.RowH - 4f), "Clear cartography table data", "resetsharedmap",
                "Remove all shared map data from the cartography tables?");
            y += Widgets.RowH + 4f;

            Widgets.Header(x, ref y, w, "Delete around you");
            Widgets.Note(x, ref y, w, "Removes everything nearby. No undo, back up first!");
            _deleteRadius = GUI.HorizontalSlider(new Rect(x + 90f, y + 6f, w - 90f, 16f), _deleteRadius, 1f, 50f);
            Ui.Label(new Rect(x, y, 90f, 22f), $"Radius {Mathf.RoundToInt(_deleteRadius)} m", Ui.Small);
            y += 26f;
            GUI.SetNextControlName("OdinsEye_DeleteName");
            _deleteName = GUI.TextField(new Rect(x, y, w - 130f, Widgets.RowH - 4f), _deleteName ?? "", 40);
            string filter = (_deleteName ?? "").Trim();
            if (filter.Length == 0) Ui.Label(new Rect(x + 8f, y, w - 140f, Widgets.RowH - 4f), $"<color={Ui.Muted}>only names containing... (optional)</color>", Ui.Small);
            int radius = Mathf.RoundToInt(_deleteRadius);
            // The game's forcedelete takes one word and deletes every object whose name contains it, so the
            // question says exactly that word. Spaces become _, as in object names ("wood wall" -> "wood_wall").
            string word = filter.Replace(' ', '_');
            string line = word.Length > 0 ? $"forcedelete {radius} {word}" : $"forcedelete {radius}";
            Widgets.Command(new Rect(x + w - 124f, y, 124f, Widgets.RowH - 4f), "Delete", line,
                word.Length > 0 ? $"Delete every object whose name contains \"{word}\" within {radius} m? There's no undo." : $"Delete EVERYTHING within {radius} m? There's no undo.");
            y += Widgets.RowH + 4f;

            Widgets.Header(x, ref y, w, "Fires and lights");
            _fuel = GUI.HorizontalSlider(new Rect(x + 90f, y + 6f, w - 200f, 16f), _fuel, 0f, 10f);
            Ui.Label(new Rect(x, y, 90f, 22f), $"Fuel {Mathf.RoundToInt(_fuel)}", Ui.Small);
            Widgets.Command(new Rect(x + w - 104f, y, 104f, Widgets.RowH - 4f), "Set all", "setfuel " + Mathf.RoundToInt(_fuel).ToString(CultureInfo.InvariantCulture));
            y += Widgets.RowH;
            Widgets.CommandRow(x, ref y, w, new[] { "Put out fires", "Clear smoke" }, new[] { "stopfire", "stopsmoke" });

            Widgets.Header(x, ref y, w, "Repair");
            Widgets.Command(new Rect(x, y, half, Widgets.RowH - 4f), "Your gear", "repairall");
            bool was = GUI.enabled;
            GUI.enabled = was && Cheats.Allowed;
            if (GUI.Button(new Rect(x + half + 6f, y, half, Widgets.RowH - 4f), new GUIContent($"Buildings within {RepairRange:0} m", Widgets.OwnCheat)))
                RepairBuildings();
            GUI.enabled = was;
            y += Widgets.RowH;
            return y;
        }

        const float RepairRange = 30f;

        // The game has no command for this: the same repair the hammer does, on every damaged piece around you.
        static void RepairBuildings()
        {
            GUIUtility.keyboardControl = 0;
            var player = Player.m_localPlayer;
            if (player == null) return;
            Vector3 p = player.transform.position;
            int count = 0;
            foreach (var piece in WearNTear.GetAllInstances())
                if (piece != null && Vector3.Distance(piece.transform.position, p) <= RepairRange && piece.Repair()) count++;
            Commands.LastCommand = "Repair buildings";
            Commands.LastOutput = count == 0 ? "nothing needed repairing" : $"repaired {count} pieces";
            player.Message(MessageHud.MessageType.TopLeft, $"Repaired {count} pieces");
        }
    }
}
