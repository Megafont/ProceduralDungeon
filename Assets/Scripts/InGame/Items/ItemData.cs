﻿using System;
using System.Collections.Generic;

using UnityEngine;

using ProceduralDungeon.InGame.Items.Definitions;


namespace ProceduralDungeon.InGame.Items
{
    [Serializable]
    public class ItemData
    {
        public string Name;
        public uint ID;
        public ItemTypes Type;


        /// <summary>
        /// This class represents an instance of an item in an inventory.
        /// </summary>
        /// <param name="item"></param>
        public ItemData(ItemDefinitionBase item)
        {
            Name = item.name;
            ID = item.ID;
            Type = item.Type;
        }


    }

}
