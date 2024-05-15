using System;
using System.Linq;
using HarmonyLib;
using Reactor;
using UnhollowerBaseLib;
using UnityEngine;

/*
using System.Linq;
using System.Text;
using Il2CppSystem.IO;
using Il2CppSystem.Collections.Generic;
using SpoklooPlugin.Roles;
using UnhollowerBaseLib;
using UnityEngine;
using Object = UnityEngine.Object;
*/

namespace SpoklooPlugin
{
    class GameOptionsPatches
    {
        /*
        public enum JesterSpawnChance
        {
            Never,
            Maybe,
            Always
        }
        public enum SheriffNumber
        {
            Zero,
            One,
            All
        }

        private static readonly string[] CustomOptions = {"JesterSpawnChance", "SheriffNb", "RandomSpawnLocations"};

        public static JesterSpawnChance JesterSc = JesterSpawnChance.Never;
        public static SheriffNumber SheriffNb = SheriffNumber.Zero;
        public static bool RandomSpawnLocations = false;


        [HarmonyPatch(typeof(ToggleOption), nameof(ToggleOption.OnEnable))]
        public static class OnEnablePatchToggle
        {
            public static bool Prefix(OptionBehaviour __instance)
            {
                return OnEnablePatchString.Prefix(__instance);
            }
        }


        [HarmonyPatch(typeof(StringOption), nameof(StringOption.OnEnable))]
        public static class OnEnablePatchString
        {
            public static bool Prefix(OptionBehaviour __instance)
            {
                var name = __instance.gameObject.name;
                return !name.EndsWith("(Clone)") || CustomOptions.Contains(name);
            }
        }
        
        
        [HarmonyPatch(typeof(StringOption), nameof(StringOption.FixedUpdate))]
        public static class FixedUpdatePatch
        {
            public static bool Prefix(StringOption __instance)
            {
                return !CustomOptions.Contains(__instance.gameObject.name);
            }
            
            public static void Postfix(StringOption __instance)
            {
                var name = __instance.gameObject.name;
                if (CustomOptions.Contains(name))
                {
                    if (__instance.oldValue != __instance.Value)
                    {
                        __instance.oldValue = __instance.Value;
                    }
                    switch (name)
                    {
                        case "JesterSpawnChance":
                            switch ((JesterSpawnChance) __instance.Value)
                            {
                                case JesterSpawnChance.Never:
                                    __instance.ValueText.Text = "0%";
                                    break;
                                case JesterSpawnChance.Maybe:
                                    __instance.ValueText.Text = "50%";
                                    break;
                                case JesterSpawnChance.Always:
                                    __instance.ValueText.Text = "100%";
                                    break;
                            }

                            break;
                        case "SheriffNb":
                            __instance.ValueText.Text = ((SheriffNumber) __instance.Value).ToString();
                            break;
                    }
                }
            }
        }


        [HarmonyPatch(typeof(ToggleOption), nameof(ToggleOption.Toggle))]
        public static class ToggleToggleOption
        {
            public static bool Prefix(ToggleOption __instance)
            {
                return !CustomOptions.Contains(__instance.gameObject.name);
            }

            public static void Postfix(ToggleOption __instance)
            {
                var name = __instance.gameObject.name;
                if (!CustomOptions.Contains(name)) return;
                __instance.CheckMark.enabled = !__instance.CheckMark.enabled;

                switch (name)
                {
                    case "RandomSpawnLocations":
                        RandomSpawnLocations = __instance.CheckMark.enabled;
                        break;
                }
                
                var localPlayer = PlayerControl.LocalPlayer;
                if (localPlayer == null)
                {
                    return;
                }
                localPlayer.RpcSyncSettings(PlayerControl.GameOptions);
            }
        }


        private static void StringPostfix(StringOption __instance, int increment)
        {
            var name = __instance.gameObject.name;
            if (!CustomOptions.Contains(name)) return;
            __instance.Value = Mathf.Clamp(__instance.Value + increment, 0, __instance.Values.Count - 1);
                    
            switch (name)
            {
                case "JesterSpawnChance":
                    JesterSc = (JesterSpawnChance) __instance.Value;
                    break;
                case "SheriffNb":
                    SheriffNb = (SheriffNumber) __instance.Value;
                    break;
            }

            var localPlayer = PlayerControl.LocalPlayer;
            if (localPlayer == null)
            {
                return;
            }
            localPlayer.RpcSyncSettings(PlayerControl.GameOptions);
        }
        
        
        [HarmonyPatch(typeof(StringOption), nameof(StringOption.Increase))]
        public static class IncreaseStringPatch
        {
            public static bool Prefix(StringOption __instance)
            {
                return !CustomOptions.Contains(__instance.gameObject.name);
            }
            
            public static void Postfix(StringOption __instance)
            {
                StringPostfix(__instance, 1);
            }
        }
        
        
        [HarmonyPatch(typeof(StringOption), nameof(StringOption.Decrease))]
        public static class DecreaseStringPatch
        {
            public static bool Prefix(StringOption __instance)
            {
                return !CustomOptions.Contains(__instance.gameObject.name);
            }
            
            public static void Postfix(StringOption __instance)
            {
                StringPostfix(__instance, -1);
            }
        }
        */
        
        [HarmonyPatch(typeof(GameOptionsMenu), nameof(GameOptionsMenu.Start))]
        [HarmonyPriority(Priority.First)]
        public static class GameOptionMenuPatch
        {
            /*
            private static void InitializeString(GameOptionsMenu __instance, string name, string title, int initialSelection, float y)
            {
                var cloneSource = Object.FindObjectOfType<StringOption>();
                var option = Object.Instantiate(cloneSource.gameObject, __instance.gameObject.transform);
                option.name = name;
                var localPosition = option.transform.localPosition;
                var strValue = option.GetComponent<StringOption>();
                strValue.TitleText.Text = title;
                strValue.Value = initialSelection;
                option.transform.localPosition = new Vector3(localPosition.x, y, localPosition.z);
            }
            
            private static void InitializeToggle(GameOptionsMenu __instance, string name, string title, bool initialChecked, float y)
            {
                var cloneSource = Object.FindObjectOfType<ToggleOption>();
                var option = Object.Instantiate(cloneSource.gameObject, __instance.gameObject.transform);
                option.name = name;
                var localPosition = option.transform.localPosition;
                var toggle = option.GetComponent<ToggleOption>();
                toggle.TitleText.Text = title;
                toggle.CheckMark.enabled = initialChecked;
                option.transform.localPosition = new Vector3(localPosition.x, y, localPosition.z);
            }
            
            
            public static void Prefix(GameOptionsMenu __instance)
            {
                Object.FindObjectOfType<ToggleOption>().transform.parent.parent.gameObject.GetComponent<Scroller>().YBounds = new FloatRange(0, 15.0f);
                
                InitializeString(__instance, "JesterSpawnChance", "Jester Spawn Chance", (int) JesterSc, -8.5f);
                InitializeString(__instance, "SheriffNb", "# Sheriffs", (int) SheriffNb, -9.0f);
                InitializeToggle(__instance, "RandomSpawnLocations", "Random Spawn Locations", RandomSpawnLocations, -9.5f);
            }
            */

            private static NumberOption InstantiateNumberChildren(GameOptionsMenu __instance, StringNames optionName)
            {
                var children = __instance.Children
                    .Single(o => o.Title == optionName)
                    .Cast<NumberOption>();
                return children;
            }
            
            public static void Postfix(GameOptionsMenu __instance)
            {
                // Patch the vision's valid range
                var crewVision = InstantiateNumberChildren(__instance, StringNames.GameCrewLight);
                crewVision.Increment = 0.1f;
                crewVision.ValidRange.min = 0.0f;
                
                var impVision = InstantiateNumberChildren(__instance, StringNames.GameImpostorLight);
                impVision.Increment = 0.1f;
                impVision.ValidRange.min = 0.0f;
            }
        }
        
        
        [HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.Update))]
        public static class MinPlayersPatch
        {
            public static void Prefix(GameStartManager __instance)
            {
                __instance.MinPlayers = 3;
            }
        }


        /*
        [HarmonyPatch(typeof(GameOptionsData), nameof(GameOptionsData.Method_53))]
        public static class SerializePatch
        {
            public static void Postfix(GameOptionsData __instance, [HarmonyArgument(0)] BinaryWriter writer)
            {
                writer.Write((byte) JesterSc);
                writer.Write((byte) SheriffNb);
                writer.Write((byte) (RandomSpawnLocations ? 1 : 0));
            }
        }
        
        
        [HarmonyPatch(typeof(GameOptionsData), nameof(GameOptionsData.Method_67))]
        public static class FromBytesPatch
        {
            public static void Postfix([HarmonyArgument(0)] Il2CppStructArray<byte> bytes)
            {
                JesterSc = (JesterSpawnChance) bytes[^3];
                SheriffNb = (SheriffNumber) bytes[^2];
                RandomSpawnLocations = bytes[^1] == 1;
            }
        }
        
        
        [HarmonyPatch(typeof(GameOptionsData), nameof(GameOptionsData.Method_24))]
        public static class ToHudStringPatch
        {
            public static void Postfix(ref string __result)
            {
                var stringBuilder = new StringBuilder(__result);
                switch (JesterSc.ToString())
                {
                    case "Never":
                        stringBuilder.AppendLine();
                        stringBuilder.AppendLine("Jester Spawn Chance: 0%");
                        break;
                    case "Maybe":
                        stringBuilder.AppendLine();
                        stringBuilder.AppendLine("Jester Spawn Chance: 50%");
                        break;
                    case "Always":
                        stringBuilder.AppendLine();
                        stringBuilder.AppendLine("Jester Spawn Chance: 100%");
                        break;
                }

                stringBuilder.AppendLine("# Sheriffs: " + SheriffNb);
                stringBuilder.AppendLine("Random Spawn Locations: " + (RandomSpawnLocations ? "On" : "Off"));
                __result = stringBuilder.ToString();
            }
        }*/


        [HarmonyPatch(typeof(GameSettingMenu), nameof(GameSettingMenu.OnEnable))]
        public static class OptionsHiddenForOnlinePatch
        {
            public static void Prefix(GameSettingMenu __instance)
            {
                __instance.HideForOnline = new Il2CppReferenceArray<Transform>(0);
            }
        }


        [HarmonyPatch(typeof(NumberOption), nameof(NumberOption.Increase))]
        public static class NumberOptionIncreasePatch
        {
            public static void Postfix(NumberOption __instance)
            {
                __instance.Value = (float)Math.Round(__instance.Value, 2);
            }
        }
        
        
        [HarmonyPatch(typeof(NumberOption), nameof(NumberOption.Decrease))]
        public static class NumberOptionDecreasePatch
        {
            public static void Postfix(NumberOption __instance)
            {
                NumberOptionIncreasePatch.Postfix(__instance);
            }
        }
        
        
        [HarmonyPatch(typeof(PingTracker), nameof(PingTracker.Update))]
        public static class PingTrackerPatch
        {
            public static void Postfix(PingTracker __instance)
            {
                if (AmongUsClient.Instance)
                {
                    __instance.text.Text += "\r\n\r\nMods made by \r\n[7488E5FF]Spokloo[]";
                }
            }
        }


        [HarmonyPatch(typeof(StatsManager), nameof(StatsManager.AmBanned), MethodType.Getter)]
        public static class RemoveBans
        {
            public static void Postfix(ref bool __result)
            {
                __result = false;
            }
        }


        [HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.Update))]
        public static class CopyLobbyCode
        {
            public static void Postfix()
            {
                if (AmongUsClient.Instance.GameMode != GameModes.OnlineGame) return;
                if (Input.GetKey(KeyCode.C) && Input.GetKey(KeyCode.LeftControl))
                {
                    GUIUtility.systemCopyBuffer = GameCode.IntToGameName(AmongUsClient.Instance.GameId);
                }
            }
        }
    }
}