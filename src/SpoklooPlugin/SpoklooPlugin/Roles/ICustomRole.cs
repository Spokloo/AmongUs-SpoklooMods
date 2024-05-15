using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SpoklooPlugin.Roles
{
    public abstract class CustomRole
    {
        public byte RoleId;
        public string Name;
        public Color32 Color;
        public string ImpostorText;
        public string RoleText = "";
        public bool CrewmatesTeam = true;
        public bool HasKillButton = false;

        public abstract Il2CppSystem.Collections.Generic.List<WinningPlayerData> GetWinners(Dictionary<byte, CustomRole[]> customRoles);

        public static readonly CustomRole[] Roles = {
            new Jester(),
            new Sheriff(),
            new Soulstealer(),
            new Lover1(),
            new Lover2()
        };
    }

    public class Jester : CustomRole
    {
        public static Jester Instance;

        public Jester()
        {
            Instance = this;
            RoleId = 0;
            Name = "Jester";
            Color = new Color32(251, 140, 255, 255);
            ImpostorText = "Get voted out to win";
            CrewmatesTeam = false;
        }

        public override Il2CppSystem.Collections.Generic.List<WinningPlayerData> GetWinners(Dictionary<byte, CustomRole[]> customRoles)
        {
            var jesterId = customRoles.FirstOrDefault(x => x.Value.Contains(Instance)).Key;
            var jesterPlayer = GameData.Instance.GetPlayerById(jesterId);
            var winnersList = new Il2CppSystem.Collections.Generic.List<WinningPlayerData>(1);
            winnersList.Add(new WinningPlayerData
            {
                IsYou = jesterId == PlayerControl.LocalPlayer.PlayerId,
                Name = jesterPlayer.PlayerName,
                ColorId = jesterPlayer.ColorId,
                IsImpostor = false,
                SkinId = jesterPlayer.SkinId,
                PetId = jesterPlayer.PetId,
                HatId = jesterPlayer.HatId,
                IsDead = true
            });
            return winnersList;
        }

        public override string ToString()
        {
            return Name;
        }
    }
    
    
    public class Sheriff : CustomRole
    {
        public static Sheriff Instance;
        
        public Sheriff()
        {
            Instance = this;
            RoleId = 1;
            Name = "Sheriff";
            Color = new Color32(251, 200, 24, 255);
            ImpostorText = "Shoot the [FF1919FF]Impostors[]";
            RoleText = "Shoot the Impostor(s)"; 
            HasKillButton = true;
        }

        public override Il2CppSystem.Collections.Generic.List<WinningPlayerData> GetWinners(Dictionary<byte, CustomRole[]> customRoles)
        {
            throw new System.NotImplementedException();
        }

        public override string ToString()
        {
            return Name;
        }
    }
    
    
    public class Soulstealer : CustomRole
    {
        public static Soulstealer Instance;
        
        public Soulstealer()
        {
            Instance = this;
            RoleId = 2;
            Name = "Soulstealer";
            Color = Palette.ImpostorRed;
            ImpostorText = "Morph into dead bodies";
        }

        public override Il2CppSystem.Collections.Generic.List<WinningPlayerData> GetWinners(Dictionary<byte, CustomRole[]> customRoles)
        {
            throw new System.NotImplementedException();
        }

        public override string ToString()
        {
            return Name;
        }
    }
    
    
    public class Lover1 : CustomRole
    {
        public static Lover1 Instance;
        
        public Lover1()
        {
            Instance = this;
            RoleId = 3;
            Name = "Lover";
            Color = new Color32(242, 47, 198, 255);
            ImpostorText = "You are in [F22FC6FF]Love[] with [F22FC6FF]";
            RoleText = "You are in Love with ";
        }

        public override Il2CppSystem.Collections.Generic.List<WinningPlayerData> GetWinners(Dictionary<byte, CustomRole[]> customRoles)
        {
            throw new System.NotImplementedException();
        }

        public override string ToString()
        {
            return Name;
        }
    }
    
    public class Lover2 : CustomRole
    {
        public static Lover2 Instance;
        
        public Lover2()
        {
            Instance = this;
            RoleId = 4;
            Name = "Lover";
            Color = new Color32(242, 47, 198, 255);
            ImpostorText = "You are in [F22FC6FF]Love[] with [F22FC6FF]";
            RoleText = "You are in Love with ";
        }

        public override Il2CppSystem.Collections.Generic.List<WinningPlayerData> GetWinners(Dictionary<byte, CustomRole[]> customRoles)
        {
            throw new System.NotImplementedException();
        }

        public override string ToString()
        {
            return Name;
        }
    }
}