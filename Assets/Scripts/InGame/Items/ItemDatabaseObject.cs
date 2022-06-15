using System.Collections;
using System.Collections.Generic;

using UnityEngine;


namespace ProceduralDungeon.InGame.Items
{
    [CreateAssetMenu(fileName = "New Item Database", menuName = "Inventory System/Items/Item Database")]
    public class ItemDatabaseObject : ScriptableObject, ISerializationCallbackReceiver
    {
        public ItemObject[] Items;
        
        
        private Dictionary<uint, ItemObject> _LookupByID;
        private Dictionary<string, ItemObject> _LookupByName;

        private Dictionary<uint, uint> _LookupNextAvailableInstanceIdFromItemId;



        public ItemDatabaseObject()
        {
            InitLookupTables();
        }



        void OnEnable()
        {
            // The _LookupByName dictionary was originally initialized in OnAfterDeserialize(), but this caused an error saying GetName is not allowed to be called during serialization
            // and suggesting to call it in OnEnable() instead. So here it is.
            for (int i = 0; i < Items.Length; i++)
            {
                _LookupByName.Add(Items[i].name, Items[i]);
            }

        }

        public ItemObject LookupByID(uint id)
        {
            return _LookupByID[id];
        }

        public ItemObject LookupByName(string name)
        {
            return _LookupByName[name];
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
            for (int i = 0; i < Items.Length; i++)
            {
                uint id = (uint) i + 1; // We want our IDs to start at 1, not 0.

                Items[i].ID = id; 
                Items[i].InstanceID = 0; // Nothing in the item database needs to have an instance ID set, so just zero it out.

                _LookupByID.Add(id, Items[i]);

            }
        }

        public void OnBeforeSerialize()
        {
            if (_LookupByID != null || _LookupByName != null)
            {
                ClearLookupTables();
            }
            else
            {
                InitLookupTables();
            }

        }



        private void ClearLookupTables()
        {
            _LookupByID.Clear();
            _LookupByName.Clear();

            _LookupNextAvailableInstanceIdFromItemId.Clear();
        }

        private void InitLookupTables()
        {
            _LookupByID = new Dictionary<uint, ItemObject>();
            _LookupByName = new Dictionary<string, ItemObject>();

            _LookupNextAvailableInstanceIdFromItemId = new Dictionary<uint, uint>();
        }


    }


}
