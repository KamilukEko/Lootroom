using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Lootroom.Models;
using Lootroom.Utils;
using Rocket.Core.Plugins;
using Rocket.Unturned;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using UnityEngine;
using Logger = Rocket.Core.Logging.Logger;
using Random = System.Random;

namespace Lootroom
{
    public class Main : RocketPlugin<Config>
    {
        public Dictionary<InteractableDoor, SerializableLootroom> Lootrooms;
        private List<CSteamID> _withKeycards;
        private IEnumerator _keycardCheckLoopCoroutine;
        public static Main Instance;
        
        protected override void Load()
        {
            U.Events.OnPlayerConnected += OnPlayerConnected;
            U.Events.OnPlayerDisconnected += OnPlayerDisconnected;
            Level.onLevelLoaded += OnLevelLoaded;
            SaveManager.onPreSave += OnPreSave;
            BarricadeManager.onDamageBarricadeRequested += OnDamageBarricadeRequested;

            Lootrooms = new Dictionary<InteractableDoor, SerializableLootroom>();
            _withKeycards = new List<CSteamID>();

            _keycardCheckLoopCoroutine = KeycardCheckLoop(Configuration.Instance.KeycardCheckLoopInterval);
            StartCoroutine(_keycardCheckLoopCoroutine);

            Instance = this;

            Logger.Log($"Kamiluk || Lootroom plugin has been loaded");
        }

        private void OnDamageBarricadeRequested(CSteamID instigatorsteamid, Transform barricadetransform, ref ushort pendingtotaldamage, ref bool shouldallow, EDamageOrigin damageorigin)
        {
            shouldallow = true;
            var lootroomDoor = Lootrooms.Keys.FirstOrDefault(x => x.transform == barricadetransform);
            if (lootroomDoor != null)
                shouldallow = false;
        }

        private void OnLevelLoaded(int level)
        {
            var doors = FindObjectsOfType<InteractableDoor>().ToList();

            foreach (var door in doors)
            {
                var lootroom = Configuration.Instance.Lootrooms.FirstOrDefault(x =>
                    (door.transform.position - x.Door.Vector3).sqrMagnitude < 0.1f);

                if (lootroom != null)
                {
                    Lootrooms[door] = lootroom;
                    
                    BarricadeManager.ServerSetDoorOpen(door, false);
                }
            }
        }

        private IEnumerator KeycardCheckLoop(float interval)
        {
            while (true)
            {
                yield return new WaitForSecondsRealtime(interval);

                foreach (var client in Provider.clients)
                {
                    if (!_withKeycards.Contains(client.playerID.steamID))
                        continue;
                    
                    PlayerLook look = client.player.look;
                  
                    if (!Physics.Raycast(look.aim.position, look.aim.forward, out RaycastHit hit, 3, RayMasks.BARRICADE_INTERACT))
                        continue;
                   
                    InteractableDoorHinge hinge = hit.transform.GetComponent<InteractableDoorHinge>();
                    
                    if (hinge == null)
                        continue;
                   
                    InteractableDoor door = hinge.door;
                    
                    if (door.isOpen)
                        continue;
                    
                    if (!Lootrooms.ContainsKey(door))
                        continue;
                    
                    var lootroom = Lootrooms[door];
                    
                    var keycard = lootroom.Keycards.FirstOrDefault(x => x.KeycardID == client.player.equipment.itemID);
                    if (keycard == null)
                        continue;
                    
                    List<Transform> storages = new List<Transform>();
                    
                    foreach (var storage in lootroom.LootStorages)
                    {
                        var storageTransform = BarricadeManager.dropBarricade(new Barricade(storage.StorageID),
                            (Transform)null, storage.StoragePosition.Vector3, storage.AngleX, storage.AngleY, storage.AngleZ, 1, 1);
                        
                        var storageComponent = storageTransform.GetComponent<InteractableStorage>();
                        Random rnd = new Random();

                        foreach (var item in storage.LootOptions[rnd.Next(storage.LootOptions.Count)])
                        {
                            storageComponent.items.tryAddItem(new Item(item, EItemOrigin.ADMIN), false);
                        }
                        
                        storageComponent.items.onStateUpdated();
                        storages.Add(storageTransform);
                    }

                    BarricadeManager.ServerSetDoorOpen(door, true);

                    if (keycard.IsOneTimeUse)
                        client.player.inventory.removeItem(client.player.equipment.equippedPage, client.player.inventory.getIndex(client.player.equipment.equippedPage, client.player.equipment.equipped_x, client.player.equipment.equipped_y));
                    EffectManager.sendEffect(Configuration.Instance.OpenEffectID, 10, door.transform.position);

                    IEnumerator ClearAfter()
                    {
                        yield return new WaitForSecondsRealtime(lootroom.ClearAfter);
                        BarricadeManager.ServerSetDoorOpen(door, false);
                        
                        foreach (var storage in storages)
                        {
                            DamageTool.damage(storage, false, 100000, 1, out _);
                        }
                    }

                    StartCoroutine(ClearAfter());
                }
            }
        }

        private void OnPlayerDisconnected(UnturnedPlayer player)
        {
            player.Player.equipment.onEquipRequested -= OnEquipRequested;
            player.Player.equipment.onDequipRequested -= OnDequipRequested;
        }

        private void OnDequipRequested(PlayerEquipment equipment, ref bool shouldallow)
        {
            if (_withKeycards.Contains(equipment.player.channel.owner.playerID.steamID))
                _withKeycards.Remove(equipment.player.channel.owner.playerID.steamID);
        }

        private void OnPlayerConnected(UnturnedPlayer player)
        {
            player.Player.equipment.onEquipRequested += OnEquipRequested;
            player.Player.equipment.onDequipRequested += OnDequipRequested;
        }

        private void OnEquipRequested(PlayerEquipment equipment, ItemJar jar, ItemAsset asset, ref bool shouldallow)
        {
            var lootRoom = Configuration.Instance.Lootrooms.FirstOrDefault(x => x.Keycards.FirstOrDefault(z => z.KeycardID == asset.id) != null);

            if (lootRoom == null)
                return;
            
            _withKeycards.Add(equipment.player.channel.owner.playerID.steamID);
        }

        public void OnPreSave()
        {
            var i = 0;
            
            foreach (var lootroom in Lootrooms)
                Configuration.Instance.Lootrooms[i].Door = new SerializableVector3(lootroom.Key.transform.position);

            Configuration.Save();
        }

        protected override void Unload()
        {
            U.Events.OnPlayerConnected -= OnPlayerConnected;
            U.Events.OnPlayerDisconnected -= OnPlayerDisconnected;
            Level.onLevelLoaded -= OnLevelLoaded;
            SaveManager.onPreSave -= OnPreSave;
            BarricadeManager.onDamageBarricadeRequested -= OnDamageBarricadeRequested;

            StopCoroutine(_keycardCheckLoopCoroutine);
            
            Logger.Log($"Kamiluk || Lootroom plugin has been unloaded");
        }
    }
}