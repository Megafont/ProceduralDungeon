using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;


namespace ProceduralDungeon.InGame
{

    [RequireComponent(typeof(Inventory))]
    [RequireComponent(typeof(PlayerInput))]
    public class Player : MonoBehaviour
    {
        [SerializeField] float MoveSpeed = 10f;


        Rigidbody2D _MyRigidBody2D;
        Vector2 _MoveInput;


        private Vector2 velocity = Vector2.zero;



        // Start is called before the first frame update
        void Start()
        {
            _MyRigidBody2D = GetComponent<Rigidbody2D>();
        }

        // Update is called once per frame
        void Update()
        {
            Run();
        }

        void FixedUpdate()
        {
            // This had jerky movement originally. Turns out you need to enable Interpolation on the Rigidbody2D component.
            _MyRigidBody2D.MovePosition(_MyRigidBody2D.position + velocity * Time.fixedDeltaTime);
        }



        void OnMove(InputValue value)
        {
            _MoveInput = value.Get<Vector2>();
        }


        void Run()
        {
            Vector2 moveDistance = new Vector2(_MoveInput.x, _MoveInput.y);

            velocity = moveDistance * MoveSpeed;

            //_MyRigidBody2D.MovePosition(_MyRigidBody2D.position + moveDistance);
            //_MyRigidBody2D.position += moveDistance;
            //_MyRigidBody2D.velocity = moveDistance;
        }



    }

}