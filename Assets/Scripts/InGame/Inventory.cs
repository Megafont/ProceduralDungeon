using ProceduralDungeon.InGame.Items;
using System.Collections.Generic;
using UnityEngine;


namespace ProceduralDungeon.InGame
{
    struct KeyItem
    {
        public uint KeyID;
        public KeyTypes KeyType;        
    }



    public class Inventory : MonoBehaviour
    {
        Dictionary<uint, KeyItem> _Keys;
        Dictionary<uint, uint> _Keys_Multipart; // Stores the ID and count of how many multipart keys with that ID the player has.
        Dictionary<uint, KeyItem> _Keys_Goal;


        private void Start()
        {
            _Keys = new Dictionary<uint, KeyItem>();
            _Keys_Multipart = new Dictionary<uint, uint>();
            _Keys_Goal = new Dictionary<uint, KeyItem>();

        }



        public bool HasKey(KeyTypes keyType, uint keyID, uint multipartKeyCount = 0)
        {
            if (keyType == KeyTypes.Key)
            {
                return _Keys.ContainsKey(keyID);
            }
            else if (keyType == KeyTypes.Key_Multipart)
            {
                if (_Keys_Multipart.ContainsKey(keyID) && _Keys_Multipart[keyID] == multipartKeyCount)
                        return true;                
            }
            else if (keyType == KeyTypes.Key_Goal)
            {
                return _Keys_Goal.ContainsKey(keyID);
            }


            return false;
        }


        public void InsertItem(Collectable item)
        {
            if (item is Item_Key)
                InsertKey((Item_Key) item);


            item.DestroyCollectable();
        }

        public void UseKey(KeyTypes keyType, uint keyID, uint multipartKeyCount = 0)
        {
            if (keyType == KeyTypes.Key && _Keys.ContainsKey(keyID))
                _Keys.Remove(keyID);
            else if (keyType == KeyTypes.Key_Multipart && _Keys_Multipart.ContainsKey(keyID) && _Keys_Multipart[keyID] >= multipartKeyCount)
                _Keys_Multipart.Remove(keyID);
            else if (keyType == KeyTypes.Key_Goal && _Keys_Goal.ContainsKey(keyID))
                _Keys_Goal.Remove(keyID);
        }


        private void InsertKey(Item_Key key)
        {
            KeyItem keyItem = new KeyItem() { KeyID = key.KeyID, KeyType = key.KeyType };

            if (key.KeyType == KeyTypes.Key)
            {
                _Keys.Add(key.KeyID, keyItem);
            }
            else if (key.KeyType == KeyTypes.Key_Multipart)
            {
                if (_Keys_Multipart.ContainsKey(key.KeyID))
                    _Keys_Multipart[key.KeyID]++;
                else
                    _Keys_Multipart.Add(key.KeyID, 1);
            }
            else if (key.KeyType == KeyTypes.Key_Goal)
            {
                _Keys_Goal.Add(key.KeyID, keyItem);
            }
        }


    }

}