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



        public InventorySlot()
        {
            Item = null;
            ItemCount = 0;
        }

        public InventorySlot(ItemData item, uint itemCount)
        {
            if (item != null &&
                (string.IsNullOrEmpty(item.Name) || item.ID == 0))
            {
                item = null;
            }


            Item = item;
            ItemCount = itemCount;
        }



        public void AddToSlot(uint amountToAdd)
        {
            ItemCount += amountToAdd;
        }

        public void UpdateSlot(ItemData item, uint itemCount)
        {
            Item = item;
            ItemCount = itemCount;
        }

        public bool IsEmpty()
        {
            if (Item == null)
            {
                return true;
            }
            else if (string.IsNullOrEmpty(Item.Name) || Item.ID == 0)
            {
                return true;
            }


            return false;
        }

    }

}
