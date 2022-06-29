using System.Collections;
using System.Collections.Generic;

using UnityEngine;


namespace ProceduralDungeon.InGame.Items.Definitions
{
    [CreateAssetMenu(fileName = "New Food Item Object", menuName = "Inventory System/Item Definitions/Food")]
    public class ItemDefinition_Food : ItemDefinitionBase
    {

        public void Awake()
        {
            Type = ItemTypes.Food;
        }
    }

}