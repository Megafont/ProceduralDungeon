using System.Collections;
using System.Collections.Generic;

using UnityEngine;


namespace ProceduralDungeon.InGame.Bosses
{

    public class Boss_Cryoculus_Eye : MonoBehaviour
    {
        private bool _IsTrackingPlayer = false;


        private Animator _Animator;

        private GameObject _Player;



        // Start is called before the first frame update
        void Start()
        {
            _Animator = GetComponent<Animator>();

            _Player = GameObject.Find("Player");
        }

        // Update is called once per frame
        void Update()
        {
            if (!_IsTrackingPlayer)
                return;


            Vector2 playerDir = _Player.transform.position - transform.position;
            playerDir.Normalize();

            _Animator.SetFloat("X", playerDir.x);
            _Animator.SetFloat("Y", playerDir.y);
        }


        public void BeginTracking()
        {
            _Animator.SetTrigger("TrackPlayer");

            _IsTrackingPlayer = true;
        }

        public void DoDamagRecoil()
        {
            _Animator.SetTrigger("TakeDamage");
        }
        

    }

}
