using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;


namespace ProceduralDungeon.InGame
{
    public enum DamageTypes
    {
        BombBlast,
        EnemyContact,
        Spikes,
        Weapon,
    }

    public class Health : MonoBehaviour
    {
        public float DamageRepeatDelay = 0.5f; // How long before damage of a given type can be receivd again.
        public bool FlashRedOnDamage = true;

        public delegate void OnDeathEventHandler(object sender);
        public delegate void OnHealEventHandler(object sender);
        public delegate void OnTakeDamageEventHandler(object sender);

        public event OnDeathEventHandler OnDeath;
        public event OnHealEventHandler OnHeal;
        public event OnTakeDamageEventHandler OnTakeDamage;


        [SerializeField]
        private Image HealthBar;

        [SerializeField]
        private float _MaxHealth;
        

        private float _Health;

        private SpriteRenderer _Renderer;

        private Dictionary<DamageTypes, float> _LastDamageTimes;



        private void Start()        
        {
            _Renderer = GetComponent<SpriteRenderer>();

            _Health = _MaxHealth;

            // Create damage times dictionary to track the last time damage of each type was received.
            _LastDamageTimes = new Dictionary<DamageTypes, float>();

            // Add an entry for each damage type.
            foreach (DamageTypes type in Enum.GetValues(typeof(DamageTypes)))
                _LastDamageTimes.Add(type, 0);
        }



        public void DealDamage(float damageAmount, DamageTypes damageType)
        {
            if (damageAmount < 0)
                throw new Exception("Health.DealDamage() - The damage amount cannot be negative!");


            // Don't allow damage of a certain type to hit again until DamageRepeatDelay amount of time has elapsed.
            if (Time.time - _LastDamageTimes[damageType] < DamageRepeatDelay)
            {
                return;
            }

            
            _Health = Mathf.Max(0, _Health - damageAmount);
            _LastDamageTimes[damageType] = Time.time;

            Debug.LogError($"{gameObject.name} took {damageAmount} poinnts of {damageType} damage!");

            UpdateHealthBar();

            OnTakeDamage?.Invoke(this); // Fire the OnTakeDamage event.

            if (FlashRedOnDamage)
                StartCoroutine(FlashRed()); // NOTE: This coroutine also invokes the OnDeath event.
            else 
                OnDeath?.Invoke(this); // Fire the OnDeath event.

        }

        public void Heal(float healAmount)
        {
            if (healAmount < 0)
                throw new Exception("Health.DealDamage() - The heal amount cannot be negative!");


            _Health = Mathf.Min(_MaxHealth, _Health + healAmount);

            UpdateHealthBar();

            OnHeal?.Invoke(this); // Fire the OnHeal event.
        }

        private void UpdateHealthBar()
        {
            // Do not attempt to display health if no display has been specified.
            if (HealthBar != null)
                HealthBar.fillAmount = _Health / _MaxHealth;
        }


        private IEnumerator FlashRed()
        {
            if (_Renderer != null)
            {
                _Renderer.color = Color.red;
                yield return new WaitForSeconds(0.05f);

                _Renderer.color = Color.white;
            }


            if (_Health <= 0)
            {
                OnDeath?.Invoke(this); // Fire the OnDeath event.
            }

        }


    }

}
