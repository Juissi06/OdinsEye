using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace OdinsEye
{
    // Odin's Eye's own cheats, for things the game has no console command for. They follow the same rule as the
    // game's cheats: only in single player or a world you host, with dev commands on and confirmed, and they stop
    // working the moment dev commands are turned off.
    internal static class Cheats
    {
        internal static bool Allowed => Commands.Access == Access.Ready;

        // A skill's own speed when it has one, otherwise the speed for all skills.
        internal static float XpMultiplier(Skills.SkillType skill)
        {
            if (!Allowed) return 1f;
            return SkillSpeeds.TryGetValue(skill, out float own) ? own : Mathf.Max(0f, Plugin.XpMultiplier.Value);
        }

        static readonly Dictionary<Skills.SkillType, float> Speeds = new Dictionary<Skills.SkillType, float>();
        static string _speedsFor;

        // "Skill speed per skill" in the config: skill number=speed pairs. Numbers so modded skills work too.
        internal static Dictionary<Skills.SkillType, float> SkillSpeeds
        {
            get
            {
                string text = Plugin.SkillSpeeds.Value ?? "";
                if (text == _speedsFor) return Speeds;
                _speedsFor = text;
                Speeds.Clear();
                foreach (var pair in text.Split(','))
                {
                    var p = pair.Split('=');
                    if (p.Length == 2 && int.TryParse(p[0].Trim(), out int skill) &&
                        float.TryParse(p[1].Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float speed))
                        Speeds[(Skills.SkillType)skill] = Mathf.Max(0f, speed);
                }
                return Speeds;
            }
        }

        // null: the skill follows the speed for all skills again.
        internal static void SetSkillSpeed(Skills.SkillType skill, float? speed)
        {
            var speeds = new Dictionary<Skills.SkillType, float>(SkillSpeeds);
            if (speed.HasValue) speeds[skill] = speed.Value;
            else speeds.Remove(skill);
            var parts = new List<string>();
            foreach (var kv in speeds) parts.Add(((int)kv.Key).ToString() + "=" + kv.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
            Plugin.SkillSpeeds.Value = string.Join(",", parts.ToArray());
        }

        internal static bool KeepSkills => Allowed && Plugin.KeepSkills.Value;

        internal static bool IsLocal(Skills skills) => Player.m_localPlayer != null && skills == Player.m_localPlayer.GetSkills();
    }

    // Every skill gain goes through here; a bigger factor fills the level faster.
    [HarmonyPatch(typeof(Skills), nameof(Skills.RaiseSkill))]
    static class XpMultiplierPatch
    {
        static void Prefix(Skills __instance, Skills.SkillType skillType, ref float factor)
        {
            if (Cheats.IsLocal(__instance)) factor *= Cheats.XpMultiplier(skillType);
        }
    }

    // Dying lowers every skill (Skills.OnDeath), or wipes them all with the "skills reset on death" world modifier.
    [HarmonyPatch(typeof(Skills), nameof(Skills.OnDeath))]
    static class KeepSkillsPatch
    {
        static bool Prefix(Skills __instance) => !(Cheats.KeepSkills && Cheats.IsLocal(__instance));
    }

    [HarmonyPatch(typeof(Player), "OnDeath")]
    static class PlayerDeathPatch
    {
        internal static bool Dying;

        static void Prefix(Player __instance) => Dying = __instance == Player.m_localPlayer;

        static void Finalizer() => Dying = false;
    }

    [HarmonyPatch(typeof(Skills), nameof(Skills.Clear))]
    static class KeepSkillsResetPatch
    {
        static bool Prefix(Skills __instance) => !(PlayerDeathPatch.Dying && Cheats.KeepSkills && Cheats.IsLocal(__instance));
    }
}
