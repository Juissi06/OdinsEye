using HarmonyLib;
using UnityEngine;

namespace OdinsEye
{
    // The fun cheats (Player tab): size, jump, speed and the switches. Like every Odin's Eye cheat they only act
    // while the game allows cheats (Cheats.Allowed), and everything snaps back to normal when it doesn't.
    internal class Fun : MonoBehaviour
    {
        internal static bool On(BepInEx.Configuration.ConfigEntry<bool> entry) => entry.Value && Cheats.Allowed;

        internal static float Size => Cheats.Allowed ? Plugin.FunSize.Value : 1f;
        internal static float Jump => Cheats.Allowed ? Plugin.FunJump.Value : 1f;
        internal static float Speed => Cheats.Allowed ? Plugin.FunSpeed.Value : 1f;
        internal static float ShipSpeed => Cheats.Allowed ? Plugin.FunShip.Value : 1f;

        // The ship you're steering and its own forces, put back when you let go of the rudder.
        Ship _ship;
        float _sailForce, _rowForce;

        // The player's own values, kept so the multipliers never stack and can be undone. Nothing is touched
        // while every multiplier is 1, so other mods that change these values aren't overridden.
        Player _player;
        float _speed, _walk, _run, _crouch, _swim, _jump, _jumpForward;
        bool _moved, _scaled;
        float _repairTimer;

        void Update()
        {
            var player = Player.m_localPlayer;
            if (player != _player) _moved = _scaled = false;
            _player = player;
            if (player == null) return;

            float speed = Speed, jump = Jump;
            bool moving = speed != 1f || jump != 1f;
            if (!_moved)
            {
                _speed = player.m_speed;
                _walk = player.m_walkSpeed;
                _run = player.m_runSpeed;
                _crouch = player.m_crouchSpeed;
                _swim = player.m_swimSpeed;
                _jump = player.m_jumpForce;
                _jumpForward = player.m_jumpForceForward;
            }
            if (moving || _moved)
            {
                player.m_speed = _speed * speed;
                player.m_walkSpeed = _walk * speed;
                player.m_runSpeed = _run * speed;
                player.m_crouchSpeed = _crouch * speed;
                player.m_swimSpeed = _swim * speed;
                player.m_jumpForce = _jump * jump;
                player.m_jumpForceForward = _jumpForward * jump;
                _moved = moving;
            }

            float size = Size;
            if (size != 1f || _scaled)
            {
                var scale = Vector3.one * size;
                if (player.transform.localScale != scale) player.transform.localScale = scale;
                _scaled = size != 1f;
            }

            UpdateShip(player.GetControlledShip());

            // No wear: keep everything you carry at full durability, a couple of times a second.
            _repairTimer -= Time.deltaTime;
            if (_repairTimer <= 0f && On(Plugin.FunNoWear))
            {
                _repairTimer = 0.5f;
                foreach (var item in player.GetInventory().GetAllItems())
                    if (item.m_shared.m_useDurability && item.m_durability < item.GetMaxDurability())
                        item.m_durability = item.GetMaxDurability();
            }
        }

        // Ship speed: more sail force (Ship.GetSailForce) and rowing force while you steer. The ship's drag
        // grows with speed, so the top speed rises with the force without running away.
        void UpdateShip(Ship ship)
        {
            if (ship != _ship)
            {
                if (_ship != null)
                {
                    _ship.m_sailForceFactor = _sailForce;
                    _ship.m_backwardForce = _rowForce;
                }
                _ship = ship;
                if (ship != null)
                {
                    _sailForce = ship.m_sailForceFactor;
                    _rowForce = ship.m_backwardForce;
                }
            }
            if (ship == null) return;
            float m = ShipSpeed;
            // Only the ship's owner moves it, and that's whoever came aboard first, not the one steering. With
            // the speed changed, the one steering takes the ship over so it applies.
            var nview = ship.GetComponent<ZNetView>();
            if (!Mathf.Approximately(m, 1f) && nview != null && nview.IsValid() && !nview.IsOwner()) nview.ClaimOwnership();
            ship.m_sailForceFactor = _sailForce * m;
            ship.m_backwardForce = _rowForce * m;
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.UseStamina))]
    static class InfiniteStaminaPatch
    {
        static bool Prefix(Player __instance) => !(__instance == Player.m_localPlayer && Fun.On(Plugin.FunStamina));
    }

    [HarmonyPatch(typeof(Player), nameof(Player.IsEncumbered))]
    static class NoWeightLimitPatch
    {
        static void Postfix(Player __instance, ref bool __result)
        {
            if (__result && __instance == Player.m_localPlayer && Fun.On(Plugin.FunNoWeight)) __result = false;
        }
    }

    // Hits start here on the attacker's side, before they're sent to whoever owns the target, so this works on
    // creatures anywhere. Your own fall damage also comes through here (HitType.Fall).
    [HarmonyPatch(typeof(Character), nameof(Character.Damage))]
    static class HitPatch
    {
        static bool Prefix(Character __instance, HitData hit)
        {
            if (hit == null || !Cheats.Allowed) return true;
            var me = Player.m_localPlayer;
            if (me == null) return true;
            if (__instance == me) return !(hit.m_hitType == HitData.HitType.Fall && Plugin.FunNoFall.Value);
            if (__instance.IsPlayer() || hit.GetAttacker() != me) return true; // leave other players alone
            // Plain damage isn't resisted by any creature, so a big enough number always kills.
            if (Plugin.FunOneHit.Value) hit.m_damage.m_damage += __instance.GetMaxHealth() * 10f + 10000f;
            if (Plugin.FunKnockback.Value)
            {
                hit.m_pushForce = Mathf.Max(hit.m_pushForce, 20f) * 10f;
                hit.m_staggerMultiplier *= 10f;
            }
            return true;
        }
    }

    // Crafting fills a timer before the item is made; jump it to the end so the item is made right away.
    // No materials is the game's own "no build cost" switch, which covers crafting too.
    [HarmonyPatch(typeof(InventoryGui), "UpdateRecipe")]
    static class InstantCraftingPatch
    {
        static readonly AccessTools.FieldRef<InventoryGui, float> CraftTimer = AccessTools.FieldRefAccess<InventoryGui, float>("m_craftTimer");

        static void Prefix(InventoryGui __instance)
        {
            if (CraftTimer(__instance) > 0f && Fun.On(Plugin.FunInstantCraft)) CraftTimer(__instance) = 1000f;
        }
    }

    // The game sets the ambient light every frame from the weather and time of day; brighten it afterwards.
    [HarmonyPatch(typeof(EnvMan), "SetEnv")]
    static class NightVisionPatch
    {
        static readonly int AmbientColor = Shader.PropertyToID("_AmbientColor");
        static readonly Color Floor = new Color(0.55f, 0.55f, 0.6f);

        static void Postfix()
        {
            if (!Fun.On(Plugin.FunNightVision)) return;
            Color c = RenderSettings.ambientLight;
            c = new Color(Mathf.Max(c.r, Floor.r), Mathf.Max(c.g, Floor.g), Mathf.Max(c.b, Floor.b), c.a);
            RenderSettings.ambientLight = c;
            Shader.SetGlobalColor(AmbientColor, c);
        }
    }
}
