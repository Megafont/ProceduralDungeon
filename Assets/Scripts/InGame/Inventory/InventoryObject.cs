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
        public InventoryData Data;



        /// <summary>
        /// NOTE: I changed this method since the Awake method on ScriptableObjects does not work the same as the one on Monobehaviors. It gets called when
        ///       a scene containing this ScriptableObject is loaded. I was having a problem with ItemDatabase being null, and changing this method
        ///       from Awake() to OnEnable() seems to have fixed it.
        /// </summary>
        public void OnEnable()
        {
            Data = new InventoryData();
            Data.SetItemDatabase(ItemDatabase);
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
                
                Data = (InventoryData)formatter.Deserialize(stream);
                Data.SetItemDatabase(ItemDatabase);

                stream.Close();

                AssignInstanceIDs();
            }
        }

        [ContextMenu("Clear Inventory")]
        public void Clear()
        {
            //Contents = new Inventory();
            Data.Items.Clear();
        }


        private void AssignInstanceIDs()
        {
            foreach (InventorySlot slot in Data.Items)
            {
                if (slot.Item.Buffs.Length > 0)
                    slot.Item.InstanceID = ItemDatabase.GetNextAvailableInstanceID(slot.Item.ID);
                else
                    slot.Item.InstanceID = 0;
            }

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