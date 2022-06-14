using System;


namespace ProceduralDungeon.InGame.Items
{
    [Serializable]
    public class Item
    {
        public string Name;
        public int ID;
        public ItemBuff[] Buffs;



        public Item(ItemObject item)
        {
            Name = item.name;
            ID = item.ID;

            Buffs = new ItemBuff[item.Buffs.Length];
            for (int i = 0; i < Buffs.Length; i++)
            {
                Buffs[i] = new ItemBuff(item.Buffs[i].MinValue, item.Buffs[i].MaxValue);
                Buffs[i].Attribute = item.Buffs[i].Attribute;
            }
        }
    }

}
