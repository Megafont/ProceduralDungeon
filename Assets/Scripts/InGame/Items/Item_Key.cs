using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using ProceduralDungeon.InGame;


namespace ProceduralDungeon.InGame.Items
{
    public enum KeyTypes
    {
        Key = 0,
        Key_Multipart,
        Key_Goal,
    }


    public class Item_Key : Collectable
    {
        [SerializeField]
        public uint KeyID;
        [SerializeField]
        public KeyTypes KeyType;

        [SerializeField]
        public Sprite KeySprite;
        [SerializeField]
        public Sprite KeyMultipartSprite;
        [SerializeField]
        public Sprite KeyGoalSprite;



        protected override void OnCollected()
        {
            base.OnCollected();

            _Player.GetComponent<Inventory>().InsertItem(new ItemData() { ItemType = KeyTypeFromItemType(KeyType), 
                                                                          ItemCount = 1, 
                                                                          GroupID = (int) KeyID } );

        }

        
        public void UpdateSprite()
        {
            SpriteRenderer renderer = GetComponent<SpriteRenderer>();

            if (KeyType == KeyTypes.Key)
                renderer.sprite = KeySprite;
            else if (KeyType == KeyTypes.Key_Multipart)
                renderer.sprite = KeyMultipartSprite;
            else if (KeyType == KeyTypes.Key_Goal)
                renderer.sprite = KeyGoalSprite;


            // Update the polygon collider to use the collision box specified by the sprite we just set.
            PolygonCollider2D collider = GetComponent<PolygonCollider2D>();
            if (Application.isPlaying) // Use Destroy() if running in the Unity editor's playmode, but we can only use DestroyImmediate() if we are running in the Unity editor.
                Destroy(collider);
            else
                DestroyImmediate(collider);

            PolygonCollider2D newCollider = gameObject.AddComponent<PolygonCollider2D>();
            newCollider.isTrigger = true; // Set it to be a trigger so we can walk through it rather than be blocked by it.

        }

        public static ItemTypes KeyTypeFromItemType(KeyTypes keyType)
        {
            if (keyType == KeyTypes.Key)
                return ItemTypes.Key;
            if (keyType == KeyTypes.Key_Multipart)
                return ItemTypes.Key_Multipart;
            if (keyType == KeyTypes.Key_Goal)
                return ItemTypes.Key_Goal;

            // This should never run, but is just here to stop the compiler complaining that not all paths return a value.
            return ItemTypes.Unknown;
        }


    }

}
