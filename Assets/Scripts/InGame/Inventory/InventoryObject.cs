using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

using UnityEngine;

using ProceduralDungeon.InGame.Items;


namespace ProceduralDungeon.InGame.Inventory
{
    [CreateAssetMenu(fileName = "New Inventory", menuName = "Inventory System/Inventory")]
    public class InventoryObject : ScriptableObject
    {
        public string ItemSavePath;
        public ItemDatabaseObject ItemDatabase;

        public InventoryData Contents;



        public void AddItem(Item item, uint amountToAdd)
        {
            // If an item has buffs, we always create a new item in the inventory.
            // This simply makes it so that items with buffs are not stackable.
            if (item.Buffs.Length > 0)
            {
                Contents.Items.Add(new InventorySlot(item.ID, item, amountToAdd));
                return;
            }


            // The item does not have buffs, so see if there is already a stack of it
            // in the inventory.
            for (int i = 0; i < Contents.Items.Count; i++)
            {
                if (Contents.Items[i].Item.ID == item.ID)
                {
                    Contents.Items[i].AddItems(amountToAdd);
                    return;
                }

            } // end for i


            // The item was not already in the inventory, so create a new slot.
            Contents.Items.Add(new InventorySlot(item.ID, item, amountToAdd));

        }

        public void CheckSavePath()
        {
            if (!Directory.Exists(string.Concat(Application.persistentDataPath, @"\Inventories")))
                Directory.CreateDirectory(string.Concat(Application.persistentDataPath, @"\Inventories"));
        }

        [ContextMenu("Save Inventory")]
        public void Save()
        {
            CheckSavePath();

            /*
            string saveData = JsonUtility.ToJson(this, true);
            BinaryFormatter bf = new BinaryFormatter();
            FileStream file = File.Create(string.Concat(Application.persistentDataPath, ItemSavePath));
            bf.Serialize(file, saveData);
            file.Close();
            */

            // This approach to saving makes the data so players can't edit it, unlike the Json method above.
            IFormatter formatter = new BinaryFormatter();
            Stream stream = new FileStream(string.Concat(Application.persistentDataPath, ItemSavePath), FileMode.Create, FileAccess.Write);
            
            formatter.Serialize(stream, Contents);
            
            stream.Close();
            
        }

        [ContextMenu("Load Inventory")]
        public void Load()
        {
            if (File.Exists(string.Concat(Application.persistentDataPath, ItemSavePath)))
            {
                /*
                BinaryFormatter bf = new BinaryFormatter();
                FileStream file = File.Open(string.Concat(Application.persistentDataPath, ItemSavePath), FileMode.Open);
                JsonUtility.FromJsonOverwrite(bf.Deserialize(file).ToString(), this);
                file.Close();
                */

                // This approach to saving makes the data so players can't edit it, unlike the Json method above.
                IFormatter formatter = new BinaryFormatter();
                Stream stream = new FileStream(string.Concat(Application.persistentDataPath, ItemSavePath), FileMode.Open, FileAccess.Read);
                
                Contents = (InventoryData)formatter.Deserialize(stream);
                
                stream.Close();
            }
        }

        [ContextMenu("Clear Inventory")]
        public void Clear()
        {
            //Contents = new Inventory();
            Contents.Items.Clear();
        }



        /* NOTE: These methods were part of the commented out Json saving/loading code above.
         *       This class must subscribe to the ISerializationCallbackReceiver interface to use these methods.
         * 
        public void OnAfterDeserialize()
        {
            for (int i = 0; i < Contents.Items.Count; i++)
            {
                Contents.Items[i].Item = Database.ItemLookup[Contents.Items[i].ItemID];
            }
        }

        public void OnBeforeSerialize()
        {

        }
        */


    }


}