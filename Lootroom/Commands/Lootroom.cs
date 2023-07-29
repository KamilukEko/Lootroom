using System.Collections.Generic;
using System.Linq;
using Lootroom.Models;
using Lootroom.Utils;
using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using SDG.Unturned;
using UnityEngine;
using Logger = Rocket.Core.Logging.Logger;

namespace Lootroom.Commands;

public class Lootroom : IRocketCommand
{
    public void Execute(IRocketPlayer caller, string[] command)
    {
        var player = (UnturnedPlayer) caller;
        PlayerLook look = player.Player.look;
        
        if (!Physics.Raycast(look.aim.position, look.aim.forward, out RaycastHit hit, 20, RayMasks.BARRICADE_INTERACT))
        {
            UnturnedChat.Say(caller, "You are not looking at any barricade.");
            return;
        }

        if (command.Length > 0)
        {
            if (command[0] == "storage")
            {
                InteractableStorage storage = hit.transform.GetComponent<InteractableStorage>();
                    
                if (storage == null)
                {
                    UnturnedChat.Say(caller, "You are not looking at any storage.");
                    return;
                }

                var transform = storage.transform;
                var rotation = transform.rotation;
                var position = transform.position;
                Logger.Log("<StoragePosition X=\"" + position.x +"\" Y=\"" + position.y +"\" Z=\"" + position.z +"\" />");
                Logger.Log($"<AngleX>{rotation.x}</AngleX>");
                Logger.Log($"<AngleY>{rotation.y}</AngleX>");
                Logger.Log($"<AngleZ>{rotation.z}</AngleX>");
            
                UnturnedChat.Say(caller, "Storage info has been logged in console.");
                return;
            }
        }

        InteractableDoorHinge hinge = hit.transform.GetComponent<InteractableDoorHinge>();
                    
        if (hinge == null)
        {
            UnturnedChat.Say(caller, "You are not looking at any door.");
            return;
        }
                   
        InteractableDoor door = hinge.door;
        var lootroom = new SerializableLootroom {Door = new SerializableVector3(door.transform.position), ClearAfter = 60, 
            Keycards = new List<Keycard> {new() {IsOneTimeUse = true, KeycardID = 329 }}, 
            LootStorages = new List<LootStorage> {new() {StoragePosition = new SerializableVector3(door.transform.position), StorageID = 1280, 
                LootOptions = new List<List<ushort>>{new() {329, 329,329}, new() { 330, 330}}}}
        };

        Main.Instance.Lootrooms[door] = lootroom;
        Main.Instance.Configuration.Instance.Lootrooms.Add(lootroom);
        Main.Instance.Configuration.Save();
        
        UnturnedChat.Say(caller, "Done.");
    }

    public AllowedCaller AllowedCaller => AllowedCaller.Player;
    
    public string Name => "lootroom";

    public string Help => "Creates lootrooms";

    public string Syntax => "";
    
    public List<string> Aliases => new List<string>();
    
    public List<string> Permissions => new List<string>();
}