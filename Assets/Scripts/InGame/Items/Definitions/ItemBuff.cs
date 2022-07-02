using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using ProceduralDungeon.DungeonGeneration;
using ProceduralDungeon.InGame.Items.Definitions;


namespace ProceduralDungeon.InGame.Items.Definitions
{

    [Serializable]
    public class ItemBuff
    {
        public ItemAttributes Attribute;
        public float MinValue;
        public float MaxValue;        



        public ItemBuff(float minValue, float maxValue)
        {
            MinValue = minValue;
            MaxValue = maxValue;
        }



        public float GenerateAttributeValue()
        {
            return DungeonGenerator.RNG_DungeonGen.RollRandomFloatInRange(MinValue, MaxValue);
        }


    }


}
