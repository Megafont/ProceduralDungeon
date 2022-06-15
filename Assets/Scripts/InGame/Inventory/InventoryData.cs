using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using ProceduralDungeon.InGame.Items;

namespace ProceduralDungeon.InGame.Inventory
{
    [Serializable]
    public class InventoryData
    {
        public List<InventorySlot> Items;


        [NonSerialized]        // We don't want this serialized, because the database doesn't need to be saved as part of this object since it is stored externally.
        private ItemDatabaseObject _ItemDatabase;



        public InventoryData()
        {
            Items = new List<InventorySlot>();
        }



        public void AddItem(Item item, uint amountToAdd)
        {
            // If an item has buffs, we always create a new item in the inventory.
            // This simply makes it so that items with buffs are not stackable.
            if (item.Buffs.Length > 0)
            {
                if (item.InstanceID == 0)
                    item.InstanceID = _ItemDatabase.GetNextAvailableInstanceID(item.ID);

                Items.Add(new InventorySlot(item, amountToAdd));
                return;
            }


            // The item does not have buffs, so see if there is already a stack of it
            // in the inventory.
            for (int i = 0; i < Items.Count; i++)
            {
                if (Items[i].Item.ID == item.ID)
                {
                    Items[i].AddItems(amountToAdd);
                    return;
                }

            } // end for i


            // The item was not already in the inventory, so create a new slot.
            Items.Add(new InventorySlot(item, amountToAdd));

        }

        public bool ConsumeItem(uint id, uint amount, uint instanceID = 0)
        {
            if (amount == 0)
                throw new ArgumentException("InventoryData.ConsumeItem(name, amount) - Amount must be greater than 0!");


            InventorySlot itemSlot;
            if (!SearchForItem(null, id, instanceID, amount, out itemSlot))
                return false;


            itemSlot.ItemCount -= amount;

            if (itemSlot.ItemCount < 1)
                Items.Remove(itemSlot);

            return true;
        }

        public bool ConsumeItem(string name, uint amount, uint instanceID = 0)
        {
            if (amount == 0)
                throw new ArgumentException("InventoryData.ConsumeItem(name, amount) - Amount must be greater than 0!");


            InventorySlot itemSlot;
            if (!SearchForItem(name, 0, instanceID, amount, out itemSlot))
                return false;


            itemSlot.ItemCount -= amount;

            if (itemSlot.ItemCount < 1)
                Items.Remove(itemSlot);

            return true;
        }

        /// <summary>
        /// Finds an item in the inventory with the specified ID and an item count that is greater than or equal to amount.
        /// </summary>
        /// <param name="id">The ID of the item to search for.</param>
        /// <param name="amount">The minimum amount of that item to check for.</param>
        /// <param name="item">This out parameter returns the first matching item found, or null if no matching item is found.</param>
        /// <returns>True if the item exists in this inventory and its item count is greater than or equal to amount.</returns>
        public bool FindItem(uint id, uint amount, out Item item)
        {
            item = null;


            InventorySlot slot;
            if (!SearchForItem(null, id, 0, amount, out slot))
                return false;


            item = slot.Item;
            return true;            

        }

        /// <summary>
        /// Finds an item in the inventory with the specified name and an item count that is greater than or equal to amount.
        /// </summary>
        /// <param name="name">The name of the item to search for.</param>
        /// <param name="amount">The minimum amount of that item to check for.</param>
        /// <param name="item">This out parameter returns the first matching item found, or null if no matching item is found.</param>
        /// <returns>True if the item exists in this inventory and its item count is greater than or equal to amount.</returns>
        public bool FindItem(string name, uint amount, out Item item)
        {
            item = null;


            InventorySlot slot;
            if (!SearchForItem(name, 0, 0, amount, out slot))
                return false;


            item = slot.Item;
            return true;
        }

        public uint GetItemCount(uint id, uint instanceID = 0)
        {
            InventorySlot itemSlot;
            if (!SearchForItem(null, id, instanceID, 0, out itemSlot))
                return 0;


            return itemSlot.ItemCount;
        }

        public uint GetItemCount(string name, uint instanceID = 0)
        {
            InventorySlot itemSlot;
            if (!SearchForItem(name, 0, instanceID, 0, out itemSlot))
                return 0;


            return itemSlot.ItemCount;
        }

        public bool RemoveItem(uint id, uint instanceID = 0)
        {
            InventorySlot itemSlot;
            if (!SearchForItem(null, id, instanceID, 0, out itemSlot))
                return false;


            Items.Remove(itemSlot);

            return true;
        }

        public bool RemoveItem(string name, uint instanceID = 0)
        {
            InventorySlot itemSlot;
            if (!SearchForItem(name, 0, instanceID, 0, out itemSlot))
                return false;


            Items.Remove(itemSlot);

            return true;
        }

        public void SetItemDatabase(ItemDatabaseObject itemDB)
        {
            _ItemDatabase = itemDB;
        }



        /// <summary>
        /// This function searches for the first item in this inventory that matches the search criteria.
        /// </summary>
        /// <param name="name">The name of the item to find. If this parameter is empty, it is ignored.</param>
        /// <param name="id">The item ID of the item to find. If this parameter is 0, it is ignored.</param>
        /// <param name="instanceID">The instance ID of the item to find. If this parameter is 0, it is ignored.</param>
        /// <param name="amount">The minimum item count required. If this parameter is 0, it is ignored.</param>
        /// <param name="itemSlot">Returns the item slot containing the first matching item found in the inventory, or null otherwise.</param
        /// <returns>True if a matching item was found, or false otherwise.</returns>
        private bool SearchForItem(string name, uint id, uint instanceID, uint amount, out InventorySlot itemSlot)
        {
            itemSlot = null;


            foreach (InventorySlot slot in Items)
            {               
                if ((!string.IsNullOrEmpty(name) && slot.Item.Name != name) || // If a name is supplied and it this item doesn't match it
                    (id > 0 && slot.Item.ID != id) ||                          // If an ID is supplied and this item doesn't match it
                    (instanceID > 0 && slot.Item.InstanceID != instanceID) ||  // If an instanceID is supplied and this item doesn't match it
                    (amount > 0 && slot.ItemCount < amount))                   // If this item's amount is less than the specified amount
                {
                    continue;
                }
                else
                {
                    itemSlot = slot;
                    return true;
                }
            }


            return false;
        }

        private bool SearchForItem(Item item, uint amount, out InventorySlot itemSlot)
        {
            itemSlot = null;

            return SearchForItem(item.Name, item.ID, item.InstanceID, amount, out itemSlot);
        }


    }


}
