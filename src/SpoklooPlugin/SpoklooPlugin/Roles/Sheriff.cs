using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using SpoklooPlugin.Roles;
using UnityEngine;

namespace SpoklooPlugin
{
    public partial class SpoklooPlugin
    {
        private static float _waitSeconds = 6f;
        private static readonly int OutlineColor = Shader.PropertyToID("_OutlineColor");

        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
        public static class SheriffKillButton
        {
            public static void Postfix(PlayerControl __instance)
            {
                if (!__instance.AmOwner) return;
                
                var killButton = DestroyableSingleton<HudManager>.Instance.KillButton;
                CustomRoles.TryGetValue(__instance.PlayerId, out var roles);
                if (roles != null && roles.Contains(Sheriff.Instance) && !__instance.Data.IsDead && !LobbyBehaviour.Instance)
                {
                    if (!roles[0].HasKillButton) return;
                    
                    _waitSeconds -= Time.deltaTime;
                    if (!(_waitSeconds < 0)) return;
                    killButton.gameObject.SetActive(true);
                    PlayerControl.LocalPlayer.SetKillTimer(Mathf.Max(0f, PlayerControl.LocalPlayer.killTimer - Time.fixedDeltaTime));
                    var target = __instance.FindClosestTarget();
                    killButton.SetTarget(target);
                }
                else if (!__instance.Data.IsImpostor)
                {
                    killButton.gameObject.SetActive(false);
                }
            }
        }


        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.SetTasks))]
        public static class KillButtonPatch
        {
            public static void Postfix()
            {
                _waitSeconds = 6f;
            }
        }


        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.MurderPlayer))]
        public static class SheriffMurdersPlayer
        {
            public static void Prefix(PlayerControl __instance, out bool __state)
            {
                CustomRoles.TryGetValue(__instance.PlayerId, out var roles);
                __state = roles != null && roles.Contains(Sheriff.Instance);

                if (!__state) return;
                __instance.Data.IsImpostor = true;
                __instance.Data.IsDead = false;
            }

            public static void Postfix(PlayerControl __instance, bool __state, [HarmonyArgument(0)] PlayerControl target)
            {
                if (!__state) return;
                __instance.Data.IsImpostor = false;
                if (!target.Data.IsImpostor && target.PlayerId != __instance.PlayerId)
                {
                    __instance.Data.IsDead = true;
                    __instance.MurderPlayer(__instance);
                }
            }
        }


        [HarmonyPatch(typeof(KeyboardJoystick), nameof(KeyboardJoystick.Method_97))]
        public static class SheriffPressAToKill
        {
            public static void Prefix(out bool __state)
            {
                __state = false;
                if (!Input.GetKeyDown(KeyCode.A)) return;
                CustomRoles.TryGetValue(PlayerControl.LocalPlayer.PlayerId, out var roles);
                __state = roles != null && roles.Contains(Sheriff.Instance);
                if (__state && PlayerControl.LocalPlayer.Data != null)
                {
                    PlayerControl.LocalPlayer.Data.IsImpostor = true;
                }
            }

            public static void Postfix(bool __state)
            {
                if (!Input.GetKeyDown(KeyCode.A)) return;
                if (__state && PlayerControl.LocalPlayer.Data != null)
                {
                    PlayerControl.LocalPlayer.Data.IsImpostor = false;                    
                }
            }
        }


        [HarmonyPatch(typeof(KillButtonManager), nameof(KillButtonManager.SetTarget))]
        public static class SheriffKillOutlineColor
        {
            public static void Postfix(KillButtonManager __instance)
            {
                CustomRoles.TryGetValue(PlayerControl.LocalPlayer.PlayerId, out var roles);
                if (roles != null && roles.Contains(Sheriff.Instance) && __instance.CurrentTarget)
                {
                    var component = __instance.CurrentTarget.GetComponent<SpriteRenderer>();
                    component.material.SetColor(OutlineColor, roles[0].Color);
                }
            }
        }


        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FindClosestTarget))]
        public static class SheriffTargetPatch
        {
            public static bool Prefix(PlayerControl __instance, out bool __state)
            {
                CustomRoles.TryGetValue(__instance.PlayerId, out var roles);
                __state = roles != null && roles.Contains(Sheriff.Instance);
                return !__state;
            }
            
            public static void Postfix(PlayerControl __instance, ref PlayerControl __result, bool __state)
            {
                if (!__state) return;
                __result = null;
                var num = GameOptionsData.KillDistances[Mathf.Clamp(PlayerControl.GameOptions.KillDistance, 0, 2)];
                if (!ShipStatus.Instance)
                {
                    return;
                }
                
                var truePosition = __instance.GetTruePosition();
                var allPlayers = GameData.Instance.AllPlayers;
                foreach (var playerInfo in allPlayers)
                {
                    if (!playerInfo.Disconnected && playerInfo.PlayerId != __instance.PlayerId && !playerInfo.IsDead)
                    {
                        var @object = playerInfo.Object;
                        if (@object)
                        {
                            var vector = @object.GetTruePosition() - truePosition;
                            var magnitude = vector.magnitude;
                            if (magnitude <= num && !PhysicsHelpers.AnyNonTriggersBetween(truePosition, vector.normalized, magnitude, Constants.ShipAndObjectsMask))
                            {
                                __result = @object;
                                num = magnitude;
                            }
                        }
                    }
                }
            }
        }
    }
}
