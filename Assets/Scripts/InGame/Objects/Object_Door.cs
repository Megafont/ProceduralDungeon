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


        private GameObject _Player;



        private void Start()
        {
            _Player = GameObject.FindGameObjectWithTag("Player");
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.collider.tag == "Player")
            {
                Inventory inventory = _Player.GetComponent<Inventory>();


                bool playerCanUnlock = false;
                KeyTypes keyType = KeyTypes.Key;

                if (LockType == DoorLockTypes.Lock && inventory.HasKey(KeyTypes.Key, Key_ID))
                {
                    keyType = KeyTypes.Key;
                    playerCanUnlock = true;
                }
                else if (LockType == DoorLockTypes.Lock_Multipart && inventory.HasKey(KeyTypes.Key_Multipart, Key_ID, MultipartKeyCount))
                {
                    keyType = KeyTypes.Key_Multipart;
                    playerCanUnlock = true;
                }
                else if (LockType == DoorLockTypes.Lock_Goal && inventory.HasKey(KeyTypes.Key_Goal, Key_ID))
                {
                    keyType = KeyTypes.Key_Goal;
                    playerCanUnlock = true;
                }


                if (playerCanUnlock)
                {
                    if (LockType != DoorLockTypes.Lock_Multipart)
                        inventory.UseKey(keyType, Key_ID);
                    else
                        inventory.UseKey(keyType, Key_ID, MultipartKeyCount);

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