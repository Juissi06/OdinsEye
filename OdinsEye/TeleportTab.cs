using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace OdinsEye
{
    // Teleport tab: pick a spot on the map, saved spots per world, the game's locations (with shortcuts to
    // traders and boss altars), biomes, and other players. Uses goto, findtp, findbiometp and recall.
    internal static class TeleportTab
    {
        const float Gap = 24f, ListH = 150f, RowH = 26f;
        const string SearchControl = "OdinsEye_LocationSearch";

        // Shortcut label and the text findtp looks for in location names.
        static readonly string[][] Shortcuts =
        {
            new[] { "Haldor", "Vendor_BlackForest" }, new[] { "Hildir", "Hildir_camp" }, new[] { "Bog Witch", "BogWitch" },
            new[] { "Eikthyr", "Eikthyrnir" }, new[] { "The Elder", "GDKing" }, new[] { "Bonemass", "Bonemass" },
            new[] { "Moder", "Dragonqueen" }, new[] { "Yagluth", "GoblinKing" }, new[] { "The Queen", "DvergrBoss" },
            new[] { "Fader", "Fader" }
        };

        // Map pick: the next left click on the large map teleports there.
        internal static bool Picking;

        static Vector2 _scroll, _locScroll, _spotScroll;
        static float _contentH = 600f;
        static string _search = "", _spotName = "";
        static List<string> _locations;
        static ZoneSystem _locationsFor;

        internal static void Draw(Rect r)
        {
            var view = new Rect(0f, 0f, r.width - 20f, _contentH);
            _scroll = GUI.BeginScrollView(r, _scroll, view);
            float colW = (view.width - Gap) * 0.5f;
            float left = Left(0f, colW), right = Right(colW + Gap, colW);
            _contentH = Mathf.Max(left, right) + 8f;
            GUI.EndScrollView();
        }

        // ---------- Map, saved spots, players ----------

        static float Left(float x, float w)
        {
            float y = 0f;
            var player = Player.m_localPlayer;

            Widgets.Header(x, ref y, w, "Map");
            if (Commands.Exists("goto"))
            {
                bool was = GUI.enabled;
                GUI.enabled = was && Commands.CanRun("goto");
                if (GUI.Button(new Rect(x, y, w - 130f, Widgets.RowH - 4f), "Pick a spot on the map")) StartPicking();
                GUI.enabled = was && Commands.CanRun("goto") && Back.Any;
                if (GUI.Button(new Rect(x + w - 124f, y, 124f, Widgets.RowH - 4f), new GUIContent("Back", Back.Line ?? "~Where you were before your last teleport")))
                {
                    GUIUtility.keyboardControl = 0;
                    Back.Go();
                }
                GUI.enabled = was;
                y += Widgets.RowH;
            }
            if (player != null)
            {
                Vector3 p = player.transform.position;
                Widgets.Note(x, ref y, w, $"You are at {p.x:0}, {p.z:0}  (height {p.y:0})");
            }

            y += 6f;
            Widgets.Header(x, ref y, w, "Saved spots");
            GUI.SetNextControlName("OdinsEye_SpotName");
            _spotName = GUI.TextField(new Rect(x, y, w - 130f, Widgets.RowH - 4f), _spotName ?? "", 30);
            if (GUI.Button(new Rect(x + w - 124f, y, 124f, Widgets.RowH - 4f), "Save this spot") && player != null)
                Spots.Add(string.IsNullOrWhiteSpace(_spotName) ? $"Spot {Spots.Current.Count + 1}" : _spotName.Trim(), player.transform.position);
            y += Widgets.RowH;
            y = SpotList(x, y, w);

            y += 6f;
            Widgets.Header(x, ref y, w, "Players");
            y = PlayerList(x, y, w);
            return y;
        }

        static float SpotList(float x, float y, float w)
        {
            var spots = Spots.Current;
            if (spots.Count == 0)
            {
                Widgets.Note(x, ref y, w, "No saved spots in this world yet.");
                return y;
            }
            var area = new Rect(x, y, w, Mathf.Min(ListH, spots.Count * RowH + 6f));
            Ui.Inset(area);
            var view = new Rect(0f, 0f, w - 24f, spots.Count * RowH);
            _spotScroll = GUI.BeginScrollView(new Rect(area.x + 2f, area.y + 3f, area.width - 4f, area.height - 6f), _spotScroll, view);
            for (int i = 0; i < spots.Count; i++)
            {
                var s = spots[i];
                float ry = i * RowH;
                Ui.Label(new Rect(4f, ry, view.width - 150f, RowH - 2f), $"{s.Name} <color={Ui.Muted}>{s.X:0}, {s.Z:0}</color>", Ui.Small);
                Widgets.Command(new Rect(view.width - 140f, ry, 70f, RowH - 3f), "Go", $"goto {s.X:0} {s.Z:0}");
                if (GUI.Button(new Rect(view.width - 66f, ry, 66f, RowH - 3f), "Delete")) Spots.Remove(s);
            }
            GUI.EndScrollView();
            return y + area.height + 6f;
        }

        static float PlayerList(float x, float y, float w)
        {
            var net = ZNet.instance;
            var players = net != null ? net.GetPlayerList() : null;
            string me = Player.m_localPlayer != null ? Player.m_localPlayer.GetPlayerName() : null;
            int shown = 0;
            if (players != null)
                foreach (var info in players)
                {
                    if (info.m_name == me) continue;
                    shown++;
                    Ui.Label(new Rect(x, y, w - 170f, RowH - 2f), info.m_name, Ui.Small);
                    if (TryGetPosition(info, out Vector3 pos))
                        Widgets.Command(new Rect(x + w - 164f, y, 80f, RowH - 3f), "Go to", $"goto {pos.x:0} {pos.z:0}");
                    // recall matches by name; the first word is enough to find them.
                    Widgets.Command(new Rect(x + w - 80f, y, 80f, RowH - 3f), "Bring", "recall " + info.m_name.Split(' ')[0]);
                    y += RowH;
                }
            if (shown == 0) Widgets.Note(x, ref y, w, "No other players here.");
            return y;
        }

        // Public map positions come with the player list; otherwise the player's own object, if it's loaded.
        internal static bool TryGetPosition(ZNet.PlayerInfo info, out Vector3 pos)
        {
            pos = info.m_position;
            if (info.m_publicPosition) return true;
            var zdo = ZDOMan.instance != null && !info.m_characterID.IsNone() ? ZDOMan.instance.GetZDO(info.m_characterID) : null;
            if (zdo == null) return false;
            pos = zdo.GetPosition();
            return true;
        }

        // ---------- Locations and biomes ----------

        static float Right(float x, float w)
        {
            float y = 0f;
            BuildLocations();

            Widgets.Header(x, ref y, w, "Traders and bosses");
            Widgets.Note(x, ref y, w, "Teleports to the nearest one the world has");
            float bw = (w - 6f * 2f) / 3f;
            int col = 0;
            foreach (var s in Shortcuts)
            {
                if (!_locations.Any(l => l.IndexOf(s[1], System.StringComparison.OrdinalIgnoreCase) >= 0)) continue;
                Widgets.Command(new Rect(x + col * (bw + 6f), y, bw, Widgets.RowH - 4f), s[0], "findtp " + s[1]);
                if (++col == 3)
                {
                    col = 0;
                    y += Widgets.RowH;
                }
            }
            if (col != 0) y += Widgets.RowH;

            y += 6f;
            Widgets.Header(x, ref y, w, "All locations");
            GUI.SetNextControlName(SearchControl);
            _search = GUI.TextField(new Rect(x, y, w, Widgets.RowH - 4f), _search ?? "", 40);
            Menu.SearchFocused = GUI.GetNameOfFocusedControl() == SearchControl;
            if (string.IsNullOrEmpty(_search) && !Menu.SearchFocused)
                Ui.Label(new Rect(x + 8f, y, w, Widgets.RowH - 4f), $"<color={Ui.Muted}>Search locations...</color>", Ui.Small);
            y += Widgets.RowH;
            string q = (_search ?? "").Trim();
            var shown = q.Length == 0 ? _locations : _locations.Where(l => l.IndexOf(q, System.StringComparison.OrdinalIgnoreCase) >= 0).ToList();
            var area = new Rect(x, y, w, ListH);
            Ui.Inset(area);
            var view = new Rect(0f, 0f, w - 24f, shown.Count * RowH);
            _locScroll = GUI.BeginScrollView(new Rect(area.x + 2f, area.y + 3f, area.width - 4f, area.height - 6f), _locScroll, view);
            for (int i = 0; i < shown.Count; i++)
            {
                var row = new Rect(0f, i * RowH, view.width, RowH - 3f);
                if (row.yMax < _locScroll.y || row.y > _locScroll.y + ListH) continue;
                Widgets.Command(row, shown[i], "findtp " + shown[i]);
            }
            GUI.EndScrollView();
            y += ListH + 6f;

            Widgets.Header(x, ref y, w, "Nearest biome");
            col = 0;
            foreach (var biome in Commands.Options("findbiometp"))
            {
                if (string.IsNullOrEmpty(biome)) continue;
                Widgets.Command(new Rect(x + col * (bw + 6f), y, bw, Widgets.RowH - 4f), biome, "findbiometp " + biome);
                if (++col == 3)
                {
                    col = 0;
                    y += Widgets.RowH;
                }
            }
            if (col != 0) y += Widgets.RowH;
            return y;
        }

        static void BuildLocations()
        {
            var zones = ZoneSystem.instance;
            if (zones == _locationsFor && _locations != null) return;
            _locationsFor = zones;
            _locations = zones == null
                ? new List<string>()
                : zones.m_locations.Where(l => l != null && l.m_enable && !string.IsNullOrEmpty(l.m_prefabName))
                    .Select(l => l.m_prefabName).Distinct().OrderBy(n => n).ToList();
        }

        // ---------- Map pick ----------

        static void StartPicking()
        {
            if (Minimap.instance == null) return;
            Picking = true;
            Menu.Close();
            Minimap.instance.SetMapMode(Minimap.MapMode.Large);
        }

        static readonly System.Reflection.MethodInfo ScreenToWorld = AccessTools.Method(typeof(Minimap), "ScreenToWorldPoint");

        internal static void Pick(Minimap map)
        {
            Picking = false;
            if (ScreenToWorld == null) return;
            var world = (Vector3)ScreenToWorld.Invoke(map, new object[] { (Vector3)ZInput.pointerPosition });
            map.SetMapMode(Minimap.MapMode.Small);
            Commands.Run($"goto {world.x:0} {world.z:0}");
        }
    }

    // While picking, a left click on the large map teleports instead of ticking a pin.
    [HarmonyPatch(typeof(Minimap), nameof(Minimap.OnMapLeftClick))]
    static class MapPickPatch
    {
        static bool Prefix(Minimap __instance)
        {
            if (!TeleportTab.Picking) return true;
            TeleportTab.Pick(__instance);
            return false;
        }
    }

    // Shows "click the map" while picking, and stops picking when the map closes.
    internal class MapPickHint : MonoBehaviour
    {
        void Update()
        {
            if (TeleportTab.Picking && !Minimap.IsOpen()) TeleportTab.Picking = false;
        }

        void OnGUI()
        {
            if (!TeleportTab.Picking) return;
            Matrix4x4 old = Ui.BeginScaled(out float sw, out float sh);
            var r = new Rect((sw - 420f) * 0.5f, 90f, 420f, 40f);
            Ui.Panel(r);
            Ui.Label(r, $"<color={Ui.Gold}>Click the map to teleport there</color>  <color={Ui.Muted}>(close the map to cancel)</color>", Ui.TextCenter);
            GUI.matrix = old;
        }
    }

    // Saved teleport spots, per world, in BepInEx/config/OdinsEye.spots.txt (one "world, name, x, z" per line).
    internal static class Spots
    {
        internal class Spot
        {
            internal string World, Name;
            internal float X, Z;
        }

        static readonly string FilePath = Path.Combine(Paths.ConfigPath, "OdinsEye.spots.txt");
        static List<Spot> _all;

        static string World => ZNet.instance != null ? ZNet.instance.GetWorldName() : "";

        internal static List<Spot> Current => All.Where(s => s.World == World).ToList();

        static List<Spot> All
        {
            get
            {
                if (_all != null) return _all;
                _all = new List<Spot>();
                if (!File.Exists(FilePath)) return _all;
                foreach (var line in File.ReadAllLines(FilePath))
                {
                    var p = line.Split('\t');
                    if (p.Length == 4 && float.TryParse(p[2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float x) &&
                        float.TryParse(p[3], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float z))
                        _all.Add(new Spot { World = p[0], Name = p[1], X = x, Z = z });
                }
                return _all;
            }
        }

        internal static void Add(string name, Vector3 pos)
        {
            All.Add(new Spot { World = World, Name = name.Replace('\t', ' '), X = pos.x, Z = pos.z });
            Save();
        }

        internal static void Remove(Spot spot)
        {
            All.RemoveAll(s => s.World == spot.World && s.Name == spot.Name && s.X == spot.X && s.Z == spot.Z);
            Save();
        }

        static void Save()
        {
            try
            {
                File.WriteAllLines(FilePath, All.Select(s =>
                    string.Join("\t", s.World, s.Name, s.X.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        s.Z.ToString(System.Globalization.CultureInfo.InvariantCulture))));
            }
            catch (IOException e)
            {
                Plugin.Log.LogWarning("Couldn't save teleport spots: " + e.Message);
            }
        }
    }
}
