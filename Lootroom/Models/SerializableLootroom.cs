using System.Collections.Generic;

namespace Lootroom.Models
{
    public class SerializableLootroom
    {
        public SerializableVector3 Door;
        public List<Keycard> Keycards;
        public List<LootStorage> LootStorages;
        public float ClearAfter;
    }
}