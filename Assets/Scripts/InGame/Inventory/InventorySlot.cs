using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using ProceduralDungeon.InGame.Items;


namespace ProceduralDungeon.InGame.Inventory
{
    [Serializable]
    public class InventorySlot
    {
        public uint ItemCount;
        public ItemData Item;



        public InventorySlot(ItemData item, uint count)
        {
            Item = item;
            ItemCount = count;
        }



        public void AddItems(uint amountToAdd)
        {
            ItemCount += amountToAdd;
        }

    }

}
