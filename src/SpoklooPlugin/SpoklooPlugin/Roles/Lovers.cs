using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Hazel;
using SpoklooPlugin.Roles;
using UnityEngine;

namespace SpoklooPlugin
{
    public partial class SpoklooPlugin
    {
        public static PlayerControl FirstLover;
        public static PlayerControl SecondLover;
        
        
        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.SetTasks))]
        public static class LoversSetup
        {
            public static void Prefix()
            {
                foreach (var player in PlayerControl.AllPlayerControls)
                {
                    CustomRoles.TryGetValue(player.PlayerId, out var roles);
                    if (roles != null && roles.Contains(Lover1.Instance))
                    {
                        FirstLover = player;
                    }

                    if (roles != null && roles.Contains(Lover2.Instance))
                    {
                        SecondLover = player;
                    }
                }
            }
        }
        
        
        [HarmonyPatch(typeof(IntroCutscene.CoBegin__d), nameof(IntroCutscene.CoBegin__d.MoveNext))]
        public static class LoversIntroPatch
        {
            public static void Prefix(IntroCutscene.CoBegin__d __instance)
            {
                CustomRoles.TryGetValue(PlayerControl.LocalPlayer.PlayerId, out var roles1);
                if (roles1 != null && (roles1.Contains(Lover1.Instance) || roles1.Contains(Lover2.Instance)))
                {
                    var otherLover = PlayerControl.LocalPlayer == FirstLover ? SecondLover : FirstLover;
                    __instance.yourTeam = new Il2CppSystem.Collections.Generic.List<PlayerControl>();
                    __instance.yourTeam.Add(PlayerControl.LocalPlayer);
                    __instance.yourTeam.Add(otherLover);
                }
            }

            public static void Postfix()
            {
                CustomRoles.TryGetValue(PlayerControl.LocalPlayer.PlayerId, out var roles);
                if (roles != null && (roles.Contains(Lover1.Instance) || roles.Contains(Lover2.Instance)))
                {
                    var otherLover = PlayerControl.LocalPlayer == FirstLover ? SecondLover : FirstLover;
                    otherLover.nameText.Color = roles[0].Color;
                }
            }
        }
        
        
        [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Method_129))]
        public static class LoversNameColorInMeeting
        {
            public static void Postfix([HarmonyArgument(0)] GameData.PlayerInfo player, ref PlayerVoteArea __result)
            {
                CustomRoles.TryGetValue(PlayerControl.LocalPlayer.PlayerId, out var roles);
                if (roles != null && (roles.Contains(Lover1.Instance) || roles.Contains(Lover2.Instance)))
                {
                    CustomRoles.TryGetValue(player.PlayerId, out var roles1);
                    if (!player.Disconnected && roles1 != null && (roles1.Contains(Lover1.Instance) || roles1.Contains(Lover2.Instance)))
                    {
                        __result.NameText.Color = roles1[0].Color;
                    }
                }
            }
        }


        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Die))]
        public static class LoversSuicideMurder
        {
            public static bool Prefix(PlayerControl __instance)
            {
                __instance.Data.IsDead = true;
            
                CustomRoles.TryGetValue(__instance.PlayerId, out var roles);
                var isLover = roles != null && (roles.Contains(Lover1.Instance) || roles.Contains(Lover2.Instance));
                if (!isLover) return true;
                
                var otherLover = __instance == FirstLover ? SecondLover : FirstLover;
                if (otherLover.Data.IsDead) return true;
                
                otherLover.Data.IsImpostor = true;
                otherLover.Data.IsDead = false;

                return true;
            }

            public static void Postfix(PlayerControl __instance)
            {
                CustomRoles.TryGetValue(__instance.PlayerId, out var roles);
                var isLover = roles != null && (roles.Contains(Lover1.Instance) || roles.Contains(Lover2.Instance));
                if (!isLover) return;
                
                var otherLover = __instance == FirstLover ? SecondLover : FirstLover;
                otherLover.MurderPlayer(otherLover);
                otherLover.Data.IsImpostor = false;
                otherLover.Data.IsDead = true;
            }
        }
        
        
        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Exiled))]
        public static class LoversSuicideExiled
        {
            public static void Postfix(PlayerControl __instance)
            {
                CustomRoles.TryGetValue(__instance.PlayerId, out var roles);
                if (roles != null && (roles.Contains(Lover1.Instance) || roles.Contains(Lover2.Instance)))
                {
                    var otherLover = __instance == FirstLover ? SecondLover : FirstLover;
                    if (otherLover.Data.IsDead) return;
                    otherLover.Exiled();
                }
            }
        }
    }
}
