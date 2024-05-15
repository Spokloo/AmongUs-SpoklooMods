using System;
using BepInEx;
using BepInEx.IL2CPP;
using HarmonyLib;
using Reactor;
using Essentials.Options;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Hazel;
using Reactor.Extensions;
using SpoklooPlugin.Roles;
using UnhollowerBaseLib;
using UnityEngine;
using Random = System.Random;

namespace SpoklooPlugin
{
    [BepInPlugin(Id, Name, Version)]
    [BepInProcess("Among Us.exe")]
    [BepInDependency(ReactorPlugin.Id)]
    [ReactorPluginSide(PluginSide.ClientOnly)]
    
    public partial class SpoklooPlugin : BasePlugin
    {
        private Sprite _morphButton;
    
        private const string Id = "gg.reactor.spokloomods";
        private const string Name = "Spokloo Mods";
        private const string Version = "1.1.0";
        public const string SubVersion = Version + "a";

        public static readonly Dictionary<byte, CustomRole[]> CustomRoles = new Dictionary<byte, CustomRole[]>();
        private static readonly Dictionary<string, int> CrewmateStreaks = new Dictionary<string, int>();
        private static readonly Random Rng = new Random(); 
        
        private static CustomRole _gameWinner = null;

        private static readonly CustomOptionHeader Space1 = new CustomOptionHeader(" ");
        private static readonly CustomOptionHeader ModSettings = new CustomOptionHeader("[CA5555FF]------ Mod Settings ------[]", true, false);
        private static readonly CustomOptionHeader CrewmateRoles = new CustomOptionHeader("Crewmate Roles:");
        private static readonly CustomStringOption SheriffOption = CustomOption.AddString("SheriffOption", "[FBC818FF]Sheriff", "Zero", "One", "Two", "All");
        private static readonly CustomOptionHeader Space2 = new CustomOptionHeader(" ");
        private static readonly CustomOptionHeader NeutralRoles = new CustomOptionHeader("Neutral Roles:");
        private static readonly CustomStringOption JesterOption = CustomOption.AddString("JesterOption", "[FB8CFFFF]Jester" ,"0%", "10%", "20%", "30%", "40%", "50%", "60%", "70%", "80%", "90%", "100%");
        private static readonly CustomStringOption LoversOption = CustomOption.AddString("LoversOption", "[F22FC6FF]Lovers" ,"0%", "10%", "20%", "30%", "40%", "50%", "60%", "70%", "80%", "90%", "100%");
        private static readonly CustomStringOption ImpLoversOption = CustomOption.AddString("ImpLoversOption", "[F22FC6FF]Impostor Lover" ,"0%", "10%", "20%", "30%", "40%", "50%", "60%", "70%", "80%", "90%", "100%");
        private static readonly CustomOptionHeader Space3 = new CustomOptionHeader(" ");
        private static readonly CustomOptionHeader ImpostorRoles = new CustomOptionHeader("Impostor Roles:");
        private static readonly CustomStringOption SoulstealerOption = CustomOption.AddString("SoulstealerOption", "[FF1919FF]Soulstealer", "0%", "10%", "20%", "30%", "40%", "50%", "60%", "70%", "80%", "90%", "100%");
        private static readonly CustomOptionHeader Space4 = new CustomOptionHeader(" ");
        private static readonly CustomOptionHeader Miscellaneous = new CustomOptionHeader("Miscellaneous:");
        public static readonly CustomToggleOption RandomSpawnLocations = CustomOption.AddToggle("RandomSpawnLocations", "Random Spawn Locations", false);
        public static readonly CustomToggleOption UnknownImpostors = CustomOption.AddToggle("UnknownImpostors", "Unknown Impostors", false);
        public static readonly CustomStringOption GhostMap = CustomOption.AddString("GhostMap", "Ghost Map", "Ghosts + Alive", "Alive");

        private Harmony Harmony { get; } = new Harmony(Id);

        
        // --------- Custom Server Setup --------- //      
        
        private static readonly IRegionInfo[] CustomRegions = {
            new DnsRegionInfo("62.171.162.221", "Spokloo", StringNames.NoTranslation, "62.171.162.221")
                .Duplicate()
        };

        private static void Patch()
        {
            var patchedRegions = MergeRegions(ServerManager.DefaultRegions, CustomRegions);

            ServerManager.DefaultRegions = patchedRegions;
            ServerManager.Instance.AvailableRegions = patchedRegions;
            ServerManager.Instance.SaveServers();
        }

        private static IRegionInfo[] MergeRegions(IRegionInfo[] oldRegions, IRegionInfo[] newRegions)
        {
            var patchedRegions = new IRegionInfo[oldRegions.Length + newRegions.Length];
            Array.Copy(oldRegions, patchedRegions, oldRegions.Length);
            Array.Copy(newRegions, 0, patchedRegions, oldRegions.Length, newRegions.Length);

            return patchedRegions;
        }
        
        
        // --------- Load Images --------- //

        private delegate bool DLoadImage(IntPtr tex, IntPtr data, bool markNonReadable);
        private static DLoadImage _iCallLoadImage;

        public static bool LoadImage(Texture2D tex, byte[] data, bool markNonReadable)
        {
            if (_iCallLoadImage == null)
                _iCallLoadImage = IL2CPP.ResolveICall<DLoadImage>("UnityEngine.ImageConversion::LoadImage");

            var il2CPPArray = (Il2CppStructArray<byte>) data;

            return _iCallLoadImage.Invoke(tex.Pointer, il2CPPArray.Pointer, markNonReadable);
        }
        
        private static Sprite LoadImage(Assembly assembly, string name)
        {
            var tex = GUIExtensions.CreateEmptyTexture();
            var imageStream = assembly.GetManifestResourceStream(name);
            var img = imageStream.ReadFully();
            LoadImage(tex, img, false);
            tex.DontDestroy();
            var sprite = tex.CreateSprite();
            sprite.DontDestroy();
            return sprite;
        }
        
        
        public override void Load()
        {
            var assembly = Assembly.GetExecutingAssembly();
            
            // Load Images
            _morphButton = LoadImage(assembly, "SpoklooPlugin.Ressources.morph.png");
            
            // Needed for MonoBehaviour (MorphButton, Reactor Debug Patches)
            RegisterInIl2CppAttribute.Register();
            
            // MinPlayers & MaxImpostors Patch
            GameOptionsData.MaxImpostors = GameOptionsData.RecommendedImpostors = new[]
            {
                0,
                0,
                0,
                1,
                1,
                1,
                1,
                2,
                2,
                3,
                3
            };
            GameOptionsData.MinPlayers = new []
            {
                3,
                3,
                7,
                9
            };
             
            // Patch Custom Server
            Patch();
            
            // Harmony Patch
            Harmony.PatchAll();
        }
        
        
        // --------- Custom Roles Handler --------- //

        private static int ChoosePlayerToGiveRole(List<GameData.PlayerInfo> players)
        {
            System.Console.WriteLine($"Choosing player between {players.Count} player(s)");
            var baseChance = 1.0 / players.Count;
            var total = 0.0;
            var chances = new List<double>();

            foreach (var player in players)
            {
                var streak = CrewmateStreaks.ContainsKey(player.PlayerName) ? CrewmateStreaks[player.PlayerName] : 0;
                
                var playerChance = baseChance * Math.Pow(1.2f, streak);
                chances.Add(playerChance);
                System.Console.WriteLine($"{player.PlayerName}: streak={streak}, chance={playerChance}");
                total += playerChance;
            }

            var x = Rng.NextDouble() * total;
            System.Console.WriteLine($"X={x}");
            total = 0.0;
            for (var i = 0; i < chances.Count; i++)
            {
                var chance = chances[i];
                total += chance;
                if (x < total)
                {
                    System.Console.WriteLine($"Result = {i}");
                    return i;
                }
            }

            return players.Count - 1;
        }
        

        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcSetInfected))]
        public static class RpcSetCustomRoles
        {
            private class RoleToGive
            {
                public CustomRole Role;
                public bool IsImpRole;
            }
            
            public static void Postfix(PlayerControl __instance, [HarmonyArgument(0)] Il2CppReferenceArray<GameData.PlayerInfo> infected)
            {
                if (AmongUsClient.Instance.GameMode == GameModes.FreePlay) return;

                var playersToGiveRoles = new List<GameData.PlayerInfo>();
                var impostorsToGiveRoles = new List<GameData.PlayerInfo>();

                foreach (var p in GameData.Instance.AllPlayers)
                {
                    if (infected.All(inf => inf.PlayerId != p.PlayerId))
                    {
                        playersToGiveRoles.Add(p);
                    }
                    else
                    {
                        impostorsToGiveRoles.Add(p);
                    }
                }

                
                // Roles spawning with custom game settings
                var rolesToAssign = new List<RoleToGive>();

                // Jester
                if (playersToGiveRoles.Count > 0 && HashRandom.Method_1(100) + 1 <= JesterOption.GetValue() * 10)
                {
                    rolesToAssign.Add(new RoleToGive { Role = Jester.Instance });
                }
                
                // Lovers
                if (HashRandom.Method_1(100) + 1 <= LoversOption.GetValue() * 10)
                {
                    if (impostorsToGiveRoles.Count > 0 && playersToGiveRoles.Count > 0 && HashRandom.Method_1(100) + 1 <= ImpLoversOption.GetValue() * 10)
                    {
                        rolesToAssign.Add(new RoleToGive { Role = Lover1.Instance });
                        rolesToAssign.Add(new RoleToGive { Role = Lover2.Instance, IsImpRole = true });
                    } else if (playersToGiveRoles.Count >= 2)
                    {
                        rolesToAssign.Add(new RoleToGive { Role = Lover1.Instance });
                        rolesToAssign.Add(new RoleToGive { Role = Lover2.Instance });
                    }
                }

                // Sheriff
                switch (SheriffOption.GetValue())
                {
                    case 0:
                        break;
                    case 1:
                        if (playersToGiveRoles.Count > 0) rolesToAssign.Add(new RoleToGive { Role = Sheriff.Instance });
                        break;
                    case 2:
                        if (playersToGiveRoles.Count >= 2)
                        {
                            rolesToAssign.Add(new RoleToGive {Role = Sheriff.Instance});
                            rolesToAssign.Add(new RoleToGive {Role = Sheriff.Instance});
                        }
                        break;
                    case 3:
                        while (rolesToAssign.Count < playersToGiveRoles.Count)
                        {
                            rolesToAssign.Add(new RoleToGive { Role = Sheriff.Instance });
                        }
                        break;
                }

                // Soulstealer
                if (impostorsToGiveRoles.Count > 0 && HashRandom.Method_1(100) + 1 <= SoulstealerOption.GetValue() * 10)
                {
                    rolesToAssign.Add(new RoleToGive { Role = Soulstealer.Instance, IsImpRole = true });
                }

                
                var messageWriter = AmongUsClient.Instance.StartRpc(__instance.NetId, (byte)CustomRpcMessage.SetCustomRoles, SendOption.Reliable);
                messageWriter.Write((byte) rolesToAssign.Count);
                
                CustomRoles.Clear();
                foreach (var role in rolesToAssign)
                {
                    var playersToGiveThisRole = role.IsImpRole ? impostorsToGiveRoles : playersToGiveRoles;

                    var index = ChoosePlayerToGiveRole(playersToGiveThisRole);
                    CustomRoles.Add(playersToGiveThisRole[index].PlayerId, new[] { role.Role });
                    messageWriter.Write(playersToGiveThisRole[index].PlayerId);
                    messageWriter.WriteBytesAndSize(new[] { role.Role.RoleId });
                    CrewmateStreaks[playersToGiveThisRole[index].PlayerName] = 0;
                    playersToGiveThisRole.RemoveAt(index);
                }

                foreach (var player in playersToGiveRoles)
                {
                    if (CrewmateStreaks.ContainsKey(player.PlayerName))
                    {
                        CrewmateStreaks[player.PlayerName]++;
                    }
                    else
                    {
                        CrewmateStreaks[player.PlayerName] = 1;
                    }
                }

                messageWriter.EndMessage();
                InitializeSoulstealer();
            }
        }


        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.HandleRpc))]
        public static class HandleRpcPatch
        {
            public static void Prefix([HarmonyArgument(1)] MessageReader writer, [HarmonyArgument(0)] int callId)
            {
                switch ((CustomRpcMessage)callId)
                {
                    case CustomRpcMessage.SetCustomRoles: // Set Custom Roles
                        CustomRoles.Clear();
                        var length = writer.ReadByte();
                        for (var i = 0; i < length; i++)
                        {
                            var playerId = writer.ReadByte();
                            var player = GameData.Instance.GetPlayerById(playerId).Object;
                            var rolesIds = writer.ReadBytesAndSize();
                            var roles = new CustomRole[rolesIds.Length];
                            for (var j = 0; j < rolesIds.Length; j++)
                            {
                                roles[j] = CustomRole.Roles[rolesIds[j]];
                            }
                            CustomRoles.Add(player.PlayerId, roles);
                        }
                        InitializeSoulstealer();
                        
                        return;
                    case CustomRpcMessage.DoMorph:
                        MorphPlayer(writer.ReadByte(), writer.ReadByte());
                        return;
                    default:
                        return;
                }
            }
        }


        [HarmonyPatch(typeof(IntroCutscene.CoBegin__d), nameof(IntroCutscene.CoBegin__d.MoveNext))]
        public static class IntroCutScenePatch
        {
            public static void Prefix(IntroCutscene.CoBegin__d __instance)
            {
                if (CustomRoles.ContainsKey(PlayerControl.LocalPlayer.PlayerId))
                {
                    var role = CustomRoles[PlayerControl.LocalPlayer.PlayerId];
                    if (!role[0].CrewmatesTeam)
                    {
                        __instance.yourTeam = new Il2CppSystem.Collections.Generic.List<PlayerControl>();
                        __instance.yourTeam.Add(PlayerControl.LocalPlayer);
                    }
                }
            }

            public static void Postfix(IntroCutscene.CoBegin__d __instance)
            {
                if (PlayerControl.LocalPlayer.Data.IsImpostor)
                {
                    __instance.__this.ImpostorText.gameObject.SetActive(true);
                    __instance.__this.ImpostorText.Text = "Sabotage and kill everyone";
                }
                
                if (CustomRoles.ContainsKey(PlayerControl.LocalPlayer.PlayerId))
                {
                    var role = CustomRoles[PlayerControl.LocalPlayer.PlayerId];
                    if (role[0].ImpostorText != null)
                    {
                        __instance.__this.ImpostorText.Text = role[0].ImpostorText;

                        if (role[0] == Lover1.Instance || role[0] == Lover2.Instance)
                        {
                            var otherLover = PlayerControl.LocalPlayer == FirstLover ? SecondLover : FirstLover;
                            __instance.__this.ImpostorText.Text = role[0].ImpostorText + $"{otherLover.nameText.Text}[]";
                        }
                    }

                    __instance.__this.BackgroundBar.material.SetColor("_Color", role[0].Color);
                    __instance.__this.Title.Text = role[0].Name;
                    __instance.__this.Title.Color = role[0].Color;
                    PlayerControl.LocalPlayer.nameText.Color = role[0].Color;
                    
                    if ((role[0] == Lover1.Instance || role[0] == Lover2.Instance) && PlayerControl.LocalPlayer.Data.IsImpostor)
                    {
                        __instance.__this.Title.Text = "Impostor " + role[0].Name;
                    }
                }
            }
        }


        [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Method_129))]
        public static class NameColorInMeetingPatch
        {
            public static void Postfix([HarmonyArgument(0)] GameData.PlayerInfo player, ref PlayerVoteArea __result)
            {
                if (!player.Disconnected && player.Object.AmOwner && CustomRoles.ContainsKey(player.PlayerId))
                {
                    var role = CustomRoles[player.PlayerId];
                    __result.NameText.Color = role[0].Color;
                }
            }
        }


        private static void TextTask(PlayerControl __instance, string textColor, string roleName, string roleDesc, bool fakeTask=false, bool removeDefaultText=false)
        {
            if (removeDefaultText)
            {
                __instance.myTasks.RemoveAt(0);
            }
            
            var importantTextTask = new GameObject("_Player").AddComponent<ImportantTextTask>();
            importantTextTask.transform.SetParent(PlayerControl.LocalPlayer.transform, false);
            importantTextTask.Text = $"[{textColor}]Role: {roleName}[]\r\n[{textColor}]{roleDesc}[]";
            if (fakeTask)
            {
                importantTextTask.Text += "\r\n[FFFFFFFF]Fake Tasks:";
            }
            
            __instance.myTasks.Insert(0, importantTextTask);
        }
        
        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.SetTasks))]
        public static class RoleText
        {
            public static void Postfix(PlayerControl __instance)
            {
                if (__instance.AmOwner)
                {
                    CustomRoles.TryGetValue(__instance.PlayerId, out var roles);
                    if (roles != null)
                    {
                        if (roles.Contains(Jester.Instance))
                        {
                            TextTask(__instance, "FB8CFFFF", roles[0].Name, roles[0].ImpostorText, true);
                        }
                        else if (roles.Contains(Sheriff.Instance))
                        {
                            TextTask(__instance, "FBC818FF", roles[0].Name, roles[0].RoleText);
                        }
                        else if (roles.Contains(Soulstealer.Instance))
                        {
                            TextTask(__instance, "FF1919FF", roles[0].Name, roles[0].ImpostorText, true, true);
                        }
                        else if ((roles.Contains(Lover1.Instance) || roles.Contains(Lover2.Instance)) && !__instance.Data.IsImpostor)
                        {
                            var otherLover = __instance == FirstLover ? SecondLover : FirstLover;
                            TextTask(__instance, "F22FC6FF", roles[0].Name, roles[0].RoleText + $"{otherLover.nameText.Text}[]");
                        }
                        else if ((roles.Contains(Lover1.Instance) || roles.Contains(Lover2.Instance)) && __instance.Data.IsImpostor)
                        {
                            var otherLover = __instance == FirstLover ? SecondLover : FirstLover;
                            TextTask(__instance, "F22FC6FF", "Impostor " + roles[0].Name, roles[0].RoleText + $"{otherLover.nameText.Text}[]", true, true);
                        }
                    }
                    else switch (__instance.Data.IsImpostor)
                    {
                        case true:
                            TextTask(__instance, "FF1919FF", "Impostor", "Sabotage and kill everyone", true, true);
                            break;
                        case false:
                            TextTask(__instance, "8CFFFFFF", "Crewmate", "Find the Impostor(s) and survive");
                            break;
                    }
                }
            }
        }


        [HarmonyPatch(typeof(ExileController), nameof(ExileController.Begin))]
        public static class ExileTextPatch
        {
            public static void Postfix(ExileController __instance, [HarmonyArgument(0)] GameData.PlayerInfo exiled)
            {
                if (exiled != null && PlayerControl.GameOptions.ConfirmImpostor && CustomRoles.ContainsKey(exiled.PlayerId))
                {
                    var role = CustomRoles[exiled.PlayerId];
                    __instance.completeString = exiled.PlayerName + " was the " + role[0].Name + ".";
                }
            }
        }


        [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.CheckEndCriteria))]
        public static class CheckEndCriteriaPatch
        {
            public static void Prefix(out List<int> __state)
            {
                __state = new List<int>();
                if (_gameWinner != null)
                {
                    for (var i = 0; i < GameData.Instance.PlayerCount; i++)
                    {
                        var playerInfo = GameData.Instance.AllPlayers[i];
                        if (!playerInfo.IsImpostor)
                        {
                            __state.Add(i);
                            playerInfo.IsImpostor = true;
                        }
                    }

                    TempData.LastDeathReason = DeathReason.Exile;
                }
            }

            public static void Postfix(List<int> __state)
            {
                foreach (var i in __state)
                {
                    var playerInfo = GameData.Instance.AllPlayers[i];
                    playerInfo.IsImpostor = false;
                }
            }
        }
        
        
        [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.IsGameOverDueToDeath))]
        public static class GameOverDueToDeathPatch
        {
            public static void Prefix(out List<int> __state)
            {
                __state = new List<int>();
                if (_gameWinner != null)
                {
                    for (var i = 0; i < GameData.Instance.PlayerCount; i++)
                    {
                        var playerInfo = GameData.Instance.AllPlayers[i];
                        if (!playerInfo.IsImpostor)
                        {
                            __state.Add(i);
                            playerInfo.IsImpostor = true;
                        }
                    }

                    TempData.LastDeathReason = DeathReason.Exile;
                }
            }

            public static void Postfix(List<int> __state)
            {
                foreach (var i in __state)
                {
                    var playerInfo = GameData.Instance.AllPlayers[i];
                    playerInfo.IsImpostor = false;
                }
            }
        }
        
        
        [HarmonyPatch(typeof(KeyboardJoystick), nameof(KeyboardJoystick.Update))]
        public static class ZqsdMovement
        {
            public static bool Prefix(KeyboardJoystick __instance)
            {
                if (!PlayerControl.LocalPlayer) return false;

                var del = Vector2.zero;
                if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D))
                    del.x += 1f;
                
                if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.Q))
                    del.x -= 1f;
                
                if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.Z))
                    del.y += 1f;
                
                if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S))
                    del.y -= 1f;
                
                
                del.Normalize();
                __instance.del = del;
                
                KeyboardJoystick.Method_97();
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    if (Minigame.Instance)
                        Minigame.Instance.Close();
                    
                    else if (DestroyableSingleton<HudManager>.InstanceExists && MapBehaviour.Instance && MapBehaviour.Instance.IsOpen)
                        MapBehaviour.Instance.Close();
                    
                    else if (CustomPlayerMenu.Instance)
                        CustomPlayerMenu.Instance.Close(true);
                    
                }
                return false;
            }
        }


        [HarmonyPatch(typeof(KeyboardJoystick), nameof(KeyboardJoystick.Method_97))]
        public static class ImpostorAToKill
        {
            public static bool Prefix()
            {
                if (!DestroyableSingleton<HudManager>.InstanceExists)
                {
                    return false;
                }
                if (Input.GetKeyDown(KeyCode.R))
                {
                    DestroyableSingleton<HudManager>.Instance.ReportButton.DoClick();
                }
                if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.E))
                {
                    DestroyableSingleton<HudManager>.Instance.UseButton.DoClick();
                }
                if (Input.GetKeyDown(KeyCode.Tab))
                {
                    DestroyableSingleton<HudManager>.Instance.ShowMap((Action<MapBehaviour>) delegate(MapBehaviour m)
                    {
                        m.ShowNormalMap();
                    });
                }
                if (PlayerControl.LocalPlayer.Data != null && PlayerControl.LocalPlayer.Data.IsImpostor && Input.GetKeyDown(KeyCode.A))
                {
                    DestroyableSingleton<HudManager>.Instance.KillButton.PerformKill();
                }

                return false;
            }
        }
    }
}
