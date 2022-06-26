using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.InputSystem;

using ProceduralDungeon.DungeonGeneration;
using ProceduralDungeon.InGame.Items;
using ProceduralDungeon.InGame.Inventory;
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


        private Animator _MyAnimator;
        private Rigidbody2D _MyRigidBody2D;
        
        private Combat _MyCombat;


        private Vector2 _MoveInput;
        private Vector3 _LastMoveDirection = Vector3.up;
        private Vector2 _Velocity;

        ItemData _BombItem;



        // Start is called before the first frame update
        void Start()
        {
            _MyAnimator = GetComponent<Animator>();
            _MyRigidBody2D = GetComponent<Rigidbody2D>();

            _MyCombat = transform.Find("Weapon").GetComponent<Combat>();


            //Create a bomb item.
            _BombItem = new ItemData(Inventory.ItemDatabase.LookupByName("Bomb"));
            Inventory.Data.AddItem(_BombItem, 3);


            Debug.LogError("Don't forget to remove temporary inventory save/load controls in Player.Update().");

        }


        // Update is called once per frame
        void Update()
        {
            Run();
            Animate();


            if (Input.GetKeyUp(KeyCode.KeypadMinus))
                Inventory.Save();
            if (Input.GetKeyUp(KeyCode.KeypadEnter))
                Inventory.Load();
        }

        void FixedUpdate()
        {
            // This had jerky movement originally. Turns out you need to enable Interpolation on the Rigidbody2D component.
            _MyRigidBody2D.MovePosition(_MyRigidBody2D.position + _Velocity * Time.fixedDeltaTime);
        }

        private void OnApplicationQuit()
        {
            Inventory.Data.Items.Clear();
        }



        void OnMove(InputValue value)
        {
            _MoveInput = value.Get<Vector2>();

            if (_MoveInput != Vector2.zero)
            {
                _LastMoveDirection = _MoveInput;
                _MyCombat.OnPlayerMoved(_MoveInput);
            }
        }

        void OnAction1()
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

        void OnAttack()
        {
            _MyCombat.DoAttack(_LastMoveDirection);
        }

        void Run()
        {
            Vector2 moveDistance = new Vector2(_MoveInput.x, _MoveInput.y);

            _Velocity = moveDistance * _MoveSpeed;

            //_MyRigidBody2D.MovePosition(_MyRigidBody2D.position + moveDistance);
            //_MyRigidBody2D.position += moveDistance;
            //_MyRigidBody2D.velocity = moveDistance;
        }

        void Animate()
        {
            bool isMoving;


            if (_MoveInput.magnitude >= 0.1f || _MoveInput.magnitude <= -0.1f)
                isMoving = true;
            else
                isMoving = false;


            if (isMoving)
            {
                _MyAnimator.SetFloat("X Movement", _MoveInput.x);
                _MyAnimator.SetFloat("Y Movement", _MoveInput.y);
            }


            _MyAnimator.SetBool("Is Moving", isMoving);
        }


    }

}