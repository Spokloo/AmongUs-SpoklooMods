using System;
using System.Linq;
using Assets.CoreScripts;
using HarmonyLib;
using InnerNet;
using Reactor;
using UnhollowerBaseLib.Attributes;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SpoklooPlugin
{
    internal static class CustomColors
    {
        private static bool _initialized = false;
        public const int RainbowColorId = 23;

        private struct ColorData
        {
            internal int ColorId;
            internal CustomStringName LongName;
            internal CustomStringName Name;
            internal Color32 MainColor;
            internal Color32 ShadowColor;
        }

        private static readonly ColorData[] CustomColorList =
        {
            new ColorData
            {
                ColorId = 12,
                LongName = CustomStringName.Register("Fortegreen"),
                Name = CustomStringName.Register("FGRN"),
                MainColor = new Color32(29, 152, 83, 255),
                ShadowColor = new Color32(18, 63, 27, 255)
            },
            new ColorData
            {
                ColorId = 13,
                LongName = CustomStringName.Register("Tan"),
                Name = CustomStringName.Register("TAN"),
                MainColor = new Color32(145, 137, 119, 255),
                ShadowColor = new Color32(81, 66, 62, 255)
            },
            new ColorData
            {
                ColorId = 14,
                LongName = CustomStringName.Register("Rose"),
                Name = CustomStringName.Register("ROSE"),
                MainColor = new Color32(238, 144, 255, 255),
                ShadowColor = new Color32(204, 77, 226, 255)
            },
            new ColorData
            {
                ColorId = 15,
                LongName = CustomStringName.Register("Magenta"),
                Name = CustomStringName.Register("MGTA"),
                MainColor = new Color32(255, 0, 143, 255),
                ShadowColor = new Color32(147, 20, 91, 255)
            },
            new ColorData
            {
                ColorId = 16,
                LongName = CustomStringName.Register("LightGreen"),
                Name = CustomStringName.Register("LGRN"),
                MainColor = new Color32(123, 202, 61, 255),
                ShadowColor = new Color32(78, 128, 38, 255)
            },
            new ColorData
            {
                ColorId = 17,
                LongName = CustomStringName.Register("Blurple"),
                Name = CustomStringName.Register("BLPL"),
                MainColor = new Color32(114, 137, 218, 255),
                ShadowColor = new Color32(78, 93, 148, 255)
            },
            new ColorData
            {
                ColorId = 18,
                LongName = CustomStringName.Register("PastelGreen"),
                Name = CustomStringName.Register("PGRN"),
                MainColor = new Color32(165, 250, 122, 255),
                ShadowColor = new Color32(118, 179, 87, 255)
            },
            new ColorData
            {
                ColorId = 19,
                LongName = CustomStringName.Register("PastelBlue"),
                Name = CustomStringName.Register("PBLU"),
                MainColor = new Color32(144, 211, 255, 255),
                ShadowColor = new Color32(91, 150, 189, 255)
            },
            new ColorData
            {
                ColorId = 20,
                LongName = CustomStringName.Register("PastelRed"),
                Name = CustomStringName.Register("PRED"),
                MainColor = new Color32(255, 120, 120, 255),
                ShadowColor = new Color32(209, 95, 95, 255)
            },
            new ColorData
            {
                ColorId = 21,
                LongName = CustomStringName.Register("PastelPurple"),
                Name = CustomStringName.Register("PPUR"),
                MainColor = new Color32(164, 91, 255, 255),
                ShadowColor = new Color32(114, 52, 190, 255)
            },
            new ColorData
            {
                ColorId = 22,
                LongName = CustomStringName.Register("PastelOrange"),
                Name = CustomStringName.Register("PORG"),
                MainColor = new Color32(255, 201, 120, 255),
                ShadowColor = new Color32(177, 135, 73, 255)
            },
            new ColorData
            {
                ColorId = RainbowColorId,
                LongName = CustomStringName.Register("Rainbow"),
                Name = CustomStringName.Register("RNBW"),
                MainColor = new Color32(38, 38, 38, 255),
                ShadowColor = new Color32(13, 13, 13, 255)
            }
        };
        
        private static void InitializeCustomColors()
        {
            Palette.PlayerColors = Palette.PlayerColors.Concat(CustomColorList.Select(playerColor => playerColor.MainColor)).ToArray();
            Palette.ShadowColors = Palette.ShadowColors.Concat(CustomColorList.Select(shadowColor => shadowColor.ShadowColor)).ToArray();
            Palette.ShortColorNames = Palette.ShortColorNames.Concat(CustomColorList.Select(name => (StringNames) name.Name)).ToArray();
            MedScanMinigame.ColorNames = MedScanMinigame.ColorNames.Concat(CustomColorList.Select(longName => (StringNames) longName.LongName)).ToArray();
            Telemetry.ColorNames = Telemetry.ColorNames.Concat(CustomColorList.Select(longName => (StringNames) longName.LongName)).ToArray();
        }

        public class RainbowUtils
        {
            private static readonly int BackColor = Shader.PropertyToID("_BackColor");
            private static readonly int BodyColor = Shader.PropertyToID("_BodyColor");
            private static readonly int VisorColor = Shader.PropertyToID("_VisorColor");

            private static Color Color => Color.HSVToRGB(_hue, 0.7f, 0.8f);

            private static Color ShadowColor => Color.HSVToRGB(_hue, 0.7f, 0.5f);
        
            public static void SetRainbow(Renderer rend)
            {
                rend.material.SetColor(BackColor, ShadowColor);
                rend.material.SetColor(BodyColor, Color);
                rend.material.SetColor(VisorColor, Palette.VisorColor);
            }

            public static bool IsRainbow(int id)
            {
                return id == RainbowColorId;
            }
        }

        
        private static float _hue;

        [RegisterInIl2Cpp]
        public class RainbowBehaviour : MonoBehaviour
        {
            public Renderer Renderer;
            public int Id;

            [HideFromIl2Cpp]
            public void AddRend(Renderer rend, int id)
            {
                Renderer = rend;
                Id = id;
            }

            public void Update()
            {
                if (Renderer != null && RainbowUtils.IsRainbow(Id)) RainbowUtils.SetRainbow(Renderer);
            }
            
            public RainbowBehaviour(IntPtr ptr) : base(ptr)
            {
            }
        }
        
        
        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.SetPlayerMaterialColors), typeof(int), typeof(Renderer))]
        public static class RainbowColor
        {
            public static bool Prefix([HarmonyArgument(0)] int colorId, [HarmonyArgument(1)] Renderer rend)
            {
                if (!rend.GetComponent<RainbowBehaviour>())
                {
                    rend.gameObject.AddComponent<RainbowBehaviour>().AddRend(rend, colorId);
                }
                rend.GetComponent<RainbowBehaviour>().Id = colorId;
                
                return !RainbowUtils.IsRainbow(colorId);
            }
        }

        
        [HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.FixedUpdate))]
        public static class HueShift
        {
            public static void Postfix()
            {
                _hue += 0.02f;
                _hue %= 1f;
            }
        }


        [HarmonyPatch(typeof(PlayerTab), nameof(PlayerTab.OnEnable))]
        public static class ColorChipsRearrangement
        {
            private const int NumPerRow = 4;
            private const float YOffset = 0.57f;    // 0.6f
            private const float YStart = -0.39f;    // -0.4f

            public static bool Prefix(PlayerTab __instance)
            {
                PlayerControl.SetPlayerMaterialColors(PlayerControl.LocalPlayer.Data.ColorId, __instance.DemoImage);
                __instance.HatImage.SetHat(SaveManager.LastHat, PlayerControl.LocalPlayer.Data.ColorId);
                PlayerControl.SetSkinImage(SaveManager.LastSkin, __instance.SkinImage);
                PlayerControl.SetPetImage(SaveManager.LastPet, PlayerControl.LocalPlayer.Data.ColorId, __instance.PetImage);
                
                /*
                var transforms = __instance.gameObject.GetComponentsInChildren<Transform>();
                Transform colorGroup = transforms.First(), background = transforms.Last();
                var scroller = colorGroup.gameObject.AddComponent<Scroller>();
                scroller.allowX = false;
                scroller.allowY = true;
                scroller.active = true;
                scroller.velocity = new Vector2(0, 0);
                scroller.ScrollerYRange = new FloatRange(0, 0);
                scroller.XBounds = new FloatRange(-10, 10);
                scroller.YBounds = new FloatRange(0, 10.2F);
                scroller.enabled = true;
                scroller.name = scroller.gameObject.name = "ColorScroller";

                scroller.Inner = Object.Instantiate(background, colorGroup);
                scroller.Inner.name = "ColorScrollerInner";
                scroller.Inner.GetComponent<SpriteRenderer>().color = new Color(0, 0, 0, 0);
                scroller.transform.SetParent(background.parent);*/
                

                for (var i = 0; i < Palette.ShortColorNames.Length; i++)
                {
                    // 1.45f, 3.275f
                    var x = Mathf.Lerp(1.46f, 3.265f, (i % NumPerRow) / (NumPerRow - 1f));
                    var y = YStart - (i / NumPerRow) * YOffset;
                    
                    var colorChip = Object.Instantiate(__instance.ColorTabPrefab/*, scroller.Inner*/);
                    colorChip.transform.SetParent(__instance.transform);
                    colorChip.transform.localPosition = new Vector3(x, y, -1f);
                    
                    var j = i;

                    colorChip.Button.OnClick.AddListener((Action) delegate {
                        __instance.Method_146(j);
                    });
                    
                    // 1, 9, 15, 29, 33, 34, 41, 65, 73, 80, 82, 83, 88, 93, 96, 100, 107, 113, 115, 146

                    colorChip.Inner.color = Palette.PlayerColors[i];
                    __instance.ColorChips.Add(colorChip);
                }

                return false;
            }
        }


        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CheckColor))]
        public static class TestPatch
        {
            public static void Prefix(PlayerControl __instance, [HarmonyArgument(0)] byte bodyColor)
            {
                System.Console.WriteLine("body color : " + bodyColor);
                System.Console.WriteLine("body color instance : " + __instance.Data.ColorId);
                System.Console.WriteLine("colors length : " + Palette.PlayerColors.Length);
            }
        }

        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.SetColor))]
        public static class TestPatch2
        {
            public static void Postfix(PlayerControl __instance, [HarmonyArgument(0)] byte bodyColor)
            {
                System.Console.WriteLine("set color : " + bodyColor);
            }
        }


        [HarmonyPatch(typeof(PlayerTab), nameof(PlayerTab.Update))]
        public static class RainbowColorChip
        {
            public static void Postfix(PlayerTab __instance)
            {
                for (var i = 0; i < Palette.ShortColorNames.Length; i++)
                {
                    if (CustomColorList.FirstOrDefault(colorId => colorId.ColorId == i).ColorId == RainbowColorId)
                    {
                        __instance.ColorChips[i].Inner.color = Color.HSVToRGB(_hue, 0.7f, 0.8f);
                    }
                }
            }
        }


        [HarmonyPatch(typeof(VersionShower), nameof(VersionShower.Start))]
        public static class VersionShowerPatch
        {
            public static void Postfix()
            {
                Reactor.Patches.ReactorVersionShower.Text.Text = "\r\n[7488E5FF]Spokloo Mods v" + SpoklooPlugin.SubVersion + "[]";
                
                if (_initialized) return;
                _initialized = true;
                InitializeCustomColors();
            }
        }
    }
}