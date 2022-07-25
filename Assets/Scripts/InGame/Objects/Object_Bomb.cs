using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using ProceduralDungeon.DungeonGeneration;
using ProceduralDungeon.DungeonGeneration.DungeonGraphGeneration;
using ProceduralDungeon.InGame.Objects;
using ProceduralDungeon.TileMaps;
using ProceduralDungeon.Utilities;


namespace ProceduralDungeon.InGame.Objects
{

    public class Object_Bomb : MonoBehaviour
    {
        public enum BombStates
        {
            Collectable,
            LightFuse,
            FuseBurning,
            Detonate,
            Exploding,
            Destroyed,
        }


        [SerializeField]
        private float _ExplosionDamage = 25f;
        [SerializeField]
        private float _FuseTime = 3.0f;


        private SpriteRenderer _BombSpriteRenderer;
        private ParticleSystem _FuseParticles;
        private ParticleSystem _ExplosionParticles;
        private Animator _ShockwaveAnimation;

        private BombStates _BombState = BombStates.Collectable;
        private float _ElapsedTime;



        void Awake()
        {
            _BombSpriteRenderer = GetComponent<SpriteRenderer>();

            GameObject objFuseParticles = transform.Find("FuseParticles").gameObject;
            GameObject objExplosionParticles = transform.Find("ExplosionParticles").gameObject;
            GameObject objShockwave = transform.Find("Shockwave Ring").gameObject;
            
            _FuseParticles = objFuseParticles.GetComponent<ParticleSystem>();
            _ExplosionParticles = objExplosionParticles.GetComponent<ParticleSystem>();
            _ShockwaveAnimation = objShockwave.GetComponent<Animator>();



            DungeonGraphNode roomNode = DungeonGenerator.LookupRoomFromTile(new Vector3Int((int) transform.position.x, (int) transform.position.y, (int) transform.position.z));
            RoomSets roomSet = RoomSets.Ice;
            if (roomNode != null)
                roomSet = roomNode.RoomBlueprint.RoomSet;

            GetComponent<SpriteRenderer>().sprite = SpriteManager.GetSprite("Item_Bomb", roomSet);
        }

        // Update is called once per frame
        void Update()
        {
            switch (_BombState)
            {
                case BombStates.FuseBurning:
                    _ElapsedTime += Time.deltaTime;
                    if (_ElapsedTime >= _FuseTime)
                        _BombState = BombStates.Detonate;
                    break;

                case BombStates.Detonate:
                    Explode();
                    _BombState = BombStates.Exploding;
                    break;

                case BombStates.Exploding:
                    _ElapsedTime += Time.deltaTime;
                    if (!_ExplosionParticles.isPlaying)
                        _BombState = BombStates.Destroyed;
                    break;

                case BombStates.Destroyed:
                    Destroy(gameObject);
                    break;
            }


            if (Input.GetKeyUp(KeyCode.Space))
                Explode();
        }
        
        public void LightFuse()
        {
            _ElapsedTime = 0;

            // Set the duration of the fuse particles.
            ParticleSystem.MainModule data = _FuseParticles.main;
            data.duration = _FuseTime;

            _FuseParticles.Play();

            _BombState = BombStates.FuseBurning;
        }



        private void Explode()
        {
            _ElapsedTime = 0;

            _BombSpriteRenderer.sprite = null;

            _ShockwaveAnimation.ResetTrigger("Explode");

            _ExplosionParticles.Play();
            _ShockwaveAnimation.SetTrigger("Explode");


            // Deal damage to the player or enemies who may be too close.
            DealDamage();
        }

        private void DealDamage()
        {
            LayerMask layerMask = (1 << LayerMask.NameToLayer("Player")) |
                                  (1 << LayerMask.NameToLayer("Enemies")) |
                                  (1 << LayerMask.NameToLayer("Objects"));
                                
            // Find all characters (the player or enemies) that are within the blast radius of the bomb.
            Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, 1.5f, layerMask);

            // Deal damage to all characters that were found.
            foreach (Collider2D collider in colliders)
            {
                if (collider.gameObject.CompareTag("Player") ||
                    collider.gameObject.CompareTag("Enemy"))
                {
                    Health health = collider.gameObject.GetComponent<Health>();

                    if (health != null)
                        health.DealDamage(_ExplosionDamage, DamageTypes.BombBlast);
                }
                else if (collider.gameObject.CompareTag("Door_BombableWall"))
                {
                    collider.gameObject.GetComponent<Object_Door_BombableWall>().OpenBombWall();
                }


            } // end foreach collider


        }



    }


}
