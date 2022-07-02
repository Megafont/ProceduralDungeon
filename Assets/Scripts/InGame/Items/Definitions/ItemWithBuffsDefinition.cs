using System.Collections;
using System.Collections.Generic;

using UnityEngine;


namespace ProceduralDungeon.InGame.Items.Definitions
{
    /// <summary>
    /// This is the abstract base class for items with buffs.
    /// </summary>
    public class ItemWithBuffsDefinition : ItemDefinition
    {
        public uint InstanceID; // This is a unique ID for items with buffs so you can tell them apart since the ID field will have the same value for all instances of an item.

        public ItemBuff[] Buffs;



        public override ItemData CreateItemInstance()
        {
            ItemDataWithBuffs newItem = new ItemDataWithBuffs(this);
            return newItem;
        }

    }

}