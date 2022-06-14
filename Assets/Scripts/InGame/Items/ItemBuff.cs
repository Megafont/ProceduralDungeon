using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;


namespace ProceduralDungeon.InGame.Items
{

    [Serializable]
    public class ItemBuff
    {
        public ItemAttributes Attribute;
        public int AttributeValue;
        public int MinValue;
        public int MaxValue;        


        public ItemBuff(int minValue, int maxValue)
        {
            MinValue = minValue;
            MaxValue = maxValue;

            GenerateValue();
        }

        public void GenerateValue()
        {
            AttributeValue = UnityEngine.Random.Range(MinValue, MaxValue);
        }


    }


}
