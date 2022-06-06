using System.Collections.Generic;

using UnityEngine;

using ProceduralDungeon.InGame.Items;


namespace ProceduralDungeon.InGame
{
    public enum ItemTypes
    {
        Unknown = -1,

        Key,
        Key_Multipart,
        Key_Goal,
    }



    public class ItemData
    {
        public int GroupID; // An ID used to differentiate between multiple sets of the same type of item.
        public uint ItemCount; // How many of this item are present.
        public ItemTypes ItemType; // The type of this item.
        public int ExtraData; // Optional extra data relating to this item.
    }



    public class Inventory : MonoBehaviour
    {
        private List<ItemData> _Items;



        public Inventory()
        {
            _Items = new List<ItemData>();
        }



        /// <summary>
        /// Checks if the inventory contains a given item of some amount.
        /// </summary>
        /// <param name="itemType">The type of item to check for.</param>
        /// <param name="itemCount">The number of the item required. Defaults to 1.</param>
        /// <param name="groupID">The group ID of the item. This is used for things like Key IDs. Defaults to -1. This parameter is completely ignored when negative.</param>
        /// <returns>The item data if it was found, or null otherwise.</returns>
        public ItemData GetItemData(ItemTypes itemType, uint itemCount = 1, int groupID = -1)
        {

            foreach (ItemData itemData in _Items)
            {
                bool bMatch = true;

                if (itemData.ItemType != itemType || 
                    itemData.ItemCount < itemCount)
                {
                    bMatch = false;
                }

                if (groupID >= 0 && itemData.GroupID != groupID)
                    bMatch = false;


                if (bMatch)
                    return itemData;

            } // end foreach itemData


            return null;
        }

        /// <summary>
        /// Checks if the inventory contains a given item of some amount.
        /// </summary>
        /// <param name="itemType">The type of item to check for.</param>
        /// <param name="itemCount">The number of the item required. Defaults to 1.</param>
        /// <param name="groupID">The group ID of the item. This is used for things like Key IDs. Defaults to -1. This parameter is completely ignored when negative.</param>
        /// <returns>True if the item was found or false otherwise.</returns>
        public bool HasItem(ItemTypes itemType, uint itemCount = 1, int groupID = -1)
        {
            return (GetItemData(itemType, itemCount, groupID) != null);
        }

        public void InsertItem(ItemData itemData)
        {
            ItemData currentData = GetItemData(itemData.ItemType, itemData.ItemCount, itemData.GroupID);
            if (currentData != null)
            {
                currentData.ItemCount++;
            }
            else
            {
                if (itemData.ItemCount < 1)
                    itemData.ItemCount = 1;

                _Items.Add(itemData);
            }

        }

        public void InsertItems(Inventory inventory)
        {
            foreach (ItemData itemData in inventory._Items)
                InsertItem(itemData);


            DEBUG_PrintInventory();
        }

        /// <summary>
        /// Checks if the inventory contains a given item of some amount.
        /// </summary>
        /// <param name="itemType">The type of item to remove.</param>
        /// <param name="itemCount">The number of the item to remove.</param>
        /// <param name="groupID">The group ID of the item. This is used for things like Key IDs. Defaults to -1. This parameter is completely ignored when negative.</param>
        /// <returns>True if removal was successful, and false otherwise.</returns>
        public bool RemoveItem(ItemTypes itemType, uint itemCount = 1, int groupID = -1)
        {
            ItemData currentData = GetItemData(itemType, itemCount, groupID);
            if (currentData == null)
                return false;

            if (currentData.ItemCount < itemCount)
                return false;


            currentData.ItemCount -= itemCount;            
            if (currentData.ItemCount == 0)
                _Items.Remove(currentData);

            return true;

        }

        public void Clear()
        {
            _Items.Clear();
        }


        void DEBUG_PrintInventory()
        {
            Debug.Log("INVENTORY CONTENTS:");
            Debug.Log(new string('-', 256));

            foreach (ItemData itemData in _Items)
                Debug.Log($"   {itemData.ItemType} - Count: {itemData.ItemCount}    - Group ID: {itemData.GroupID}");
        }


    }

}