using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace OdinsEye
{
    // Creatures tab: the game's commands for creatures around you (tame, aggravate, kill tames, passive monsters),
    // freezing every creature's AI, and a list of what's nearby with its own tame and kill buttons.
    internal static class CreaturesTab
    {
        const float LeftW = 330f, Gap = 20f, RowH = 30f, Range = 40f;

        // Freeze AI: session only, so creatures never stay frozen by surprise after a restart.
        internal static bool Frozen;

        static readonly List<Character> Nearby = new List<Character>();
        static float _nextScan;
        static Vector2 _scroll;

        static readonly System.Action<Tameable> Tame =
            AccessTools.MethodDelegate<System.Action<Tameable>>(AccessTools.Method(typeof(Tameable), "Tame"));

        internal static void Draw(Rect r)
        {
            var player = Player.m_localPlayer;
            if (player == null) return;
            DrawCommands(r.x, r.y, LeftW);
            DrawNearby(new Rect(r.x + LeftW + Gap, r.y, r.width - LeftW - Gap, r.height), player);
        }

        static void DrawCommands(float x, float y, float w)
        {
            Widgets.Header(x, ref y, w, "Around you");
            Widgets.Command(new Rect(x, y, w, Widgets.RowH - 4f), "Tame every tameable creature", "tame");
            y += Widgets.RowH;
            Widgets.Note(x, ref y, w, "The game tames every one that's loaded, not just nearby.");
            Widgets.Command(new Rect(x, y, w, Widgets.RowH - 4f), "Make neutral creatures angry", "aggravate");
            y += Widgets.RowH;
            Widgets.Command(new Rect(x, y, w, Widgets.RowH - 4f), "Kill tamed creatures", "killtame", "Kill every tamed creature nearby?");
            y += Widgets.RowH;
            Widgets.CheatPair(x, ref y, w, "Grow up young", OdinsEye.Nearby.GrowUpYoung, "Heal tames", OdinsEye.Nearby.HealTames);
            Widgets.Note(x, ref y, w, "Young wolves, boars, lox and askvins become adults.");
            y += 6f;

            Widgets.Header(x, ref y, w, "Behaviour");
            bool passive = ZoneSystem.instance != null && ZoneSystem.instance.GetGlobalKey(GlobalKeys.PassiveMobs);
            Widgets.Switch(new Rect(x, y + 2f, w, 24f), "Passive monsters", passive, "setkey PassiveMobs", "removekey PassiveMobs");
            y += 28f;
            Widgets.Note(x, ref y, w, "World setting: monsters don't attack until you hit them.");
            Frozen = Widgets.CheatSwitch(new Rect(x, y + 2f, w, 24f), "Freeze creatures", Frozen) && Cheats.Allowed;
            y += 28f;
            Widgets.Note(x, ref y, w, "They stand still and do nothing. For building and screenshots.");
        }

        // ---------- Nearby list ----------

        static void DrawNearby(Rect r, Player player)
        {
            if (Time.time >= _nextScan)
            {
                _nextScan = Time.time + 0.5f;
                Nearby.Clear();
                Vector3 p = player.transform.position;
                foreach (var c in Character.GetAllCharacters())
                    if (c != null && !c.IsPlayer() && !c.IsDead() && Vector3.Distance(c.transform.position, p) <= Range) Nearby.Add(c);
                Nearby.Sort((a, b) => Vector3.Distance(a.transform.position, p).CompareTo(Vector3.Distance(b.transform.position, p)));
            }

            Ui.Label(new Rect(r.x, r.y, r.width, Widgets.HeaderH), $"<color={Ui.Gold}>Within {Range:0} m</color> <color={Ui.Muted}>({Nearby.Count})</color>", Ui.Text);
            var area = new Rect(r.x, r.y + Widgets.HeaderH + 4f, r.width, r.height - Widgets.HeaderH - 4f);
            Ui.Inset(area);
            var inner = new Rect(area.x + 4f, area.y + 4f, area.width - 8f, area.height - 8f);
            if (Nearby.Count == 0)
            {
                Ui.Label(new Rect(inner.x + 6f, inner.y, inner.width, 24f), $"<color={Ui.Muted}>No creatures nearby.</color>", Ui.Small);
                return;
            }
            var view = new Rect(0f, 0f, inner.width - 20f, Nearby.Count * RowH);
            _scroll = GUI.BeginScrollView(inner, _scroll, view);
            for (int i = 0; i < Nearby.Count; i++)
                if (Nearby[i] != null) DrawRow(new Rect(0f, i * RowH, view.width, RowH - 3f), Nearby[i], player);
            GUI.EndScrollView();
        }

        static void DrawRow(Rect r, Character c, Player player)
        {
            string name = Localization.instance.Localize(c.m_name);
            int stars = c.GetLevel() - 1;
            string star = stars > 0 ? $" <color={Ui.Gold}>{new string('*', stars)}</color>" : "";
            string tamed = c.IsTamed() ? $" <color={Ui.Good}>tamed</color>" : "";
            Ui.Label(new Rect(r.x + 4f, r.y, r.width - 250f, r.height), $"{name}{star}{tamed}", Ui.Small);

            float dist = Vector3.Distance(c.transform.position, player.transform.position);
            Ui.Bar(new Rect(r.xMax - 240f, r.y + r.height * 0.5f - 5f, 70f, 10f), c.GetHealthPercentage(), new Color(0.75f, 0.2f, 0.15f));
            Ui.Label(new Rect(r.xMax - 164f, r.y, 40f, r.height), $"<color={Ui.Muted}>{dist:0}m</color>", Ui.Small);

            bool was = GUI.enabled;
            GUI.enabled = was && Cheats.Allowed;
            if (GUI.Button(new Rect(r.xMax - 122f, r.y, 64f, r.height), new GUIContent(c.IsTamed() ? "Untame" : "Tame", Widgets.OwnCheat)))
                SetTamed(c, !c.IsTamed());
            if (GUI.Button(new Rect(r.xMax - 54f, r.y, 54f, r.height), new GUIContent("Kill", Widgets.OwnCheat)))
                Kill(c);
            GUI.enabled = was;
        }

        // Tameable.Tame (the game's own taming, with its effects) where the creature has one; otherwise just
        // mark it tamed, which makes it friendly.
        internal static void SetTamed(Character c, bool tamed)
        {
            // Only the creature's owner can tame it; another player nearby may own it.
            var nview = c.GetComponent<ZNetView>();
            if (nview != null && nview.IsValid() && !nview.IsOwner()) nview.ClaimOwnership();
            var tameable = c.GetComponent<Tameable>();
            if (tamed && tameable != null && Tame != null) Tame(tameable);
            else c.SetTamed(tamed);
        }

        static void Kill(Character c)
        {
            var hit = new HitData();
            hit.m_damage.m_damage = c.GetMaxHealth() * 10f + 10000f;
            hit.m_point = c.transform.position;
            c.Damage(hit);
        }
    }

    // Frozen creatures skip their AI and stand still.
    [HarmonyPatch]
    static class FreezeAIPatch
    {
        static IEnumerable<System.Reflection.MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(BaseAI), nameof(BaseAI.UpdateAI));
            yield return AccessTools.Method(typeof(MonsterAI), nameof(MonsterAI.UpdateAI));
        }

        static bool Prefix(BaseAI __instance, ref bool __result)
        {
            if (!CreaturesTab.Frozen || !Cheats.Allowed) return true;
            __instance.StopMoving();
            __result = false;
            return false;
        }
    }
}
