using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

using ProceduralDungeon.DungeonGeneration;
using ProceduralDungeon.DungeonGeneration.DungeonGraphGeneration;
using ProceduralDungeon.InGame.Items;
using ProceduralDungeon.InGame.Inventory;
using ProceduralDungeon.InGame.Objects;
using ProceduralDungeon.Utilities;


namespace ProceduralDungeon.InGame
{
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(PlayerInput))]
    public class Player : MonoBehaviour
    {
        public InventoryObject Inventory;

        [SerializeField] 
        private float _MoveSpeed = 10f;


        private Animator _Animator;
        private Combat _Combat;
        private Health _Health;
        private Rigidbody2D _RigidBody2D;
        

        private Vector2 _MoveInput;
        private Vector3 _LastMoveDirection = Vector3.up;
        private Vector2 _Velocity;

        ItemData _BombItem;

        private bool _MouseIsOverGUI;



        // Start is called before the first frame update
        void Start()
        {
            _Animator = GetComponent<Animator>();
            _Combat = transform.Find("Weapon").GetComponent<Combat>();
            _Health = GetComponent<Health>();
            _RigidBody2D = GetComponent<Rigidbody2D>();


            //Create a bomb item.
            _BombItem = Inventory.ItemDatabase.LookupByName("Bomb").CreateItemInstance();
            Inventory.Data.AddItem(_BombItem, 3);
            Inventory.OnItemClicked += OnInventoryItemClicked;


            Debug.LogError("Don't forget to remove temporary inventory save/load controls in Player.Update().");

        }

        // Update is called once per frame
        void Update()
        {
            Run();
            Animate();


            // I added this variable, because calling IsPointerOverGameObject in the OnAttackWithMouse() event method triggers a warning message in Unity:
            //      "Calling IsPointerOverGameObject() from within event processing (such as from InputAction callbacks) will not work as expected; it will query UI state from the last frame"
            _MouseIsOverGUI = EventSystem.current.IsPointerOverGameObject();


            if (Input.GetKeyUp(KeyCode.KeypadMinus))
                Inventory.Save();
            if (Input.GetKeyUp(KeyCode.KeypadEnter))
                Inventory.Load();
        }

        void FixedUpdate()
        {
            // This had jerky movement originally. Turns out you need to enable Interpolation on the Rigidbody2D component.
            _RigidBody2D.MovePosition(_RigidBody2D.position + _Velocity * Time.fixedDeltaTime);
        }

        private void OnApplicationQuit()
        {
            Inventory.Data.Clear();
        }



        void OnMove(InputValue value)
        {
            _MoveInput = value.Get<Vector2>();

            if (_MoveInput != Vector2.zero)
            {
                _LastMoveDirection = _MoveInput;
                _Combat.OnPlayerMoved(_MoveInput);               
            }

        }

        void OnAction1()
        {
            PlaceBomb();
        }

        void OnAttack()
        {
            if (string.IsNullOrEmpty(_Combat.GetWeaponItem().Name))
            {
                ItemDataWithBuffs sword;
                if (Inventory.Data.FindItem("Training Sword", 1, out sword))
                {
                    _Combat.EquipWeapon(sword);
                }
                else
                {
                    // Debug.LogError("Player.OnAttack() - The player does not have a sword!");
                    return;
                }
            }

            _Combat.DoAttack(_LastMoveDirection);
        }

        void OnAttackWithMouse()
        {
            if (!_MouseIsOverGUI)
                OnAttack();
        }

        private void OnInventoryItemClicked(object sender, ItemData itemClicked)
        {
            if (itemClicked is ItemDataWithBuffs)
            {

            }
            else
            {
                if (itemClicked.Type == ItemTypes.Food)
                {
                    ItemData_Food foodItem = (ItemData_Food) itemClicked;

                    _Health.Heal(foodItem.RecoveryAmount);

                    Inventory.Data.ConsumeItem(foodItem.Name, 1);
                }

                if (itemClicked.Name == "Bomb")
                    PlaceBomb();
            }
        }

        private void Run()
        {
            Vector2 moveDistance = new Vector2(_MoveInput.x, _MoveInput.y);

            _Velocity = moveDistance * _MoveSpeed;

            if (_Velocity.x != 0 || _Velocity.y != 0)
            {
                // Update the current and previous room variables if needed.
                DungeonGraphNode temp = DungeonGenerator.LookupRoomFromTile(Vector3Int.FloorToInt(transform.position));
                InGameUtils.PlayerVisitedRoom(temp);
            }

            //_MyRigidBody2D.MovePosition(_MyRigidBody2D.position + moveDistance);
            //_MyRigidBody2D.position += moveDistance;
            //_MyRigidBody2D.velocity = moveDistance;
        }

        private void Animate()
        {
            bool isMoving;


            if (_MoveInput.magnitude >= 0.1f || _MoveInput.magnitude <= -0.1f)
                isMoving = true;
            else
                isMoving = false;


            if (isMoving)
            {
                _Animator.SetFloat("X Movement", _MoveInput.x);
                _Animator.SetFloat("Y Movement", _MoveInput.y);
            }


            _Animator.SetBool("Is Moving", isMoving);
        }

        private void PlaceBomb()
        {
            ItemData bombItem;
            if (!Inventory.Data.FindItem("Bomb", 1, out bombItem))
                return;

            LayerMask layerMask = (1 << LayerMask.NameToLayer("Walls")) |
                                  (1 << LayerMask.NameToLayer("Objects"));


            // The player has a bomb, so check if there is empty space in front of him.
            RaycastHit2D hit = Physics2D.Raycast(transform.position, _LastMoveDirection, 1.0f, layerMask);
            if (hit.collider == null)
            {
                //_MyInventory.RemoveItem(ItemTypes.Item_Bomb, 1);
                Inventory.Data.ConsumeItem("Bomb", 1);


                GameObject litBomb = Instantiate(PrefabManager.GetPrefab("Object_Bomb", DungeonGenerator.DungeonTilemapManager.RoomSet),
                                                 transform.position + _LastMoveDirection,
                                                 Quaternion.identity);

                litBomb.GetComponent<Object_Bomb>().LightFuse();
            }

        }


    }

}