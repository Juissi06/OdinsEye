using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace OdinsEye
{
    // Odin's Eye's own cheats for the things around you that the game has no command for: production, cooking,
    // beehives, pickables, young creatures, tames, terrain and snow. Each goes through the game's own code where
    // it has some (Plant.Grow, the smelter's queue, the cooking station's slots...), takes ownership first like the
    // game does when you use something, and says what it did.
    internal static class Nearby
    {
        internal const float Range = 30f;

        // ---------- Helpers ----------

        static List<T> Around<T>(float range) where T : Component
        {
            var list = new List<T>();
            var player = Player.m_localPlayer;
            if (player == null) return list;
            Vector3 p = player.transform.position;
            foreach (var c in UnityEngine.Object.FindObjectsByType<T>(FindObjectsSortMode.None))
                if (c != null && Vector3.Distance(c.transform.position, p) <= range) list.Add(c);
            return list;
        }

        // Only the owner may change an object; take it, as the game does when you open or use something.
        static ZNetView Own(Component c)
        {
            var view = c.GetComponent<ZNetView>();
            if (view == null || !view.IsValid()) return null;
            if (!view.IsOwner()) view.ClaimOwnership();
            return view;
        }

        static void Report(string what, string text)
        {
            GUIUtility.keyboardControl = 0;
            Commands.LastCommand = what;
            Commands.LastOutput = text;
            Player.m_localPlayer?.Message(MessageHud.MessageType.TopLeft, $"{what}: {text}");
        }

        static T Method<T>(Type type, string name) where T : Delegate
        {
            try { return AccessTools.MethodDelegate<T>(AccessTools.Method(type, name)); }
            catch (Exception e)
            {
                Debug.LogWarning($"[Odin's Eye] {type.Name}.{name} not found: {e.Message}");
                return null;
            }
        }

        // ---------- Production ----------

        static readonly Func<Smelter, string> QueuedOre = Method<Func<Smelter, string>>(typeof(Smelter), "GetQueuedOre");
        static readonly Action<Smelter> RemoveOneOre = Method<Action<Smelter>>(typeof(Smelter), "RemoveOneOre");
        static readonly Action<Smelter, string> QueueProcessed = Method<Action<Smelter, string>>(typeof(Smelter), "QueueProcessed");
        static readonly Action<Smelter> SpawnProcessed = Method<Action<Smelter>>(typeof(Smelter), "SpawnProcessed");
        static readonly Action<Smelter, float> SetBakeTimer = Method<Action<Smelter, float>>(typeof(Smelter), "SetBakeTimer");

        // Smelters, kilns, furnaces, spinning wheels, windmills, refineries: everything queued comes out now, as
        // the smelter's own loop would hand it out (stacks where it stacks). Fuel isn't used up. Fermenters are
        // made ready to tap.
        internal static void FinishProduction()
        {
            int items = 0, stations = 0, barrels = 0;
            if (QueuedOre != null && RemoveOneOre != null && QueueProcessed != null && SpawnProcessed != null)
            {
                foreach (var smelter in Around<Smelter>(Range))
                {
                    if (Own(smelter) == null) continue;
                    string ore = QueuedOre(smelter);
                    if (ore == "") continue;
                    stations++;
                    for (int guard = 0; guard < 1000 && ore != ""; guard++)
                    {
                        RemoveOneOre(smelter);
                        QueueProcessed(smelter, ore);
                        items++;
                        ore = QueuedOre(smelter);
                    }
                    SpawnProcessed(smelter);
                    SetBakeTimer?.Invoke(smelter, 0f);
                }
            }
            foreach (var fermenter in Around<Fermenter>(Range))
            {
                var view = Own(fermenter);
                if (view == null) continue;
                var zdo = view.GetZDO();
                if (zdo.GetString(ZDOVars.s_content) == "" || zdo.GetLong(ZDOVars.s_startTime, 0L) == 0L) continue;
                long done = ZNet.instance.GetTime().Ticks - TimeSpan.FromSeconds(fermenter.m_fermentationDuration + 5f).Ticks;
                zdo.Set(ZDOVars.s_startTime, done);
                barrels++;
            }
            Report("Finish production", stations + barrels == 0 ? "nothing in progress nearby"
                : $"{items} items from {stations} stations" + (barrels > 0 ? $", {barrels} fermenters ready" : ""));
        }

        // ---------- Cooking ----------

        static readonly MethodInfo GetSlot = AccessTools.Method(typeof(CookingStation), "GetSlot");
        static readonly MethodInfo SetSlot = AccessTools.Method(typeof(CookingStation), "SetSlot");
        static readonly MethodInfo GetConversion = AccessTools.Method(typeof(CookingStation), "GetItemConversion");

        // Everything on cooking stations, ovens and iron cooking stations is done (or burnt) now.
        internal static void Cook(bool burn)
        {
            if (GetSlot == null || SetSlot == null || GetConversion == null)
            {
                Report("Cook", "not available in this game version");
                return;
            }
            Type status = GetSlot.GetParameters()[3].ParameterType.GetElementType();
            object done = Enum.ToObject(status, 1), burnt = Enum.ToObject(status, 2);
            int count = 0;
            foreach (var station in Around<CookingStation>(Range))
            {
                if (Own(station) == null) continue;
                for (int i = 0; i < station.m_slots.Length; i++)
                {
                    var args = new object[] { i, null, null, null, null };
                    GetSlot.Invoke(station, args);
                    string item = (string)args[1];
                    if (string.IsNullOrEmpty(item) || args[3].Equals(burnt)) continue;
                    var conversion = GetConversion.Invoke(station, new object[] { item });
                    if (conversion == null) continue;
                    var tr = Traverse.Create(conversion);
                    var from = tr.Field("m_from").GetValue<ItemDrop>();
                    var to = tr.Field("m_to").GetValue<ItemDrop>();
                    float time = tr.Field("m_cookTime").GetValue<float>();
                    bool cheated = (bool)args[4];
                    if (burn && station.m_overCookedItem != null)
                        SetSlot.Invoke(station, new object[] { i, station.m_overCookedItem.name, time * 2f + 1f, burnt, cheated });
                    else if (!burn && from != null && to != null && item == from.name)
                        SetSlot.Invoke(station, new object[] { i, to.name, time + 0.1f, done, cheated });
                    else continue;
                    count++;
                }
            }
            Report(burn ? "Burn food" : "Cook food", count == 0 ? "nothing cooking nearby" : $"{count} {(burn ? "burnt" : "done")}");
        }

        // ---------- Beehives and sap ----------

        internal static void FillHives()
        {
            int count = 0;
            foreach (var hive in Around<Beehive>(Range))
            {
                var view = Own(hive);
                if (view == null) continue;
                view.GetZDO().Set(ZDOVars.s_level, hive.m_maxHoney);
                count++;
            }
            foreach (var sap in Around<SapCollector>(Range))
            {
                var view = Own(sap);
                if (view == null) continue;
                view.GetZDO().Set(ZDOVars.s_level, sap.m_maxLevel);
                count++;
            }
            Report("Fill beehives", count == 0 ? "no beehives or sap collectors nearby" : $"{count} full");
        }

        // ---------- Pickables ----------

        // Berry bushes, mushrooms, thistle, flax: grown back now, through the game's own RPC.
        internal static void RegrowPickables()
        {
            int count = 0;
            foreach (var p in Around<Pickable>(Range))
            {
                if (p.m_respawnTimeMinutes <= 0f || p.CanBePicked()) continue;
                var view = Own(p);
                if (view == null) continue;
                view.GetZDO().Set(ZDOVars.s_pickedTime, 0L);
                view.InvokeRPC(ZNetView.Everybody, "RPC_SetPicked", false);
                count++;
            }
            Report("Regrow", count == 0 ? "nothing picked nearby" : $"{count} grown back");
        }

        // ---------- Creatures ----------

        static readonly MethodInfo GrownPrefab = AccessTools.Method(typeof(Growup), "GetPrefab");

        // Young wolves, boars, lox, askvins: grown up now, the way Growup does it when their time comes.
        internal static void GrowUpYoung()
        {
            int count = 0;
            foreach (var young in Around<Growup>(Range * 2f))
            {
                var view = Own(young);
                if (view == null || GrownPrefab == null) continue;
                var prefab = GrownPrefab.Invoke(young, null) as GameObject;
                if (prefab == null) continue;
                var was = young.GetComponent<Character>();
                var adult = UnityEngine.Object.Instantiate(prefab, young.transform.position, young.transform.rotation).GetComponent<Character>();
                if (was != null && adult != null)
                {
                    if (young.m_inheritTame) adult.SetTamed(was.IsTamed());
                    adult.SetLevel(was.GetLevel());
                }
                view.Destroy();
                count++;
            }
            Report("Grow up", count == 0 ? "no young creatures nearby" : $"{count} grown up");
        }

        internal static void HealTames()
        {
            int count = 0;
            var player = Player.m_localPlayer;
            if (player == null) return;
            foreach (var c in Character.GetAllCharacters())
            {
                if (c == null || c.IsPlayer() || !c.IsTamed() || c.IsDead() ||
                    Vector3.Distance(c.transform.position, player.transform.position) > Range * 2f) continue;
                if (c.GetHealth() >= c.GetMaxHealth()) continue;
                if (Own(c) == null) continue;
                c.SetHealth(c.GetMaxHealth());
                count++;
            }
            Report("Heal tames", count == 0 ? "no hurt tames nearby" : $"{count} healed");
        }

        // Stars: level 1 is none, each level above is one star (the game spawns up to two).
        internal static void SetStars(Character c, int stars)
        {
            if (c == null || Own(c) == null) return;
            c.SetLevel(stars + 1);
            Report("Stars", $"{Localization.instance.Localize(c.m_name)} now has {stars}");
        }

        // ---------- Terrain ----------

        static readonly AccessTools.FieldRef<TerrainComp, bool[]> ModifiedHeight = AccessTools.FieldRefAccess<TerrainComp, bool[]>("m_modifiedHeight");
        static readonly AccessTools.FieldRef<TerrainComp, float[]> LevelDelta = AccessTools.FieldRefAccess<TerrainComp, float[]>("m_levelDelta");
        static readonly AccessTools.FieldRef<TerrainComp, float[]> SmoothDelta = AccessTools.FieldRefAccess<TerrainComp, float[]>("m_smoothDelta");
        static readonly AccessTools.FieldRef<TerrainComp, bool[]> ModifiedPaint = AccessTools.FieldRefAccess<TerrainComp, bool[]>("m_modifiedPaint");
        static readonly AccessTools.FieldRef<TerrainComp, Heightmap> Hmap = AccessTools.FieldRefAccess<TerrainComp, Heightmap>("m_hmap");
        static readonly AccessTools.FieldRef<TerrainComp, bool> Initialized = AccessTools.FieldRefAccess<TerrainComp, bool>("m_initialized");
        static readonly MethodInfo SaveTerrain = AccessTools.Method(typeof(TerrainComp), "Save");

        // The ground within the radius goes back to how the world made it: digging, raising, levelling, paths and
        // cultivation are undone. Pieces standing there stay.
        internal static void UndoTerrain(float radius)
        {
            var player = Player.m_localPlayer;
            if (player == null) return;
            Vector3 center = player.transform.position;
            int points = 0;
            foreach (var comp in Around<TerrainComp>(radius + 64f))
            {
                if (!Initialized(comp)) continue;
                var hmap = Hmap(comp);
                var heights = ModifiedHeight(comp);
                if (hmap == null || heights == null) continue;
                int width = hmap.m_width, pitch = width + 1;
                float scale = hmap.m_scale;
                Vector3 origin = comp.transform.position;
                bool changed = false;
                var level = LevelDelta(comp);
                var smooth = SmoothDelta(comp);
                var paint = ModifiedPaint(comp);
                for (int y = 0; y < pitch; y++)
                for (int x = 0; x < pitch; x++)
                {
                    int i = y * pitch + x;
                    if (!heights[i] && !paint[i]) continue;
                    var p = new Vector3(origin.x + (x - width / 2f) * scale, 0f, origin.z + (y - width / 2f) * scale);
                    if (Utils.DistanceXZ(p, center) > radius) continue;
                    if (Own(comp) == null) break;
                    heights[i] = false;
                    level[i] = 0f;
                    smooth[i] = 0f;
                    paint[i] = false;
                    changed = true;
                    points++;
                }
                if (!changed) continue;
                SaveTerrain?.Invoke(comp, new object[] { false });
                hmap.Poke(0, false);
            }
            ClutterSystem.instance?.ResetGrass(center, radius);
            Report("Undo terrain", points == 0 ? "no changed ground nearby" : $"ground restored within {radius:0} m");
        }

        // ---------- Snow ----------

        internal static void ClearSnow()
        {
            int count = 0;
            foreach (var wnt in WearNTear.GetAllInstances())
            {
                var player = Player.m_localPlayer;
                if (wnt == null || player == null || wnt.m_snowBuildup <= 0f ||
                    Vector3.Distance(wnt.transform.position, player.transform.position) > Range) continue;
                var view = wnt.GetComponent<ZNetView>();
                if (view == null || !view.IsValid()) continue;
                view.InvokeRPC(ZNetView.Everybody, "RPC_SetSnow", 0f);
                count++;
            }
            Report("Clear snow", count == 0 ? "no snow on pieces nearby" : $"{count} pieces cleared");
        }

        // ---------- Counting ----------

        internal struct Counts
        {
            internal int Pieces, Drops, Creatures, Objects;
        }

        internal static Counts Count(float range)
        {
            var counts = new Counts();
            var player = Player.m_localPlayer;
            if (player == null) return counts;
            Vector3 p = player.transform.position;
            var pieces = new List<Piece>();
            Piece.GetAllPiecesInRadius(p, range, pieces);
            counts.Pieces = pieces.Count;
            counts.Drops = Around<ItemDrop>(range).Count;
            foreach (var c in Character.GetAllCharacters())
                if (c != null && !c.IsPlayer() && Vector3.Distance(c.transform.position, p) <= range) counts.Creatures++;
            foreach (var view in ZNetScene.instance != null ? UnityEngine.Object.FindObjectsByType<ZNetView>(FindObjectsSortMode.None) : new ZNetView[0])
                if (view != null && Vector3.Distance(view.transform.position, p) <= range) counts.Objects++;
            return counts;
        }
    }
}
