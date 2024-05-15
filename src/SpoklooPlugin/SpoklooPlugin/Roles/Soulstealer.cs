using System;
using System.Collections;
using System.Linq;
using HarmonyLib;
using Hazel;
using Reactor;
using SpoklooPlugin.Roles;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SpoklooPlugin
{
    public partial class SpoklooPlugin
    {
        private static byte _morphedPlayer = 255; 
        private static bool _isMorphed;
        private static float _morphStartTime;
        private static float _morphTime = PlayerControl.GameOptions.KillCooldown / 2f + 6f;
        private static readonly Color32 Transparent = new Color32(255, 255, 255, 0);
        private static readonly int Desat = Shader.PropertyToID("_Desat");
        private static readonly int BackColor = Shader.PropertyToID("_BackColor");
        private static readonly int BodyColor = Shader.PropertyToID("_BodyColor");
        private static readonly int Percent = Shader.PropertyToID("_Percent");

        private static void MorphPlayer(byte fromId, byte toId)
        {
            var from = GameData.Instance.GetPlayerById(fromId);
            var to = GameData.Instance.GetPlayerById(toId);
            if (from.Disconnected || to.Disconnected) return;
            
            System.Console.WriteLine($"Morphing from {from.PlayerName} to {to.PlayerName}");
            Coroutines.Start(CoMorphPlayer(from, to));
        }

        private static IEnumerator CoMorphPlayer(GameData.PlayerInfo from, GameData.PlayerInfo to)
        {
            if (from.PlayerId == PlayerControl.LocalPlayer.PlayerId)
            {
                _isMorphed = true;
                _morphStartTime = Time.time;
            }
            
            from.Object.nameText.Text = to.PlayerName;
            var player = from.Object;
            var hasHat = player.HatRenderer != null;
            
            var rend = player.myRend;
            var initialBackColor = rend.material.GetColor(BackColor);
            var initialBodyColor = rend.material.GetColor(BodyColor);
            PlayerControl.SetPlayerMaterialColors(Color.gray, rend);
            const int frames = 30;

            for (var i = 0; i < frames; i++)
            {
                var percent = (float) i / frames;
                rend.material.SetColor(BackColor, Color.Lerp(initialBackColor, Palette.ShadowColors[to.ColorId], percent));
                rend.material.SetColor(BodyColor, Color.Lerp(initialBodyColor, Palette.PlayerColors[to.ColorId], percent));

                var opacity = Mathf.Abs(percent * 2 - 1);
                if (Math.Abs(percent - 0.5) < 0.001f)   // percent == 0.5
                {
                    hasHat = to.HatId != 0;
                    player.HatRenderer.SetHat(to.HatId, to.ColorId);
                    player.MyPhysics.SetSkin(to.SkinId);

                    var oldPetId = from.PetId;
                    player.SetPet(to.PetId);
                    GameData.Instance.UpdatePet(from.PlayerId, oldPetId);
                    from.PetId = oldPetId;
                    PlayerControl.SetPlayerMaterialColors(to.ColorId, player.CurrentPet.rend);
                }
                else
                {
                    if (hasHat)
                    {
                        player.HatRenderer.color = Color.Lerp(Transparent, Color.white, opacity);
                    }
                    
                    
                    player.MyPhysics.Skin.layer.color = Color.Lerp(Transparent, Color.white, opacity);
                }
                
                yield return null;
            }
            
            PlayerControl.SetPlayerMaterialColors(to.ColorId, rend);
            _morphStartTime = Time.time;
            yield return new WaitForSeconds(_morphTime);
            
            if (!LobbyBehaviour.Instance)
            {
                initialBackColor = rend.material.GetColor(BackColor);
                initialBodyColor = rend.material.GetColor(BodyColor);
            
                for (var i = 0; i < frames; i++)
                {
                    var percent = (float) i / frames;
                    rend.material.SetColor(BackColor, Color.Lerp(initialBackColor, Palette.ShadowColors[from.ColorId], percent));
                    rend.material.SetColor(BodyColor, Color.Lerp(initialBodyColor, Palette.PlayerColors[from.ColorId], percent));

                    var opacity = Mathf.Abs(percent * 2 - 1);
                    if (Math.Abs(percent - 0.5) < 0.001f)   // percent == 0.5
                    {
                        hasHat = from.HatId != 0;
                        player.HatRenderer.SetHat(from.HatId, from.ColorId);
                        player.MyPhysics.SetSkin(from.SkinId);
                        player.SetPet(from.PetId);
                    }
                    else
                    {
                        if (hasHat)
                        {
                            player.HatRenderer.color = Color.Lerp(Transparent, Color.white, opacity);
                        }
                    
                    
                        player.MyPhysics.Skin.layer.color = Color.Lerp(Transparent, Color.white, opacity);
                    }
                
                    yield return null;
                }
                player.SetPlayerMaterialColors(rend);
                player.nameText.Text = from.PlayerName;
                if (from.PlayerId == PlayerControl.LocalPlayer.PlayerId)
                {
                    _isMorphed = false;
                    _morphCooldown = PlayerControl.GameOptions.KillCooldown * 1.5f;
                }
            }
        }

        private static void InitializeSoulstealer()
        {
            _morphedPlayer = 255;
                
            CustomRoles.TryGetValue(PlayerControl.LocalPlayer.PlayerId, out var roles);
            var isSoulstealer = roles != null && roles.Contains(Soulstealer.Instance);
                
            var killButton = DestroyableSingleton<HudManager>.Instance.KillButton;
            var gameObject = Object.Instantiate(killButton, killButton.transform.parent).gameObject;
            Object.Destroy(gameObject.GetComponent<KillButtonManager>());
            var morphButton = gameObject.AddComponent<MorphButtonManager>();
            gameObject.GetComponent<PassiveButton>().OnClick.AddListener(morphButton, morphButton.GetIl2CppType().GetMethod(nameof(MorphButtonManager.DoClick)));
            gameObject.gameObject.SetActive(isSoulstealer);
            var transform = gameObject.transform;
            var position = transform.localPosition;
            transform.localPosition = new Vector3(position.x, -1, position.z);
        }


        private static PlayerControl FindClosestTarget(PlayerControl __instance)
        {
            CustomRoles.TryGetValue(__instance.PlayerId, out var roles);
            if (roles == null || !roles.Contains(Soulstealer.Instance)) return null;
            
            PlayerControl result = null;
            var num = GameOptionsData.KillDistances[Mathf.Clamp(PlayerControl.GameOptions.KillDistance, 0, 2)];
            if (!ShipStatus.Instance)
            {
                return null;
            }

            var truePosition = __instance.GetTruePosition();
            var allBodies = Object.FindObjectsOfType<DeadBody>();
            foreach (var body in allBodies)
            {
                var vector = (Vector2) body.transform.position + body.myCollider.offset - truePosition;
                var magnitude = vector.magnitude;
                if (magnitude <= num && !PhysicsHelpers.AnyNonTriggersBetween(truePosition, vector.normalized, magnitude, Constants.ShipAndObjectsMask))
                {
                    result = GameData.Instance.GetPlayerById(body.ParentId).Object;
                    num = magnitude;
                }
            }

            return result;
        }


        [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Close))]
        public static class ClearMorphedPlayerMeeting
        {
            public static void Postfix()
            {
                _morphedPlayer = 255;
                _isMorphed = false;
                _morphCooldown = PlayerControl.GameOptions.KillCooldown * 1.5f + 10f;
            }
        }
        
        
        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.SetTasks))]
        public static class MorphCooldownReset
        {
            public static void Postfix()
            {
                _morphedPlayer = 255;
                _isMorphed = false;
                _morphCooldown = 16f;
            }
        }
        
        
        
        private static float _morphCooldown;
        private static float _timer = _morphTime;
        private static readonly int Outline = Shader.PropertyToID("_Outline");
        private static readonly int AddColor = Shader.PropertyToID("_AddColor");

        [RegisterInIl2Cpp]
        public class MorphButtonManager : MonoBehaviour
        {
            private SpriteRenderer _sprite;
            private TextRenderer _text;
            public static bool Interactable;
            public PlayerControl currentTarget;

            public MorphButtonManager(IntPtr ptr) : base(ptr)
            {
            }

            private void Start()
            {
                _sprite = GetComponent<SpriteRenderer>();
                _sprite.material.SetFloat(Percent, 0f);
                _text = GetComponentInChildren<TextRenderer>();
                SetTarget(null);
            }

            public static byte Target
            {
                get
                {
                    var target = FindClosestTarget(PlayerControl.LocalPlayer);
                    if (target is null) return 255;
                    return target.PlayerId;
                }
            }

            private void FixedUpdate()
            {
                if (_morphCooldown >= 0)
                {
                    _morphCooldown -= Time.deltaTime;
                }
                
                var target = Target;
                if (target != 255)
                {
                    SetActive(!_isMorphed && _morphCooldown < 0);
                }
                else if (target == 255)
                {
                    SetActive(false);
                }

                _sprite.sprite = PluginSingleton<SpoklooPlugin>.Instance._morphButton;
                if (_isMorphed)
                {
                    _timer = _morphTime - (Time.time - _morphStartTime);
                    
                    _text.Color = Palette.PlayerColors[11];
                    _text.Text = Mathf.CeilToInt(_timer).ToString();
                    _text.gameObject.SetActive(true);
                    _sprite.material.SetFloat(Percent, Mathf.Clamp(_timer / _morphTime, 0f, 1f));
                    CooldownHelpers.SetCooldownNormalizedUvs(_sprite);
                }
                else if (_morphCooldown < 0)
                {
                    _text.gameObject.SetActive(false);
                }
                else
                {
                    _text.Color = Palette.White;
                    _text.Text = Mathf.CeilToInt(_morphCooldown).ToString();
                    _text.gameObject.SetActive(true);
                    _sprite.material.SetFloat(Percent, Mathf.Clamp(_morphCooldown / (PlayerControl.GameOptions.KillCooldown * 1.5f), 0f, 1f));
                    CooldownHelpers.SetCooldownNormalizedUvs(_sprite);
                }
            }

            public void SetTarget(PlayerControl target)
            {
                CustomRoles.TryGetValue(PlayerControl.LocalPlayer.PlayerId, out var roles);
                if (roles != null && roles.Contains(Soulstealer.Instance))
                {
                    currentTarget = target;
                    foreach (var body in FindObjectsOfType<DeadBody>())
                    {
                        if (currentTarget is null)
                        {
                            body.GetComponent<SpriteRenderer>().material.SetFloat(Outline, 0f);
                            continue;
                        }

                        if (body.ParentId != currentTarget.PlayerId) continue;

                        var component = body.GetComponent<SpriteRenderer>();
                        component.material.SetFloat(Outline, 1f);
                        component.material.SetColor(OutlineColor, roles[0].Color);
                        component.material.SetColor(AddColor, roles[0].Color);
                        break;
                    }
                }
            }

            public void SetActive(bool isActive)
            {
                Interactable = isActive;
                
                var target = Target;
                if (target != 255)
                {
                    SetTarget(GameData.Instance.GetPlayerById(target).Object);
                }
                else
                {
                    SetTarget(null);
                }
                
                if (isActive)
                {
                    _sprite.color = Palette.EnabledColor;
                    _sprite.material.SetFloat(Desat, 0f);
                    return;
                }

                _sprite.color = Palette.DisabledColor;
                _sprite.material.SetFloat(Desat, 1f);
            }

            public void DoClick()
            {
                if (!Interactable) return;
                _morphedPlayer = Target;

                if (_morphedPlayer != 255)
                {
                    var morphTarget = GameData.Instance.GetPlayerById(_morphedPlayer);
                    if (morphTarget != null)
                    {
                        // Soulstealer morphs

                        var messageWriter = AmongUsClient.Instance.StartRpc(PlayerControl.LocalPlayer.NetId, (byte) CustomRpcMessage.DoMorph, SendOption.Reliable);
                        messageWriter.Write(PlayerControl.LocalPlayer.PlayerId);
                        messageWriter.Write(_morphedPlayer);
                        messageWriter.EndMessage();

                        MorphPlayer(PlayerControl.LocalPlayer.PlayerId, _morphedPlayer);
                    }
                }
            }
        }
        
        
        
        [HarmonyPatch(typeof(KeyboardJoystick), nameof(KeyboardJoystick.Method_97))]
        public static class SoulstealerPressMToMorph
        {
            public static void Postfix()
            {
                if (!Input.GetKeyDown(KeyCode.F)) return;
                if (!MorphButtonManager.Interactable) return;
                _morphedPlayer = MorphButtonManager.Target;

                if (_morphedPlayer != 255)
                {
                    var morphTarget = GameData.Instance.GetPlayerById(_morphedPlayer);
                    if (morphTarget != null)
                    {
                        // Soulstealer morphs

                        var messageWriter = AmongUsClient.Instance.StartRpc(PlayerControl.LocalPlayer.NetId, (byte) CustomRpcMessage.DoMorph, SendOption.Reliable);
                        messageWriter.Write(PlayerControl.LocalPlayer.PlayerId);
                        messageWriter.Write(_morphedPlayer);
                        messageWriter.EndMessage();

                        MorphPlayer(PlayerControl.LocalPlayer.PlayerId, _morphedPlayer);
                    }
                }
            }
        }
/*

        [HarmonyPatch(typeof(KillButtonManager), nameof(KillButtonManager.SetTarget))]
        public static class SoulstealerMorphOutline
        {
            public static void Postfix(KillButtonManager __instance)
            {
                CustomRoles.TryGetValue(PlayerControl.LocalPlayer.PlayerId, out var roles);
                if (roles != null && roles.Contains(Soulstealer.Instance))
                {
                    foreach (var body in Object.FindObjectsOfType<DeadBody>())
                    {
                        if (!__instance.CurrentTarget)
                        {
                            body.GetComponent<SpriteRenderer>().material.SetFloat(Outline, 0f);
                            continue;
                        }

                        if (body.ParentId != __instance.CurrentTarget.PlayerId) continue;

                        var component = body.GetComponent<SpriteRenderer>();
                        component.material.SetFloat(Outline, 1f);
                        component.material.SetColor(OutlineColor, roles[0].Color);
                        component.material.SetColor(AddColor, roles[0].Color);
                        break;
                    }
                }
            }
        }*/
    }
}