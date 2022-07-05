using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using ProceduralDungeon.InGame.Items;


namespace ProceduralDungeon.InGame
{
    public enum WeaponTypes
    {
        Sword
    }



    public class Combat : MonoBehaviour
    {
        public Player _Player;
        public WeaponTypes _WeaponType;

        private SpriteRenderer _SpriteRenderer;
        private Animator _WeaponAnimator;
        private Weapon _Weapon;

        string _LastAttack = "";

        bool _IsAttacking = false;




        // Start is called before the first frame update
        void Start()
        {
            _SpriteRenderer = GetComponent<SpriteRenderer>();
            _WeaponAnimator = GetComponent<Animator>();
            
            _Weapon = GetComponent<Weapon>();
            _Weapon.gameObject.SetActive(false); // Disable the weapon object. This way it is both invisible and disabled so if you move too close to an enemy, it won't deal erroneous damage to it.

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
                _WeaponAnimator.SetBool("IsSwordEquipped", true);
            else
                _WeaponAnimator.SetBool("IsSwordEquipped", false);

        }

        public void DoAttack(Vector3 moveInput)
        {
            if (_IsAttacking)
                return;

            // Enable the weapon gameobject. It is disabled while not attacking to stop it dealing erroneous damage when the player gets close to enemies.
            gameObject.SetActive(true);

            _WeaponAnimator.SetFloat("X Movement", moveInput.x);
            _WeaponAnimator.SetFloat("Y Movement", moveInput.y);


            if (_WeaponType == WeaponTypes.Sword)
            {
                _LastAttack = "DoSwordStab";

                _WeaponAnimator.SetTrigger(_LastAttack);

                _IsAttacking = true;
            }


            _SpriteRenderer.enabled = _IsAttacking;

        }


        public void OnAttackFinished()
        {
            _SpriteRenderer.enabled = false;

            _WeaponAnimator.ResetTrigger(_LastAttack);

            _IsAttacking = false;

            gameObject.SetActive(false);
        }


        public void OnPlayerMoved(Vector2 moveInput)
        {
            _WeaponAnimator.SetFloat("X Movement", moveInput.x);
            _WeaponAnimator.SetFloat("Y Movement", moveInput.y);


            // Check whether the weapon should be in front of or behind the player. The player's sortingLayer property is set to 1.
            if (moveInput == Vector2.down || moveInput == Vector2.right)
                _SpriteRenderer.sortingOrder = 2;
            else
                _SpriteRenderer.sortingOrder = 0;

        }



        public void EquipWeapon(ItemDataWithBuffs weapon)
        {
            _Weapon.WeaponItem = weapon;            
        }

        public ItemData GetWeaponItem()
        {
            return _Weapon.WeaponItem;
        }

    }


}
