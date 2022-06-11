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
        Spikes,
    }

    public class Health : MonoBehaviour
    {
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

        private Dictionary<DamageTypes, float> _LastDamageTimes;


        private void Start()
        {
            _Health = _MaxHealth;

            // Create damage times dictionary to track the last time damage of each type was received.
            _LastDamageTimes = new Dictionary<DamageTypes, float>();

            // Add an entry for each damage type.
            foreach (DamageTypes type in Enum.GetValues(typeof(DamageTypes)))
                _LastDamageTimes.Add(type, 0);
        }



        public void DealDamage(float damageAmount, DamageTypes damageType)
        {
            // If the player already took spike damage in the last second, then ignore this damage event.
            // This is to prevent the player from taking double damage if standing on the edges of two
            // spike tiles at the same time.
            if (damageType == DamageTypes.Spikes &&
                Time.time - _LastDamageTimes[damageType] < 1.0f)
            {
                return;
            }


            _Health = Mathf.Max(0, _Health - damageAmount);
            _LastDamageTimes[damageType] = Time.time;

            UpdateHealthBar();

            OnTakeDamage?.Invoke(this); // Fire the OnTakeDamage event.

            if (_Health <= 0)
                OnDeath?.Invoke(this); // Fire the OnDeath event.
        }

        public void Heal(float healAmount)
        {
            _Health = Mathf.Min(_MaxHealth, _Health + healAmount);

            UpdateHealthBar();

            OnHeal?.Invoke(this); // Fire the OnHeal event.
        }

        private void UpdateHealthBar()
        {
            HealthBar.fillAmount = _Health / _MaxHealth;
        }

    }

}
