using HarmonyLib;
using UnityEngine;

namespace SpoklooPlugin
{
    public class UnknownImpostors
    {
        [HarmonyPatch(typeof(IntroCutscene.CoBegin__d), nameof(IntroCutscene.CoBegin__d.MoveNext))]
        public static class UnknownImpostorsPatch
        {
            public static void Prefix(IntroCutscene.CoBegin__d __instance)
            {
                if (!SpoklooPlugin.UnknownImpostors.GetValue()) return;
                if (!PlayerControl.LocalPlayer.Data.IsImpostor) return;
                __instance.yourTeam = new Il2CppSystem.Collections.Generic.List<PlayerControl>();
                __instance.yourTeam.Add(PlayerControl.LocalPlayer);
            }

            public static void Postfix(IntroCutscene.CoBegin__d __instance)
            {
                if (!SpoklooPlugin.UnknownImpostors.GetValue()) return;
                if (!PlayerControl.LocalPlayer.Data.IsImpostor) return;
                
                foreach (var p in GameData.Instance.AllPlayers)
                {
                    if (p.IsImpostor)
                    {
                        p.Object.nameText.Color = Palette.White;
                    }
                }

                if (PlayerControl.LocalPlayer.Data.IsImpostor)
                {
                    PlayerControl.LocalPlayer.nameText.Color = Palette.ImpostorRed;
                }
            }
        }
        
        
        [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Method_129))]
        public static class UnknownImpostorsNameMeetingPatch
        {
            public static void Postfix([HarmonyArgument(0)] GameData.PlayerInfo player, ref PlayerVoteArea __result)
            {
                if (!SpoklooPlugin.UnknownImpostors.GetValue()) return;
                if (!PlayerControl.LocalPlayer.Data.IsImpostor) return;
                
                if (player.IsImpostor)
                {
                    __result.NameText.Color = player.Object.nameText.Color;
                }
            }
        }
        
        
        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FindClosestTarget))]
        public static class UnknownImpostorsTargetPatch
        {
            public static bool Prefix(PlayerControl __instance, out bool __state)
            {
                __state = PlayerControl.LocalPlayer.Data.IsImpostor && SpoklooPlugin.UnknownImpostors.GetValue();
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