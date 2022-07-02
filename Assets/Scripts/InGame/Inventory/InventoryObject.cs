using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

using UnityEngine;
using UnityEngine.Assertions;

using ProceduralDungeon.DungeonGeneration;
using ProceduralDungeon.InGame.Items;


namespace ProceduralDungeon.InGame.Inventory
{
    [CreateAssetMenu(fileName = "New Inventory", menuName = "Inventory System/Inventory Object")]
    public class InventoryObject : ScriptableObject
    {
        public string ItemSavePath;
        public ItemDatabaseObject ItemDatabase;
        public InventoryData Data;

        public delegate void OnItemClickedEventHandler(object sender, ItemData itemClicked);

        public event OnItemClickedEventHandler OnItemClicked;



        /// <summary>
        /// NOTE: I changed this method since the Awake method on ScriptableObjects does not work the same as the one on Monobehaviors. It gets called when
        ///       a scene containing this ScriptableObject is loaded. I was having a problem with ItemDatabase being null, and changing this method
        ///       from Awake() to OnEnable() seems to have fixed it.
        /// </summary>
        public void OnEnable()
        {
            Data ??= new InventoryData();
            
            ItemDatabase ??= DungeonGenerator.ItemDatabase;

            Data.SetItemDatabase(ItemDatabase);
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
            
            formatter.Serialize(stream, Data);
            
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
                
                InventoryData newData = (InventoryData)formatter.Deserialize(stream);
                Data.SetItemDatabase(ItemDatabase);
                for (int i = 0; i < Data.InventorySlots.Length; i++)
                {
                    Data.InventorySlots[i].UpdateSlot(newData.InventorySlots[i].Item, newData.InventorySlots[i].ItemCount);
                }
                stream.Close();

                AssignInstanceIDs();
            }
        }

        [ContextMenu("Clear Inventory")]
        public void Clear()
        {
            //Contents = new Inventory();
            Data.Clear();
        }



        private void CheckSavePath()
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(ItemSavePath), "InventoryObject.CheckSavePath() - The ItemSavePath field on this inventory is not set!");

            string path = string.Concat(Application.persistentDataPath, ItemSavePath);
            if (!Directory.Exists(Path.GetDirectoryName(path)))
                Directory.CreateDirectory(path);
        }

        private void AssignInstanceIDs()
        {
            foreach (InventorySlot slot in Data.InventorySlots)
            {
                if (slot.Item is ItemDataWithBuffs)
                {
                    ItemDataWithBuffs itemWithBuffs = (ItemDataWithBuffs)slot.Item;

                    if (itemWithBuffs.Buffs.Count > 0)
                        itemWithBuffs.InstanceID = ItemDatabase.GetNextAvailableInstanceID(slot.Item.ID);
                }

            }

        }

        public void SendItemClickedEventToInventoryOwner(ItemData itemClicked)
        {
            OnItemClicked?.Invoke(this, itemClicked);
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