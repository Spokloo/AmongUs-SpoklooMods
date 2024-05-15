using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace SpoklooPlugin
{
    public class RandomSpawnLocationsPatches
    {
        [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Close))]
        public static class RandomSpawnLocations
        {
            private static Vector2[] _skeldSpawns =
            {
                new Vector2(-4.5f, 2.8f),
                new Vector2(2.4f, 5.3f),
                new Vector2(-2.0f, -2.3f),
                new Vector2(4.6f, -7.1f),
                new Vector2(2.2f, -14.6f),
                new Vector2(5.6f, -16.2f),
                new Vector2(18.3f, -5.6f),
                new Vector2(6.6f, -4.7f),
                new Vector2(10.5f, 2.2f),
                new Vector2(-10.5f, -2.1f),
                new Vector2(-7.3f, -4.0f),
                new Vector2(-16.2f, 2.5f),
                new Vector2(-13.1f, -5.5f),
                new Vector2(-22.3f, -7.3f),
                new Vector2(-18.6f, -9.6f),
                new Vector2(-7.1f, -9.7f),
                new Vector2(0.3f, -9.3f),
                new Vector2(-4.1f, -10.4f),
                new Vector2(-1.8f, -16.8f)
            };
            
            private static Vector2[] _hqSpawns =
            {
                new Vector2(21.0f, 17.7f),
                new Vector2(13.5f, 17.2f),
                new Vector2(15.9f, 20.6f),
                new Vector2(20.7f, 23.1f),
                new Vector2(16.5f, 3.4f),
                new Vector2(15.0f, 1.7f),
                new Vector2(10.0f, 5.0f),
                new Vector2(10.9f, 12.3f),
                new Vector2(0.6f, 11.2f),
                new Vector2(2.5f, 11.9f),
                new Vector2(10.8f, 14.2f),
                new Vector2(10.6f, 0.8f),
                new Vector2(-2.9f, 1.7f),
                new Vector2(14.4f, 5.1f),
                new Vector2(13.5f, 22.9f),
                new Vector2(24.1f, 1.0f),
                new Vector2(25.6f, -2.3f),
                new Vector2(28.6f, 5.3f),
                new Vector2(20.9f, 4.8f),
                new Vector2(9.6f, -0.6f)
            };
            
            private static Vector2[] _polusSpawns =
            {
                new Vector2(19.6f, -16.3f),
                new Vector2(24.1f, -17.0f),
                new Vector2(24.9f, -20.7f),
                new Vector2(22.1f, -25.1f),
                new Vector2(35.3f, -20.8f),
                new Vector2(34.9f, -9.3f),
                new Vector2(38.4f, -7.5f),
                new Vector2(29.6f, -8.1f),
                new Vector2(25.4f, -6.9f),
                new Vector2(19.2f, -10.7f),
                new Vector2(18.6f, -0.8f),
                new Vector2(1.0f, -17.3f),
                new Vector2(2.3f, -24.5f),
                new Vector2(11.2f, -16.9f),
                new Vector2(13.1f, -24.4f),
                new Vector2(17.3f, -25.7f),
                new Vector2(32.7f, -15.6f),
                new Vector2(4.8f, -20.9f),
                new Vector2(11.9f, -6.8f),
                new Vector2(25.8f, -12.9f)
            };
            
                
            public static void Postfix()
            {
                if (!AmongUsClient.Instance.AmHost || !SpoklooPlugin.RandomSpawnLocations.GetValue()) return;

                var potentialSpawnLocations = new List<Vector2>();
                
                switch (ShipStatus.Instance.Type)
                {
                    case ShipStatus.MapType.Ship:
                        potentialSpawnLocations.AddRange(_skeldSpawns);
                        break;
                    case ShipStatus.MapType.Hq:
                        potentialSpawnLocations.AddRange(_hqSpawns);
                        break;
                    case ShipStatus.MapType.Pb:
                        potentialSpawnLocations.AddRange(_polusSpawns);
                        break;
                }

                for (int i = 0; i < potentialSpawnLocations.Count - 1; i++)
                {
                    var value = potentialSpawnLocations[i];
                    int index = UnityEngine.Random.Range(i, potentialSpawnLocations.Count);
                    potentialSpawnLocations[i] = potentialSpawnLocations[index];
                    potentialSpawnLocations[index] = value;
                }

                for (int j = 0; j < PlayerControl.AllPlayerControls.Count; j++)
                {
                    PlayerControl playerControl = PlayerControl.AllPlayerControls[j];
                    
                    if (!playerControl.Data.IsDead)
                    {
                        playerControl.NetTransform.RpcSnapTo(potentialSpawnLocations[j]);    
                    }
                }
            }
        }
    }
}