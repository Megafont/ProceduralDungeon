using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.InputSystem;

using ProceduralDungeon.InGame.Items;


namespace ProceduralDungeon.InGame
{

    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(Inventory))]
    [RequireComponent(typeof(PlayerInput))]
    public class Player : MonoBehaviour
    {
        [SerializeField] 
        private float _MoveSpeed = 10f;


        private Vector3 _LastMoveDirection = Vector3.right;

        private Rigidbody2D _MyRigidBody2D;
        private Vector2 _MoveInput;
        private Vector2 _Velocity;

        private Inventory _MyInventory;
        private GameObject _Prefab_Item_Bomb;



        // Start is called before the first frame update
        void Start()
        {
            _MyRigidBody2D = GetComponent<Rigidbody2D>();

            _MyInventory = GetComponent<Inventory>();
            
            _MyInventory.InsertItem(new ItemData() { ItemType = ItemTypes.Item_Bomb, ItemCount = 10, GroupID = 0 });

            if (_Prefab_Item_Bomb == null)
                _Prefab_Item_Bomb = (GameObject)Resources.Load("Prefabs/Items/Item_Bomb");
        }


        // Update is called once per frame
        void Update()
        {
            Run();
        }

        void FixedUpdate()
        {
            // This had jerky movement originally. Turns out you need to enable Interpolation on the Rigidbody2D component.
            _MyRigidBody2D.MovePosition(_MyRigidBody2D.position + _Velocity * Time.fixedDeltaTime);
        }



        void OnMove(InputValue value)
        {
            _MoveInput = value.Get<Vector2>();
            
            if (_MoveInput != Vector2.zero)
                _LastMoveDirection = _MoveInput;
        }

        void OnAction1()
        {
            // Check if the player has a bomb.
            if (!_MyInventory.ContainsItem(ItemTypes.Item_Bomb, 1))
                return;

            LayerMask layerMask = (1 << LayerMask.NameToLayer("Walls")) |
                                  (1 << LayerMask.NameToLayer("Objects"));

            // The player has a bomb, so check if there is empty space in front of him.
            RaycastHit2D hit = Physics2D.Raycast(transform.position, _LastMoveDirection, 1.0f, layerMask);
            if (hit.collider == null)
            {
                _MyInventory.RemoveItem(ItemTypes.Item_Bomb, 1);

                GameObject litBomb = Instantiate(_Prefab_Item_Bomb, 
                                                 transform.position + _LastMoveDirection, 
                                                 Quaternion.identity);

                litBomb.GetComponent<Item_Bomb>().LightFuse();
            }

        }

        void Run()
        {
            Vector2 moveDistance = new Vector2(_MoveInput.x, _MoveInput.y);

            _Velocity = moveDistance * _MoveSpeed;

            //_MyRigidBody2D.MovePosition(_MyRigidBody2D.position + moveDistance);
            //_MyRigidBody2D.position += moveDistance;
            //_MyRigidBody2D.velocity = moveDistance;
        }



    }

}