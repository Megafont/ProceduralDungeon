using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using ProceduralDungeon.InGame.Items.Definitions;


namespace ProceduralDungeon.InGame.Items
{
    [CreateAssetMenu(fileName = "New Item Database", menuName = "Inventory System/Item Database")]
    [ExecuteInEditMode]
    public class ItemDatabaseObject : ScriptableObject, ISerializationCallbackReceiver
    {
        public ItemDefinition[] Items;
        
        
        private Dictionary<uint, ItemDefinition> _LookupByID;
        private Dictionary<string, ItemDefinition> _LookupByName;

        private Dictionary<uint, uint> _LookupNextAvailableInstanceIdFromItemId;



        public ItemDatabaseObject()
        {
            PopulateLookupTables(false);
        }



        void OnEnable()
        {
            InitLookupTables();
            PopulateLookupTables(false);
        }



        public IItemDefinition LookupByID(uint id)
        {
            ItemDefinition item;
            _LookupByID.TryGetValue(id, out item);

            return item;
        }

        public IItemDefinition LookupByName(string name)
        {
            ItemDefinition item;
            _LookupByName.TryGetValue(name, out item);

            return item;
        }

        public uint GetNextAvailableInstanceID(uint itemID)
        {
            uint instanceID;
            if (!_LookupNextAvailableInstanceIdFromItemId.TryGetValue(itemID, out instanceID))
            {
                _LookupNextAvailableInstanceIdFromItemId.Add(itemID, 1); // The first instance of an item with buffs should have ID 1, not 0. The inventory class' search function uses 0 to mean ignore this search parameter.
                return 1;
            }
            else
            {
                return ++_LookupNextAvailableInstanceIdFromItemId[itemID];
            }

        }



        public void OnAfterDeserialize()
        {
            PopulateLookupTables(true);
        }

        public void OnBeforeSerialize()
        {
            InitLookupTables();
        }



        private void ClearLookupTables()
        {
            _LookupByID.Clear();
            _LookupByName.Clear();

            _LookupNextAvailableInstanceIdFromItemId.Clear();
        }

        private void InitLookupTables()
        {
            //Debug.LogError("ItemDatabaeObject.InitLookupTables() called!");

            if (_LookupByID == null || _LookupByName == null)
            {
                _LookupByID = new Dictionary<uint, ItemDefinition>();
                _LookupByName = new Dictionary<string, ItemDefinition>();

                _LookupNextAvailableInstanceIdFromItemId = new Dictionary<uint, uint>();
            }
            else
            {
                ClearLookupTables();
            }

        }

        public void PopulateLookupTables(bool isSerializing)
        {
            if (_LookupByID == null || _LookupByName == null || Items == null)
                return;


            PopulateIDLookup();


            // This prevents us from having the error that GetName() cannot be called during serialization.
            // Unfortunately, it also means that the LookupByName dictionary won't work right in Unity's edit mode
            // if the ItemDatabaseObject is open in the Inspector. This is because when that is open in the Inspector, it causes
            // OnBeforeSerialize() to get called over and over and over again. As you can see above, that method
            // in turn calls InitLookupTables() each time it is called, causing the dictionaries to get cleared
            // again. The problem is that the previously mentioned error prevents the name lookup dictionary from getting
            // repopulated. So if you have problems with null references exceptions, just close the ItemDatabaseObject
            // in the Unity Inspector by selecting some other object in your project!
            // I was not able to find a solution to this problem other than that. You can see the problem in action
            // by uncommenting the debug output line at the top of InitLookupTables() and then opening the ItemDatabaseObject
            // in the inspector. That debug line's message will start showing up continuously in the console.
            // This happens not just in Unity's editor mode, but also in play mode.
            if (!isSerializing)
            {
                PopulateNameLookup();
                SortItemsArray();
            }

        }

        /// <summary>
        /// Populates the _LookupByID dictionary.
        /// </summary>
        private void PopulateIDLookup()
        {
            for (int i = 0; i < Items.Length; i++)
            {
                uint id = (uint)i + 1; // We want our IDs to start at 1, not 0.

                Items[i].ID = id;

                // Nothing in the item database needs to have an instance ID set, so just zero it out if the item is one with buffs.
                if (Items[i] is ItemWithBuffsDefinition)
                    (Items[i] as ItemWithBuffsDefinition).InstanceID = 0;

                _LookupByID.Add(id, Items[i]);

            }

        }

        /// <summary>
        /// Populates the _LookupByName dictionary.
        /// </summary>
        /// <remarks>
        /// The _LookupByName dictionary was originally initialized in OnAfterDeserialize(), but this caused an error saying GetName is not allowed to be called during serialization
        /// and suggesting to call it in OnEnable() instead, so I moved it here in its own method.
        /// </remarks>
        private void PopulateNameLookup()
        {
            _LookupByName.Clear();

            for (int i = 0; i < Items.Length; i++)
            {
                //Debug.LogError($"Added Item[{i}] to DB: \"{Items[i].name}\"");
                _LookupByName.Add(Items[i].name, Items[i]);
            }
        }

        /// <summary>
        /// Sorts the Items array.
        /// </summary>
        private void SortItemsArray()
        {
            List<ItemDefinition> ItemList = new List<ItemDefinition>(Items);

            ItemList.Sort(ItemDefinitionBase.CompareByName);

            Items = ItemList.ToArray();
        }


    }




}
