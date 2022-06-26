using System.Collections;
using System.Collections.Generic;

using UnityEngine;


namespace ProceduralDungeon.InGame
{
    public enum WeaponTypes
    {
        Sword
    }



    public class Combat : MonoBehaviour
    {
        public WeaponTypes _WeaponType;

        private SpriteRenderer _MySpriteRenderer;
        private Animator _MyWeaponAnimator;
        
        string _LastAttack = "";

        bool _IsAttacking = false;




        // Start is called before the first frame update
        void Start()
        {
            _MySpriteRenderer = GetComponent<SpriteRenderer>();
            _MyWeaponAnimator = GetComponent<Animator>();

            _WeaponType = WeaponTypes.Sword;
        }

        // Update is called once per frame
        void Update()
        {
            UpdateAnimationController();

        }


        public void UpdateAnimationController()
        {
            if (_WeaponType == WeaponTypes.Sword)
                _MyWeaponAnimator.SetBool("IsSwordEquipped", true);
            else
                _MyWeaponAnimator.SetBool("IsSwordEquipped", false);

        }

        public void DoAttack(Vector3 moveInput)
        {
            if (_IsAttacking)
                return;


            _MyWeaponAnimator.SetFloat("X Movement", moveInput.x);
            _MyWeaponAnimator.SetFloat("Y Movement", moveInput.y);


            if (_WeaponType == WeaponTypes.Sword)
            {
                _LastAttack = "DoSwordStab";

                _MyWeaponAnimator.SetTrigger(_LastAttack);

                _IsAttacking = true;
            }


            _MySpriteRenderer.enabled = _IsAttacking;

        }


        public void OnAttackFinished()
        {
            _MySpriteRenderer.enabled = false;

            _MyWeaponAnimator.ResetTrigger(_LastAttack);

            _IsAttacking = false;
        }


        public void OnPlayerMoved(Vector2 moveInput)
        {
            _MyWeaponAnimator.SetFloat("X Movement", moveInput.x);
            _MyWeaponAnimator.SetFloat("Y Movement", moveInput.y);


            // Check whether the weapon should be in front of or behind the player. The player's sortingLayer property is set to 1.
            if (moveInput == Vector2.down || moveInput == Vector2.right)
                _MySpriteRenderer.sortingOrder = 2;
            else
                _MySpriteRenderer.sortingOrder = 0;

        }

    }


}
