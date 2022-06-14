using System.Collections;
using System.Collections.Generic;

using UnityEngine;


namespace ProceduralDungeon.InGame.Items
{
    [CreateAssetMenu(fileName = "New Item Database", menuName = "Inventory System/Items/Item Database")]
    public class ItemDatabaseObject : ScriptableObject, ISerializationCallbackReceiver
    {
        public ItemObject[] Items;
        public Dictionary<int, ItemObject> ItemLookup = new Dictionary<int, ItemObject>();



        public void OnAfterDeserialize()
        {
            for (int i = 0; i < Items.Length; i++)
            {
                Items[i].ID = i;
                ItemLookup.Add(i, Items[i]);
            }
        }

        public void OnBeforeSerialize()
        {
            if (ItemLookup != null)
                ItemLookup.Clear();
            else
                ItemLookup = new Dictionary<int, ItemObject>();
        }

    }


}
