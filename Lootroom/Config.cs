using System.Collections.Generic;
using Lootroom.Models;
using Rocket.API;

namespace Lootroom
{
    public class Config : IRocketPluginConfiguration
    {
        public List<SerializableLootroom> Lootrooms;
        public float KeycardCheckLoopInterval;
        public ushort OpenEffectID;

        public void LoadDefaults()
        {
            Lootrooms = new List<SerializableLootroom>();
            KeycardCheckLoopInterval = 0.5f;
            OpenEffectID = 56;
        }
    }
}