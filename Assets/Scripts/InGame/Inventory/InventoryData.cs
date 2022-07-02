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
        public InventorySlot[] InventorySlots;


        [NonSerialized]        // We don't want this serialized, because the database doesn't need to be saved as part of this object since it is stored externally.
        private ItemDatabaseObject _ItemDatabase;


        const int DEFAULT_INVENTORY_SIZE = 25;



        public InventoryData()
        {
            InventorySlots = new InventorySlot[DEFAULT_INVENTORY_SIZE];

            for (int i = 0; i < InventorySlots.Length; i++)
                InventorySlots[i] = new InventorySlot();
        }



        public bool AddItem(ItemData item, uint itemCount)
        {
            if (item == null)
            {
                return false;
            }


            // If an item has buffs, we always create a new item in the inventory.
            // This simply makes it so that items with buffs are not stackable.
            if (item is ItemDataWithBuffs)
            {
                ItemDataWithBuffs itemWithBuffs = (ItemDataWithBuffs) item;
                if (itemWithBuffs.InstanceID == 0)
                    itemWithBuffs.InstanceID = _ItemDatabase.GetNextAvailableInstanceID(itemWithBuffs.ID);

                SetFirstEmptySlot(itemWithBuffs, itemCount);
                return true;
            }


            // The item does not have buffs, so see if there is already a stack of it
            // in the inventory.
            for (int i = 0; i < InventorySlots.Length; i++)
            {
                if (InventorySlots[i].Item != null && InventorySlots[i].Item.ID == item.ID)
                {
                    InventorySlots[i].AddToSlot(itemCount);
                    return true;
                }

            } // end for i


            // The item was not already in the inventory, so create a new slot.
            return SetFirstEmptySlot(item, itemCount) != null;
            
        }

        public bool AddItems(InventoryData data)
        {
            if (data == null)
            {
                //Debug.LogWarning("InventoryData.AddItems() - No items were added to this inventory because the passed in InventoryObject is null!");
                return false;
            }

            if (data.GetOccupiedSlotCount() > GetEmptySlotCount())
                return false;


            foreach (InventorySlot slot in data.InventorySlots)
            {
                if (slot.Item != null)
                    AddItem(slot.Item, slot.ItemCount);
            }

            return true;
        }

        public bool AddItems(InventoryObject otherInventory)
        {
            if (otherInventory == null)
            {
                //Debug.LogWarning("InventoryData.AddItems() - No items were added to this inventory because the passed in InventoryObject is null!");
                return false;
            }

            return AddItems(otherInventory.Data);
        }

        public void Clear()
        {
            InventorySlots = new InventorySlot[DEFAULT_INVENTORY_SIZE];
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
                itemSlot.UpdateSlot(null, 0);

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
                itemSlot.UpdateSlot(null, 0);

            return true;
        }

        /// <summary>
        /// Finds a key in this inventory with the specified ID.
        /// </summary>
        /// <param name="keyType">The type of key to look for.</param>
        /// <param name="keyID">The ID of the key to look for.</param>
        /// <param name="keyPartsRequired">If the key type is multi_part, then this parameter specifies how many parts the player must have.</param>
        /// <returns>True if the key was found and removed from the inventory, or false otherwise.</returns>
        public bool ConsumeKey(KeyTypes keyType, uint keyID, uint keyPartsRequired = 0)
        {
            foreach (InventorySlot itemSlot in InventorySlots)
            {
                if (itemSlot.Item != null && itemSlot.Item is ItemData_Key)
                {
                    ItemData_Key key = (ItemData_Key) itemSlot.Item;

                    if (key.KeyType == keyType && key.KeyID == keyID && itemSlot.ItemCount >= keyPartsRequired)
                    {
                        itemSlot.UpdateSlot(null, 0);
                        return true;
                    }
                }

            } // end foreach slot


            return false;

        }

        /// <summary>
        /// Finds an item in the inventory with the specified ID and an item count that is greater than or equal to amount.
        /// </summary>
        /// <param name="id">The ID of the item to search for.</param>
        /// <param name="amount">The minimum amount of that item to check for.</param>
        /// <param name="item">This out parameter returns the first matching item found, or null if no matching item is found.</param>
        /// <returns>True if the item exists in this inventory and its item count is greater than or equal to amount.</returns>
        public bool FindItem(uint id, uint amount, out ItemData item)
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
        public bool FindItem(string name, uint amount, out ItemData item)
        {
            item = null;


            InventorySlot slot;
            if (!SearchForItem(name, 0, 0, amount, out slot))
                return false;


            item = slot.Item;
            return true;
        }


        public bool FindItem(string name, uint amount, out ItemDataWithBuffs requestedItem)
        {
            requestedItem = null;


            InventorySlot slot;
            if (!SearchForItem(name, 0, 0, amount, out slot))
                return false;


            if (slot.Item is ItemDataWithBuffs)
            {
                requestedItem = (ItemDataWithBuffs) slot.Item;
                return true;
            }
            else
            {
                return false;
            }

        }

        public bool FindItem(uint id, uint amount, out ItemDataWithBuffs requestedItem)
        {
            requestedItem = null;


            InventorySlot slot;
            if (!SearchForItem(null, id, 0, amount, out slot))
                return false;


            if (slot.Item is ItemDataWithBuffs)
            {
                requestedItem = (ItemDataWithBuffs) slot.Item;
                return true;
            }
            else
            {
                return false;
            }

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
        
        public uint GetEmptySlotCount()
        {
            uint count = 0;

            foreach (InventorySlot slot in InventorySlots)
            {
                if (slot.IsEmpty())
                    count++;
            }

            return count;

        }

        public uint GetOccupiedSlotCount()
        {
            uint count = 0;

            foreach (InventorySlot slot in InventorySlots)
            {
                if (slot.Item != null)
                    count++;
            }

            return count;

        }

        public uint GetSlotCount()
        {
            return (uint) InventorySlots.Length;
        }

        public bool RemoveItem(uint id, uint instanceID = 0)
        {
            InventorySlot itemSlot;
            if (!SearchForItem(null, id, instanceID, 0, out itemSlot))
                return false;


            itemSlot.UpdateSlot(null, 0);

            return true;
        }

        public bool RemoveItem(string name, uint instanceID = 0)
        {
            InventorySlot itemSlot;
            if (!SearchForItem(name, 0, instanceID, 0, out itemSlot))
                return false;


            itemSlot.UpdateSlot(null, 0);

            return true;
        }

        

        public void SwapItem(InventorySlot item1, InventorySlot item2)
        {
            InventorySlot temp = new InventorySlot(item2.Item, item2.ItemCount);
            item2.UpdateSlot(item1.Item, item1.ItemCount);
            item1.UpdateSlot(temp.Item, temp.ItemCount);

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
        /// <param name="requestedItemSlot">Returns the item slot containing the first matching item found in the inventory, or null otherwise.</param
        /// <returns>True if a matching item was found, or false otherwise.</returns>
        private bool SearchForItem(string name, uint id, uint instanceID, uint amount, out InventorySlot requestedItemSlot)
        {
            requestedItemSlot = null;


            for (int i = 0; i < InventorySlots.Length; i++)
            {
                InventorySlot curSlot = InventorySlots[i];

                if (curSlot.IsEmpty())
                    continue;

                if ((!string.IsNullOrEmpty(name) && curSlot.Item.Name != name) || // If a name is supplied and it this item DOESN'T match it
                    (id > 0 && curSlot.Item.ID != id) ||                          // If an ID is supplied and this item DOESN'T match it
                    (!InstanceIdMatches(curSlot.Item, instanceID)) ||             // If an instanceID is supplied and this item DOESN'T match it
                    (amount > 0 && curSlot.ItemCount < amount))                   // If this item's amount is less than the specified amount
                {
                    continue;
                }
                else
                {
                    requestedItemSlot = InventorySlots[i];
                    return true;
                }

            } // end foreach itemSlot


            return false;
        }

        private bool SearchForItem(ItemData item, uint amount, out InventorySlot requestedItemSlot)
        {
            requestedItemSlot = null;

            if (item is ItemDataWithBuffs)
            {
                ItemDataWithBuffs itemWithBuffs = (ItemDataWithBuffs) item;
                return SearchForItem(itemWithBuffs.Name, itemWithBuffs.ID, itemWithBuffs.InstanceID, amount, out requestedItemSlot);
            }
            else
            {
                return SearchForItem(item.Name, item.ID, 0, amount, out requestedItemSlot);
            }

        }

        private InventorySlot SetFirstEmptySlot(ItemData item, uint itemCount)
        {
            for (int i = 0; i < InventorySlots.Length; i++)
            {
                if (InventorySlots[i].IsEmpty())
                {
                    InventorySlots[i].UpdateSlot(item, itemCount);

                    return InventorySlots[i];
                }
            }


            // If there was no empty inventory slot.
            return null;
        }

        private bool InstanceIdMatches(ItemData item, uint instanceID)
        {
            // If instanceID is 0, return true since that means we're not searching for an item with buffs anyway.
            // This is because this filter should not block non-buff items from passing the search in this case.
            if (instanceID == 0)
                return true;

            if (item is not ItemDataWithBuffs)
                return false;


            ItemDataWithBuffs itemWithBuffs = (item as ItemDataWithBuffs);
            if (item != null)
            {
                if (itemWithBuffs.InstanceID == instanceID)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                    return false;
            }
            

        }

    }


}
