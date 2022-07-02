using System.Collections;
using System.Collections.Generic;

using UnityEngine;


namespace ProceduralDungeon.InGame.Items.Definitions
{
    [CreateAssetMenu(fileName = "New Default Item Object", menuName = "Inventory System/Item Definitions/Default")]
    public class ItemDefinition : ItemDefinitionBase, IItemDefinition
    {
        public new uint ID { get { return base.ID; }
                             set { base.ID = value; } }
        public new Sprite Icon { get { return base.Icon; } 
                                 set { base.Icon = value; } }
        public new ItemTypes Type { get { return base.Type; } 
                                    set { base.Type = value; } }
        public new string Description { get { return base.Description; } 
                                        set { base.Description = value; } }



        public override ItemData CreateItemInstance()
        {
            ItemData newItem = new ItemData(this);
            return newItem;
        }


    }

}