using System.Collections;
using System.Collections.Generic;

using UnityEngine;


namespace ProceduralDungeon.InGame.Items.Definitions
{
    [CreateAssetMenu(fileName = "New Default Item Object", menuName = "Inventory System/Item Definitions/Default")]
    public class ItemDefinition_Default : ItemDefinitionBase
    {
        public void Awake()
        {
            Type = ItemTypes.Default;
        }
    }

}