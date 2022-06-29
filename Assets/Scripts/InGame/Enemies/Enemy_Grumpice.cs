using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using ProceduralDungeon.DungeonGeneration;


namespace ProceduralDungeon.InGame.Enemies
{
    public class Enemy_Grumpice : MonoBehaviour
    {
        public float ContactDamage = 10f;
        public float MaxIdleTime = 5f;
        public float MaxWalkTime = 10f;
        public float WalkSpeed = 6f;


        private Animator _Animator;
        private Health _Health;
        private SpriteRenderer _Renderer;


        private Vector2 _MoveDirection = Vector2.right;

        private float _IdleElapsedTime;
        private float _IdleTime;
        private float _WalkElapsedTime;
        private float _WalkTime;

        private bool _IsWalking = true;


        private LayerMask _LayerMask;



        // Start is called before the first frame update
        void Start()
        {
            _Animator = GetComponent<Animator>();
            _Animator.SetBool("IsWalking", true);

            _Health = GetComponent<Health>();
            _Health.OnDeath += OnDeath;

            _Renderer = GetComponent<SpriteRenderer>();


            // Setup a contact filter.
            _LayerMask = (1 << LayerMask.NameToLayer("Player")) |
                         (1 << LayerMask.NameToLayer("Enemies")) |
                         (1 << LayerMask.NameToLayer("Objects")) |
                         (1 << LayerMask.NameToLayer("Walls"));
        }

        // Update is called once per frame
        void Update()
        {
            RunAI();
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.layer == LayerMask.NameToLayer("Weapons"))
                _Health.DealDamage(10f, DamageTypes.Weapon);

        }
        private void OnCollisionStay2D(Collision2D collision)
        {
            if (collision.gameObject.tag == "Player")
                collision.gameObject.GetComponent<Health>().DealDamage(ContactDamage, DamageTypes.EnemyContact);

        }



        private void RunAI()
        {
            if (_IsWalking)
            {
                _WalkElapsedTime += Time.deltaTime;
                
                if (_WalkElapsedTime >= _WalkTime)
                {
                    SwitchToIdleState();
                    return;
                }


                // Check if the grumpice hit something.
                RaycastHit2D hit = Physics2D.Raycast(transform.position, _MoveDirection, 0.4f, _LayerMask);               
                if (hit.collider != null)
                {
                    SelectMoveDirection();
                    return;
                }
                
                transform.position = (Vector2) transform.position + (_MoveDirection * Time.deltaTime);
            }
            else
            {
                _IdleElapsedTime += Time.deltaTime;

                if (_IdleElapsedTime >= _IdleTime)
                    SwitchToWalkState();                
            }
        }

        private void SelectMoveDirection()
        {
            Vector2 direction = Vector2.right;

            while (true)
            {
                int index = DungeonGenerator.RNG_InGame.RollRandomIntInRange(0, 3);


                switch (index)
                {
                    case 0:
                        direction = Vector2.up;
                        break;

                    case 1:
                        direction = Vector2.right;
                        break;

                    case 2:
                        direction = Vector2.down;
                        break;

                    case 3:
                        direction = Vector2.left;
                        break;

                } // end switch


                if (direction != _MoveDirection)
                    break;

            } // end while


            _MoveDirection = direction;
        }

        private void SwitchToIdleState()
        {
            _IdleTime = DungeonGenerator.RNG_InGame.RollRandomFloat_ZeroToOne() * MaxIdleTime;
            _IsWalking = false;
            _Animator.SetBool("IsWalking", _IsWalking);

            _IdleElapsedTime = 0f;
        }

        private void SwitchToWalkState()
        {
            _WalkTime = DungeonGenerator.RNG_InGame.RollRandomFloat_ZeroToOne() * MaxWalkTime;
            _IsWalking = true;
            _Animator.SetBool("IsWalking", _IsWalking);

            _WalkElapsedTime = 0f;

            SelectMoveDirection();
        }


        private void OnDeath(object sender)
        {
            Destroy(gameObject);
        }

    }

}
