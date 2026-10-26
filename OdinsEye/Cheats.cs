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

        internal static float XpMultiplier => Allowed ? Mathf.Max(0f, Plugin.XpMultiplier.Value) : 1f;
        internal static bool KeepSkills => Allowed && Plugin.KeepSkills.Value;

        internal static bool IsLocal(Skills skills) => Player.m_localPlayer != null && skills == Player.m_localPlayer.GetSkills();
    }

    // Every skill gain goes through here; a bigger factor fills the level faster.
    [HarmonyPatch(typeof(Skills), nameof(Skills.RaiseSkill))]
    static class XpMultiplierPatch
    {
        static void Prefix(Skills __instance, ref float factor)
        {
            if (Cheats.IsLocal(__instance)) factor *= Cheats.XpMultiplier;
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
