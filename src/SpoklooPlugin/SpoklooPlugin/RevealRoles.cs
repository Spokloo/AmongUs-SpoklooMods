using System.Linq;
using HarmonyLib;
using SpoklooPlugin.Roles;
using UnityEngine;

namespace SpoklooPlugin
{
    public class RevealRoles
    {
        private static bool _areNameColorsShown;
        private static bool _impLoverNameColor;
        private static bool _impLoverMeetingColor;

        
        [HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.BeginCrewmate))]
        public static class ResetBeginCrew
        {
            public static void Postfix()
            {
                _areNameColorsShown = false;
                _impLoverNameColor = true;
                _impLoverMeetingColor = true;
            }
        }
        
        
        [HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.BeginImpostor))]
        public static class ResetBeginImp
        {
            public static void Postfix()
            {
                _areNameColorsShown = false;
                _impLoverNameColor = true;
                _impLoverMeetingColor = true;
            }
        }


        [HarmonyPatch(typeof(KeyboardJoystick), nameof(KeyboardJoystick.Method_97))]
        public static class GhostsPressXToRevealRoles
        {
            public static void Postfix()
            {
                if (!Input.GetKeyDown(KeyCode.X)) return;
                if (!PlayerControl.LocalPlayer.Data.IsDead) return;
                
                _areNameColorsShown = true;

                foreach (var (playerId, roles) in SpoklooPlugin.CustomRoles)
                {
                    var player = GameData.Instance.GetPlayerById(playerId);
                    if (!player.Disconnected) player.Object.nameText.Color = roles[0].Color;
                }

                foreach (var player in GameData.Instance.AllPlayers)
                {
                    if (!player.Disconnected && player.IsImpostor)
                    {
                        player.Object.nameText.Color = Palette.ImpostorRed;
                        
                        SpoklooPlugin.CustomRoles.TryGetValue(player.PlayerId, out var roles);
                        if (roles != null && (roles.Contains(Lover1.Instance) || roles.Contains(Lover2.Instance)))
                        {
                            player.Object.nameText.Color = roles[0].Color;
                            if (_impLoverNameColor)
                            {
                                player.Object.nameText.Text += "\n[FF1919FF]Impostor[]";
                                var nameGameObject = player.Object.nameText.gameObject;
                                var position = nameGameObject.transform.localPosition;
                                nameGameObject.transform.localPosition = player.HatId != 0 ? new Vector3(position.x, position.y + 0.15f, position.z) : new Vector3(position.x, position.y + 0.4f, position.z);
                                
                                _impLoverNameColor = false;
                            }
                        }
                    }
                }
            }
        }
        
        
        [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Method_129))]
        public static class RevealNameColorInMeeting
        {
            public static void Postfix([HarmonyArgument(0)] GameData.PlayerInfo player, ref PlayerVoteArea __result)
            {
                if (!_areNameColorsShown || !PlayerControl.LocalPlayer.Data.IsDead) return;

                if (!player.Disconnected && SpoklooPlugin.CustomRoles.ContainsKey(player.PlayerId))
                {
                    var role = SpoklooPlugin.CustomRoles[player.PlayerId];
                    __result.NameText.Color = role[0].Color;
                }
                    
                if (!player.Disconnected && player.IsImpostor)
                {
                    __result.NameText.Color = Palette.ImpostorRed;
                    
                    SpoklooPlugin.CustomRoles.TryGetValue(player.PlayerId, out var roles);
                    if (roles != null && (roles.Contains(Lover1.Instance) || roles.Contains(Lover2.Instance)))
                    {
                        __result.NameText.Color = roles[0].Color;
                        if (_impLoverMeetingColor)
                        {
                            __result.NameText.Text += "\n[FF1919FF]Impostor[]";
                            _impLoverMeetingColor = false;
                        }
                    }
                }
            }
        }
    }
}