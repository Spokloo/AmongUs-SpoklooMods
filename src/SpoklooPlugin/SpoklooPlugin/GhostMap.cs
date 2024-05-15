using System.Linq;
using HarmonyLib;
using Il2CppSystem.Collections.Generic;
using Reactor.Extensions;
using UnityEngine;

namespace SpoklooPlugin
{
    public class GhostMap
    {
        private static SpriteRenderer _mapIcon;
        private static GameObject _text;
        private static readonly Dictionary<GameData.PlayerInfo, SpriteRenderer> Icons = new Dictionary<GameData.PlayerInfo, SpriteRenderer>();
        private static readonly Dictionary<GameData.PlayerInfo, GameObject> TextObject = new Dictionary<GameData.PlayerInfo, GameObject>();
        private static readonly Dictionary<GameData.PlayerInfo, TextRenderer> Text = new Dictionary<GameData.PlayerInfo, TextRenderer>();
        
        [HarmonyPatch(typeof(MapBehaviour), nameof(MapBehaviour.ShowNormalMap))]
        public static class GhostMapColor
        {
            public static void Postfix(MapBehaviour __instance)
            {
                if (!PlayerControl.LocalPlayer.Data.IsDead) return;
                __instance.ColorControl.SetColor(new Color(0.5f, 0.5f, 0.5f, 1f));
            }
        }


        private static void GhostMapObjectsPos(MapBehaviour __instance, GameData.PlayerInfo player, bool showDead)
        {
            if (!Icons.ContainsKey(player))
            {
                _mapIcon = Object.Instantiate(__instance.HerePoint, __instance.HerePoint.transform.parent, true);
                Icons.Add(player, _mapIcon);
            }
            if (!Text.ContainsKey(player) && !TextObject.ContainsKey(player))
            {
                _text = new GameObject();
                _text.transform.SetParent(_mapIcon.gameObject.transform);
                var textComponent = _text.AddTextRenderer();
                TextObject.Add(player, _text);
                Text.Add(player, textComponent);
            }

            var position = player.Object.transform.position / ShipStatus.Instance.MapScale;
            position.x *= Mathf.Sign(ShipStatus.Instance.transform.localScale.x);
            position.z = -1f;
            Icons[player].transform.position = __instance.HerePoint.transform.position;
            player.Object.SetPlayerMaterialColors(Icons[player]);
            Icons[player].transform.localPosition = position;

            Text[player].Color = Palette.White;
            Text[player].Text = player.PlayerName;
            if (showDead && player.IsDead) Text[player].Text += " [FF1919FF][[Dead][]";
            Text[player].Centered = true;
            Text[player].gameObject.SetActive(true);
            Text[player].gameObject.layer = LayerMask.NameToLayer("UI");
            Text[player].transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            Text[player].transform.position = Icons[player].transform.position;
            Text[player].transform.localPosition = new Vector3(0f , -0.11f, 0f);
        }


        [HarmonyPatch(typeof(MapBehaviour), nameof(MapBehaviour.FixedUpdate))]
        public static class GhostMapSeePlayers
        {
            public static void Postfix(MapBehaviour __instance)
            {
                if (!PlayerControl.LocalPlayer.Data.IsDead) return;

                if (SpoklooPlugin.GhostMap.GetValue() == 0)
                {
                    foreach (var player in GameData.Instance.AllPlayers.ToArray().Where(p => !p.Disconnected))
                    {
                        GhostMapObjectsPos(__instance, player, true);
                    }
                }
                else if (SpoklooPlugin.GhostMap.GetValue() == 1)
                {
                    foreach (var player in GameData.Instance.AllPlayers.ToArray().Where(p => !p.Disconnected && !p.IsDead))
                    {
                        GhostMapObjectsPos(__instance, player, false);
                    }
                }

                foreach (var player in GameData.Instance.AllPlayers.ToArray().Where(p => p.Disconnected))
                {
                    if (Icons.ContainsKey(player))
                    {
                        Object.Destroy(Icons[player].gameObject);
                        Icons.Remove(player);
                    }

                    if (TextObject.ContainsKey(player))
                    {
                        Object.Destroy(TextObject[player].gameObject);
                        TextObject.Remove(player);
                    }
                    
                    if (Text.ContainsKey(player))
                    {
                        Object.Destroy(Text[player].gameObject);
                        Text.Remove(player);
                    }
                }
            }
        }


        [HarmonyPatch(typeof(MapBehaviour), nameof(MapBehaviour.Close))]
        public static class GhostDeleteMapIcons
        {
            public static void Postfix()
            {
                if (!PlayerControl.LocalPlayer.Data.IsDead) return;
                
                foreach (var player in GameData.Instance.AllPlayers.ToArray().Where(p => !p.Disconnected))
                {
                    if (Icons.ContainsKey(player))
                    {
                        Object.Destroy(Icons[player].gameObject);
                    }

                    if (TextObject.ContainsKey(player))
                    {
                        Object.Destroy(TextObject[player].gameObject);
                    }
                    
                    if (Text.ContainsKey(player))
                    {
                        Object.Destroy(Text[player].gameObject);
                    }
                }
                
                Icons.Clear();
                TextObject.Clear();
                Text.Clear();
            }
        }
    }
}