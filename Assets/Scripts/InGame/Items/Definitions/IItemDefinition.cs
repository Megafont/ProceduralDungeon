using System.Collections;
using System.Collections.Generic;

using UnityEngine;


namespace ProceduralDungeon.InGame.Items.Definitions
{
    public interface IItemDefinition
    {
        public uint ID { get; set; }
        public Sprite Icon { get; set; }
        public ItemTypes Type { get; set; }
        public string Description { get; set; }
        


        public ItemData CreateItemInstance();
        

    }

}