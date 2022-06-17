using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using ProceduralDungeon.InGame.Items;


namespace ProceduralDungeon.InGame.Items.Definitions
{
    [CreateAssetMenu(fileName = "New Key Item Object", menuName = "Inventory System/Item Definitions/Key")]
    public class ItemDefinition_Key : ItemDefinition
    {
        public KeyTypes KeyType;
        public uint KeyID;



        public void Awake()
        {
            Type = ItemTypes2.Key;
        }
    }

}