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
        public int ItemID;
        public Item Item;
        public uint ItemCount;



        public InventorySlot(int id, Item item, uint count)
        {
            ItemID = id;
            Item = item;
            ItemCount = count;
        }



        public void AddItems(uint amountToAdd)
        {
            ItemCount += amountToAdd;
        }

    }

}
