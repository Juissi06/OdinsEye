using System.Collections.Generic;
using UnityEngine;

namespace OdinsEye
{
    // Inspect tab (kept small): what's in the middle of your view, behind the window. The camera stands still
    // while the window is open, and a physics ray ignores the UI, so it reads the thing you were looking at.
    internal static class InspectTab
    {
        const float LookRange = 60f, RefreshInterval = 0.5f, KeyW = 130f, LineH = 22f;

        class Line
        {
            internal string Key, Value;
        }

        static readonly List<Line> Lines = new List<Line>();
        static string _title, _prefab;
        static Character _creature;
        static Vector3? _markA, _markB;
        static Nearby.Counts _counts;
        static float _countedAt = -10f;
        static float _refreshedAt = -10f;

        internal static void Draw(Rect r)
        {
            if (Time.unscaledTime - _refreshedAt > RefreshInterval)
            {
                _refreshedAt = Time.unscaledTime;
                Read();
            }

            float x = r.x, y = r.y, w = r.width;
            if (_title == null)
            {
                Widgets.Header(x, ref y, w, "Nothing in view");
                Widgets.Note(x, ref y, w, $"Close the window, look at something within {LookRange:0} m, and open it again.");
                DrawAround(x, y + 10f, w);
                return;
            }

            Widgets.Header(x, ref y, w, _title);
            foreach (var line in Lines)
            {
                Ui.Label(new Rect(x, y, KeyW, LineH), $"<color={Ui.Muted}>{line.Key}</color>", Ui.Small);
                Ui.Label(new Rect(x + KeyW, y, w - KeyW, LineH), line.Value, Ui.Small);
                y += LineH;
            }

            y += 10f;
            float bw = 200f;
            if (GUI.Button(new Rect(x, y, bw, Widgets.RowH - 4f), "Copy prefab name"))
            {
                GUIUtility.systemCopyBuffer = _prefab;
                Commands.LastCommand = "copy";
                Commands.LastOutput = _prefab + " copied";
            }
            Widgets.Command(new Rect(x + bw + 8f, y, bw, Widgets.RowH - 4f), "Spawn another", "spawn " + _prefab);
            y += Widgets.RowH;

            if (_creature != null)
            {
                Ui.Label(new Rect(x, y, KeyW, LineH), $"<color={Ui.Muted}>Stars</color>", Ui.Small);
                for (int s = 0; s <= 4; s++)
                {
                    string label = s == _creature.GetLevel() - 1 ? $"<color={Ui.Gold}><b>{s}</b></color>" : s.ToString();
                    if (Widgets.CheatButton(new Rect(x + KeyW + s * 44f, y, 40f, Widgets.RowH - 4f), label)) Nearby.SetStars(_creature, s);
                }
                y += Widgets.RowH;
                Widgets.Note(x, ref y, w, "The game spawns up to 2 stars; 3 and 4 are for testing.");
            }
            DrawAround(x, y + 6f, w);
        }

        // Where you stand, a tape measure between two marks, and how much is loaded around you.
        static void DrawAround(float x, float y, float w)
        {
            var player = Player.m_localPlayer;
            if (player == null) return;
            Vector3 me = player.transform.position;
            Widgets.Header(x, ref y, w, "Around you");
            Ui.Label(new Rect(x, y, KeyW, LineH), $"<color={Ui.Muted}>You</color>", Ui.Small);
            Ui.Label(new Rect(x + KeyW, y, w - KeyW, LineH), $"{me.x:0.0}, {me.z:0.0}, height {me.y:0.0}", Ui.Small);
            y += LineH;

            float bw = 120f;
            if (GUI.Button(new Rect(x, y, bw, Widgets.RowH - 4f), "Mark A here")) _markA = me;
            if (GUI.Button(new Rect(x + bw + 6f, y, bw, Widgets.RowH - 4f), "Mark B here")) _markB = me;
            string measure;
            if (_markA.HasValue && _markB.HasValue)
            {
                Vector3 a = _markA.Value, b = _markB.Value;
                measure = $"A to B <b>{Vector3.Distance(a, b):0.0} m</b> <color={Ui.Muted}>({Utils.DistanceXZ(a, b):0.0} m flat, {b.y - a.y:+0.0;-0.0} m up)</color>";
            }
            else if (_markA.HasValue)
                measure = $"You to A <b>{Vector3.Distance(me, _markA.Value):0.0} m</b> <color={Ui.Muted}>({Utils.DistanceXZ(me, _markA.Value):0.0} m flat)</color>";
            else measure = $"<color={Ui.Muted}>Mark a spot, walk, and read the distance.</color>";
            Ui.Label(new Rect(x + bw * 2f + 16f, y, w - bw * 2f - 16f, LineH + 4f), measure, Ui.Small);
            y += Widgets.RowH;

            if (Time.unscaledTime - _countedAt > 1f)
            {
                _countedAt = Time.unscaledTime;
                _counts = Nearby.Count(Nearby.Range);
            }
            Ui.Label(new Rect(x, y, KeyW, LineH), $"<color={Ui.Muted}>Within {Nearby.Range:0} m</color>", Ui.Small);
            Ui.Label(new Rect(x + KeyW, y, w - KeyW, LineH),
                $"{_counts.Pieces} pieces, {_counts.Drops} items on the ground, {_counts.Creatures} creatures, {_counts.Objects} objects in all", Ui.Small);
        }

        static void Read()
        {
            Lines.Clear();
            _title = _prefab = null;
            _creature = null;
            var cam = GameCamera.instance != null ? GameCamera.instance.transform : null;
            var player = Player.m_localPlayer;
            if (cam == null || player == null) return;

            // Nearest hit that isn't you.
            GameObject target = null;
            Vector3 point = Vector3.zero;
            float best = float.MaxValue;
            foreach (var hit in Physics.RaycastAll(cam.position, cam.forward, LookRange, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.distance >= best || hit.collider.transform.IsChildOf(player.transform)) continue;
                best = hit.distance;
                point = hit.point;
                // The ground belongs to a zone's Heightmap, whose root is just "_Zone": name it as terrain instead.
                var nview = hit.collider.GetComponentInParent<ZNetView>();
                var ground = nview == null ? hit.collider.GetComponentInParent<Heightmap>() : null;
                target = nview != null ? nview.gameObject : ground != null ? ground.gameObject : hit.collider.transform.root.gameObject;
            }
            if (target == null) return;

            _prefab = Utils.GetPrefabName(target);
            _title = target.GetComponent<Heightmap>() != null ? $"Terrain ({Heightmap.FindBiome(point)})" : NameOf(target) ?? _prefab;
            Add("Prefab", _prefab);
            Add("Kind", KindOf(target));
            Vector3 pos = target.transform.position;
            Add("Position", $"{pos.x:0.0}, {pos.y:0.0}, {pos.z:0.0}   ({Vector3.Distance(player.transform.position, point):0.0} m away)");

            var nv = target.GetComponent<ZNetView>();
            var zdo = nv != null && nv.IsValid() ? nv.GetZDO() : null;
            if (zdo != null)
            {
                long owner = zdo.GetOwner();
                string ownerText = owner == 0L ? "no one" : owner == ZDOMan.GetSessionID() ? "you" : "another player or the server";
                Add("Network", $"id {zdo.m_uid}, owned by {ownerText}{(zdo.Persistent ? "" : ", not saved")}");
            }
            else Add("Network", "not networked (part of the terrain or a location)");

            var piece = target.GetComponent<Piece>();
            if (piece != null)
            {
                long creator = piece.GetCreator();
                Add("Built by", creator == 0L ? "the world" : creator == player.GetPlayerID() ? "you" : $"player {creator}");
            }

            var wnt = target.GetComponent<WearNTear>();
            if (wnt != null) Add("Health", $"{wnt.GetHealthPercentage() * 100f:0}%");

            var character = target.GetComponent<Character>();
            if (character != null && !character.IsPlayer()) _creature = character;
            if (character != null)
            {
                Add("Health", $"{character.GetHealth():0} / {character.GetMaxHealth():0}");
                Add("Level", character.GetLevel() > 1 ? $"{character.GetLevel()} ({character.GetLevel() - 1} star)" : "1 (no stars)");
                Add("Faction", character.m_faction + (character.IsTamed() ? ", tamed" : ""));
                var ai = target.GetComponent<BaseAI>();
                if (ai != null)
                {
                    var targetCreature = ai.GetTargetCreature();
                    Add("Mind", (ai.IsAlerted() ? "alerted" : "calm") +
                                (targetCreature != null ? $", after {Localization.instance.Localize(targetCreature.m_name)}" : ""));
                }
            }
            else if (zdo != null && target.GetComponent<Destructible>() != null)
                Add("Health", $"{zdo.GetFloat(ZDOVars.s_health, target.GetComponent<Destructible>().m_health):0}");
        }

        static void Add(string key, string value) => Lines.Add(new Line { Key = key, Value = value });

        static string NameOf(GameObject go)
        {
            var hoverable = go.GetComponent<Hoverable>();
            string name = hoverable?.GetHoverName();
            if (string.IsNullOrEmpty(name)) name = go.GetComponent<Piece>()?.m_name;
            if (string.IsNullOrEmpty(name)) name = go.GetComponent<Character>()?.m_name;
            if (string.IsNullOrEmpty(name)) name = go.GetComponent<ItemDrop>()?.m_itemData.m_shared.m_name;
            return string.IsNullOrEmpty(name) ? null : Localization.instance.Localize(name);
        }

        static string KindOf(GameObject go)
        {
            if (go.GetComponent<Character>() != null) return "creature";
            if (go.GetComponent<ItemDrop>() != null) return "item";
            if (go.GetComponent<Plant>() != null) return "growing plant";
            if (go.GetComponent<Pickable>() != null) return "pickable";
            if (go.GetComponent<Piece>() != null) return "building piece";
            if (go.GetComponent<TreeBase>() != null || go.GetComponent<TreeLog>() != null) return "tree";
            if (go.GetComponent<MineRock>() != null || go.GetComponent<MineRock5>() != null) return "rock or ore";
            if (go.GetComponent<Destructible>() != null) return "destructible";
            if (go.GetComponent<Heightmap>() != null) return "terrain";
            return "object";
        }
    }
}
