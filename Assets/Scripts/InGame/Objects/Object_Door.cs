using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using ProceduralDungeon.InGame.Items;


namespace ProceduralDungeon.InGame.Objects
{
    public enum DoorStates
    {
        Open = 0,
        Closed,
        Locked,
    }

    public enum DoorLockTypes
    {
        None = 0,
        Lock,
        Lock_Multipart,
        Lock_Goal,
    }


    public class Object_Door : MonoBehaviour
    {
        [SerializeField]
        public Sprite ClosedSprite;
        [SerializeField]
        public Sprite LockedSprite;
        [SerializeField]
        public Sprite LockedMultipartSprite;
        [SerializeField]
        public Sprite LockedGoalSprite;


        [SerializeField]
        public DoorStates DoorState = DoorStates.Open;
        [SerializeField]
        public DoorLockTypes LockType = DoorLockTypes.None;

        [SerializeField]
        public uint Key_ID;
        [SerializeField]
        public uint MultipartKeyCount = 0;



        private static GameObject _Player;



        private void Start()
        {
            if (_Player == null)
                _Player = GameObject.FindGameObjectWithTag("Player");
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.collider.tag == "Player")
            {
                Inventory inventory = _Player.GetComponent<Inventory>();


                bool unlocked = false;

                if (LockType == DoorLockTypes.Lock && 
                    inventory.RemoveItem(ItemTypes.Key, 1, (int) Key_ID))
                {
                    unlocked = true;
                }
                else if (LockType == DoorLockTypes.Lock_Multipart && 
                         inventory.RemoveItem(ItemTypes.Key_Multipart, MultipartKeyCount, (int) Key_ID))
                {
                    unlocked = true;
                }
                else if (LockType == DoorLockTypes.Lock_Goal &&
                         inventory.RemoveItem(ItemTypes.Key_Goal, 1, (int) Key_ID))
                {
                    unlocked = true;
                }


                if (unlocked)
                {
                    DoorState = DoorStates.Closed; // Switch from locked state to just closed so we can open the door.
                    ToggleState(); // Open the door.
                }

            }


        }



        public void ToggleState()
        {
            bool state = true;

            if (DoorState == DoorStates.Closed)
                state = false;
                
                   
            GetComponent<BoxCollider2D>().enabled = state;


            UpdateSprite();

        }
       
        public void UpdateSprite()
        {
            SpriteRenderer renderer = GetComponent<SpriteRenderer>();

            if (DoorState == DoorStates.Locked)
            {
                if (LockType == DoorLockTypes.Lock)
                    renderer.sprite = LockedSprite;
                else if (LockType == DoorLockTypes.Lock_Multipart)
                    renderer.sprite = LockedMultipartSprite;
                else if (LockType == DoorLockTypes.Lock_Goal)
                    renderer.sprite = LockedGoalSprite;
            }
            else
            {
                renderer.sprite = null;
            }

        }
    }

}