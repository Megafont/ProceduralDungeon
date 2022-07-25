using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using ProceduralDungeon.InGame;
using ProceduralDungeon.Utilities;


namespace ProceduralDungeon.InGame.Bosses
{
    /// <summary>
    /// Represents one of Cryoculus' arms.
    /// </summary>
    /// <remarks>
    /// NOTE: The visual aspect of this GameObject is in a child object for the purposes of rotation.
    ///       The main arm GameObject is positioned at the center of the boss' body.
    ///       The child object is offset so it will rotate properly around that center point.
    /// </remarks>
    public class Boss_Cryoculus_Arm : MonoBehaviour
    {
        public float BulletSpeed = 3f;

        public Boss_Cryoculus ParentCryoculus;


        private SpriteRenderer _SpriteRenderer;


        // Start is called before the first frame update
        void Start()
        {
            _SpriteRenderer = GetComponent<SpriteRenderer>();

            GetComponent<Health>().OnDeath += OnDeath;
        }



        public void FireProjectile()
        {
            Quaternion rotation = transform.parent.rotation;
            Vector3 velocityVector = rotation * Vector3.right;

            StartCoroutine(DoGunRecoil());

            // The Cryoculus arm sub object that this script resides on has only one child, which is the spawn point for bullets.
            // So grab the spawn position from that object.
            ParentCryoculus.GetBulletFromPool(transform.GetChild(0).position, rotation, velocityVector * BulletSpeed);
        }


        private void OnDeath(object sender)
        {
            GetComponent<Health>().OnDeath -= OnDeath;

            Destroy(transform.parent.gameObject);
        }

        private IEnumerator DoGunRecoil()
        {           
            _SpriteRenderer.sprite = SpriteManager.GetSpriteFromSheet("Boss_Cryoculus", "Cryoculus_Gun_2", ParentCryoculus.GetParentRoom().RoomBlueprint.RoomSet);
            yield return new WaitForSeconds(0.15f);

            _SpriteRenderer.sprite = SpriteManager.GetSpriteFromSheet("Boss_Cryoculus", "Cryoculus_Gun_1", ParentCryoculus.GetParentRoom().RoomBlueprint.RoomSet);
        }


    }

}
