using System;

using UnityEngine;

using ProceduralDungeon.InGame.Items.Definitions;


namespace ProceduralDungeon.InGame.Items
{
    [Serializable]
    public class ItemData
    {
        public string Name;
        public uint ID;

        //[HideInInspector]
        public uint InstanceID; // This is a unique ID for items with buffs so you can tell them apart since the ID field will be the same for all instances of an item.

        public ItemBuff[] Buffs;



        /// <summary>
        /// This class represents an instance of an item in an inventory.
        /// </summary>
        /// <param name="item"></param>
        public ItemData(ItemDefinition item)
        {
            Name = item.name;
            ID = item.ID;

            InstanceID = item.InstanceID;

            Buffs = new ItemBuff[item.Buffs.Length];
            for (int i = 0; i < Buffs.Length; i++)
            {
                Buffs[i] = new ItemBuff(item.Buffs[i].MinValue, item.Buffs[i].MaxValue);
                Buffs[i].Attribute = item.Buffs[i].Attribute;
            }

        }

    }


}
