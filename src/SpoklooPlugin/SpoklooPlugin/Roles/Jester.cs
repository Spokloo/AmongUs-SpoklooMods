using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using Hazel;
using SpoklooPlugin.Roles;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SpoklooPlugin
{
    public partial class SpoklooPlugin
    {
        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Exiled))]
        public static class JesterWinPatch
        {
            public static void Postfix(PlayerControl __instance)
            {
                if (_gameWinner != Jester.Instance)
                {
                    CustomRoles.TryGetValue(__instance.PlayerId, out var roles);
                    if (roles != null && roles.Contains(Jester.Instance))
                    {
                        _gameWinner = Jester.Instance;
                    }
                }
            }
        }


        [HarmonyPatch(typeof(GameData), nameof(GameData.RecomputeTaskCounts))]
        public static class RecomputeTaskCountsPatch
        {
            public static void Prefix(GameData __instance, out Il2CppSystem.Collections.Generic.List<GameData.TaskInfo> __state)
            {
                foreach (var (key, value) in CustomRoles)
                {
                    if (!value.Contains(Jester.Instance)) continue;
                    var player = __instance.GetPlayerById(key);
                    if (player == null) continue;
                    __state = player.Tasks;
                    player.Tasks = null;
                    return;
                }
                __state = null;
            }

            public static void Postfix(GameData __instance, Il2CppSystem.Collections.Generic.List<GameData.TaskInfo> __state)
            {
                if (__state is null) return;
                foreach (var (key, value) in CustomRoles)
                {
                    if (!value.Contains(Jester.Instance)) continue;
                    __instance.GetPlayerById(key).Tasks = __state;
                    return;
                }
            }
        }
        
        
        [HarmonyPatch(typeof(EndGameManager), nameof(EndGameManager.SetEverythingUp))]
        public static class CustomRolesEndGamePatch
        {
            public static void Prefix()
            {
                if (_gameWinner != null)
                {
                    TempData.winners = _gameWinner.GetWinners(CustomRoles);
                }
                else
                {
                    for (int i = 0; i < TempData.winners.Count; i++)
                    {
                        var winner = TempData.winners[i];
                        byte playerId = 255;
                        foreach (var player in GameData.Instance.AllPlayers)
                        {
                            if (player.PlayerName == winner.Name)
                            {
                                playerId = player.PlayerId;
                                break;
                            }
                        }
                        
                        CustomRoles.TryGetValue(playerId, out var roles);
                        if (roles != null && roles.Contains(Jester.Instance))
                        {
                            TempData.winners.RemoveAt(i);
                            i--;
                        }
                    }
                }
            }

            public static void Postfix(EndGameManager __instance)
            {
                if (_gameWinner != null)
                {
                    var poolablePlayers = Object.FindObjectsOfType<PoolablePlayer>();
                    foreach (var player in poolablePlayers)
                    {
                        player.NameText.Color = _gameWinner.Color;
                    }
                    __instance.BackgroundBar.material.SetColor("_Color", _gameWinner.Color);
                    __instance.WinText.Color = _gameWinner.Color;
                    _gameWinner = null;
                    CustomRoles.Clear();
                }
            }
        }
    }
}