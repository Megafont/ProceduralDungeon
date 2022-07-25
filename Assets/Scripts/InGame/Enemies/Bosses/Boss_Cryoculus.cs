using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using ProceduralDungeon.DungeonGeneration;
using ProceduralDungeon.DungeonGeneration.DungeonGraphGeneration;
using ProceduralDungeon.InGame.Projectiles;
using ProceduralDungeon.Utilities;


namespace ProceduralDungeon.InGame.Bosses
{

    public class Boss_Cryoculus : MonoBehaviour
    {
        [Tooltip("The number of gun arms Cryoculus starts with.")]
        public int NumberOfArms = 4;
        [Tooltip("The starting rotation rate of the arms in degrees.")]
        public float BaseArmRotationRate = 20.0f;
        [Tooltip("The amount by which the arm rotation rate increases each time an arm is destroyed.")]
        public float ArmRotationRateStep = 20.0f;

        [Tooltip("The initial delay between each time the gun arms fire.")]
        public float BaseArmFireDelay = 2.0f;
        [Tooltip("The amount the gun fire delay changes by each time an arm is destroyed.")]
        public float ArmFireRateStep = -0.5f;

        [Tooltip("The base fire delay for projectiles from the eye.")]
        public float BaseEyeFireDelay = 1.0f;
        [Tooltip("The amount the eye's fire rate increases by each time it takes damage.")]
        public float EyeFireRateStep = -0.1f;
        [Tooltip("The speed of the eye projectiles.")]
        public float EyeBulletSpeed = 4.0f;
        [Tooltip("The initial size of bullet volleys fired from the eye.")]
        public int BaseEyeBulletVolleySize = 3;
        [Tooltip("The amount the eye's bullet volley size increases by each time it takes damage.")]
        public int EyeBulletVolleySizeStep = 1;
        [Tooltip("The delay between the eye's bullet volleys.")]
        public float EyeBulletGroupDelay = 2.0f;



        private Animator _Animator;
        private Health _Health;
        private DungeonGraphNode _ParentRoom;
        private GameObject _Player;

        private GameObject _ArmsParent;
        private List<Boss_Cryoculus_Arm> _Arms;

        private Boss_Cryoculus_Eye _Eye;

        private float _ArmRotationRate;
        private bool _ArmsRotateClockwise = true; // Controls the current rotation direction of the arms, which changes when one dies.

        private List<GameObject> _BulletPool;
        private GameObject _BulletParent;
        private GameObject _BulletPrefab;


        private int _EyeFireVolleySize;
        private int _EyeFireCount;

        private float _FireDelay;
        private float _TimeSinceLastFire;
        private bool _FiringVolley = false;
        


        // Start is called before the first frame update
        void Start()
        {
            _Animator = GetComponent<Animator>();

            _FireDelay = BaseArmFireDelay;
            _ArmRotationRate = BaseArmRotationRate;

            _Health = GetComponent<Health>();
            _Health.IsVulnerable = false;
            _Health.OnTakeDamage += OnTakeDamage;
            _Health.OnDeath += OnDeath;

            _ParentRoom = DungeonGenerator.LookupRoomFromTile(Vector3Int.FloorToInt(transform.position));

            _Player = GameObject.Find("Player");

            _ArmsParent = transform.Find("Arms").gameObject;
            _Eye = transform.Find("Eye").gameObject.GetComponent<Boss_Cryoculus_Eye>();

            DungeonGraphNode parentRoom = DungeonGenerator.LookupRoomFromTile(Vector3Int.FloorToInt(transform.position));
            _BulletPool = new List<GameObject>();
            _BulletParent = GameObject.Find("SpawnedProjectiles");
            _BulletPrefab = PrefabManager.GetPrefab("Projectile_Cryoculus", parentRoom.RoomBlueprint.RoomSet);

            SpawnArms(NumberOfArms);
        }

        // Update is called once per frame
        void Update()
        {
            // Cause the boss to do circular laps in the room.
            float x = Mathf.Sin(Time.time * 0.5f) * 1;
            float y = Mathf.Cos(Time.time * 0.5f) * 1;

            // Give the boss smaller circular sub movements on top of the main circular path.
            float x2 = Mathf.Sin(Time.time * 0.5f * 4f);
            float y2 = Mathf.Cos(Time.time * 0.5f * 4f);

            Vector3 newPosLocal = new Vector3(x + x2, y + y2, 0);
            transform.position = newPosLocal + _ParentRoom.RoomCenterPoint;


            RotateArms();


            _TimeSinceLastFire += Time.deltaTime;
            if (_TimeSinceLastFire >= _FireDelay)
            {
                // Are we in the first phase of the battle?
                if (_Arms.Count > 0)
                {
                    FireGuns();
                    _TimeSinceLastFire = 0;
                }
                else // We are in the second phase of the battle.
                {
                    //Debug.Log($"{_EyeFireCount} - {_EyeFireVolleySize} - {_TimeSinceLastFire}");

                    // Check if a full volley has been fired from the eye.
                    if (_FiringVolley)
                    {
                        FireEyeProjectile();
                        _TimeSinceLastFire = 0;
                    }
                    else
                    {
                        // If the delay between bullet volleys has elapsed, then reset the fire count to 0 and initiate the next volley at the player.
                        if (_TimeSinceLastFire >= EyeBulletGroupDelay)
                        {
                            _EyeFireCount = 0;
                            _FiringVolley = true;
                        }
                    }


                }


            }


        }

        void OnDestroy()
        {
            DestroyArms();    
        }



        public GameObject GetBulletFromPool(Vector3 spawnPosition, Quaternion rotation, Vector3 velocity)
        {
            GameObject bullet;

            if (_BulletPool.Count < 1)
            {
                bullet = Instantiate(_BulletPrefab, spawnPosition, rotation, _BulletParent.transform);
            }
            else
            {
                bullet = _BulletPool[0];
                bullet.transform.parent = _BulletParent.transform;
                bullet.transform.position = spawnPosition;
                bullet.transform.rotation = rotation;

                _BulletPool.RemoveAt(0);

            }


            bullet.GetComponent<Projectile_Cryoculus>().OnCollision += OnProjectileCollision;

            bullet.SetActive(true);

            bullet.GetComponent<Rigidbody2D>().velocity = velocity;


            return bullet;
        }

        public DungeonGraphNode GetParentRoom()
        {
            return _ParentRoom;
        }


        private void SpawnArms(int number)
        {
            if (_Arms != null)
                DestroyArms();


            _Arms = new List<Boss_Cryoculus_Arm>();


            GameObject armPrefab = PrefabManager.GetPrefab("Boss_Cryoculus_Arm", _ParentRoom.RoomBlueprint.RoomSet);
            Quaternion q = Quaternion.identity;
            for (int i = 0; i < NumberOfArms; i++)
            {
                Vector3 rotation = q.eulerAngles;
                rotation.z = (360 / NumberOfArms) * i;
                q.eulerAngles = rotation;

                GameObject arm = Instantiate(armPrefab,
                                             transform.position, 
                                             q,
                                             _ArmsParent.transform);


                GameObject child = arm.transform.GetChild(0).gameObject;

                // Subscribe to the OnDeath event of the arm's Health component so we can be notified whenever one of the arms is destroyed.
                child.GetComponent<Health>().OnDeath += OnArmDestroyed;

                Boss_Cryoculus_Arm armComponent = child.GetComponent<Boss_Cryoculus_Arm>();
                armComponent.ParentCryoculus = this;
                _Arms.Add(armComponent);
            }

        }

        private void RotateArms()
        {
            foreach (Boss_Cryoculus_Arm arm in _Arms)
            {
                Vector3 rotation = arm.transform.parent.rotation.eulerAngles;
                
                float changeAmount = _ArmsRotateClockwise ? _ArmRotationRate :
                                                            -_ArmRotationRate;
                changeAmount *= Time.deltaTime;

                rotation.z += changeAmount;
                arm.transform.parent.eulerAngles = rotation;
            }
        }

        private void DestroyArms()
        {
            if (_Arms == null || _Arms.Count < 1)
                return;


            for (int i = 0; i < _Arms.Count; i++)
                Destroy(_Arms[i]);

            _Arms.Clear();
        }

        private void OnArmDestroyed(object sender)
        {
            GameObject arm = (GameObject) sender;

            // NOTE: This does not use transform.GetChild() like we do above in SpawnArms() because it is not necessary.
            //       Since the Health component is on the child object in the arm prefab, it will return the game object
            //       of the sub object (the visual aspects of the arm).
            //       This is the same reason the following line removes the sender's parent object from the list rather than the sender itself.
            arm.GetComponent<Health>().OnDeath -= OnArmDestroyed;

            _Arms.Remove(arm.GetComponent<Boss_Cryoculus_Arm>());


            // Increase arm rotatation speed and change direction.
            _ArmRotationRate += ArmRotationRateStep;
            _ArmsRotateClockwise = !_ArmsRotateClockwise;

            // Increase fire rate.
            _FireDelay += ArmFireRateStep;


            if (_Arms.Count < 1)
            {
                _Animator.SetTrigger("OpenEye");

            }
        }

        /// <summary>
        /// This function is called by a trigger in the OpenEye animation.
        /// </summary>
        private void OnEyeOpened()
        {
            // Make the eye start tracking the player.
            _Eye.BeginTracking();

            _EyeFireVolleySize = BaseEyeBulletVolleySize;
            _FireDelay = BaseEyeFireDelay;
            _Health.IsVulnerable = true;
        }

        private void OnTakeDamage(object sender)
        {
            _Eye.DoDamagRecoil();

            // Decrease the fire delay and increase the volley size.
            _FireDelay += EyeFireRateStep;
            _EyeFireVolleySize += EyeBulletVolleySizeStep;            

            // This fixes a bug where if you hit the Cryoculus while it was in the pause between volleys, it would fire an extra shot and then do another wait period before firing another volley.
            // The reason is that the code above increased the volley size. This causes the if statement in Update() to see that the fire count is now less than volley size, so it thinks
            // it is in the middle of a volley and fires another round. The _FiringVolley flag and this if statement fix it.
            //if (_FiringVolley)
            //   _EyeFireCount++;
 
        }

        private void OnDeath(object sender)
        {
            _Health.OnTakeDamage -= OnTakeDamage;
            _Health.OnDeath -= OnDeath;

            Destroy(gameObject);
        }

        private void FireGuns()
        {
            foreach (Boss_Cryoculus_Arm arm in _Arms)
                arm.FireProjectile();

        }

        private void FireEyeProjectile()
        {
            Vector2 velocityVector = _Player.transform.position - transform.position;
            velocityVector.Normalize();

            float angle = Vector2.Angle(transform.position, _Player.transform.position);
            Quaternion rotation = new Quaternion(0, 0, angle, 1);
            
            GetBulletFromPool(transform.position, rotation, velocityVector * EyeBulletSpeed);

            
            _EyeFireCount++;
            if (_EyeFireCount >= _EyeFireVolleySize)
                _FiringVolley = false;

        }

        private void OnProjectileCollision(GameObject sender, Collision2D collidedWith)
        {
            if (_BulletPool.Contains(sender))
                return;


            sender.SetActive(false);

            sender.GetComponent<Projectile_Cryoculus>().OnCollision -= OnProjectileCollision;

            _BulletPool.Add(sender);
        }


    }

}
