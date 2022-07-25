using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;


namespace ProceduralDungeon.InGame
{
    public class Health : MonoBehaviour
    {
        [Tooltip("The amount of time that must elapse before this GameObject can take damage again.")]
        public float DamageRepeatDelay = 0.5f;

        [Tooltip("Specifies whether or not this GameObject is currently allowed to take damage.")]
        public bool IsVulnerable = true; // Controls whether the entity is currently vulnerable to damage or not.

        [Tooltip("Specifies whether or not this GameObject should flash upon taking damage.")]
        public bool FlashWhenDamaged = true;

        [Tooltip("The duration of a single flash of color when this GameObject flashes from taking damage.")]
        public float SingleDamageFlashTime = 0.05f;
        [Tooltip("The duration in between two flashes of color when this GameObject flashes from taking damage.")]
        public float SingleDamageFlashOffTime = 0.05f;
        [Tooltip("The total duration of time during which this GameObject will flash upon taking damage.")]
        public float DamageFlashingTotalDuration = 0.05f;
        [Tooltip("The color this GameObject will flash upon taking damage.")]
        public Color DamageFlashColor = Color.red;
        [Tooltip("A list of all child objects in this GameObject that need to flash when it takes damage.")]
        public List<SpriteRenderer> SubObjectsToFlashOnTakingDamage;



        public delegate void OnDeathEventHandler(GameObject sender);
        public delegate void OnHealEventHandler(GameObject sender);
        public delegate void OnTakeDamageEventHandler(GameObject sender);

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

            if (!IsVulnerable)
                return;


            // Don't allow damage of a certain type to hit again until DamageRepeatDelay amount of time has elapsed.
            if (Time.time - _LastDamageTimes[damageType] < DamageRepeatDelay)
            {
                return;
            }

            
            _Health = Mathf.Max(0, _Health - damageAmount);
            _LastDamageTimes[damageType] = Time.time;

            Debug.LogError($"{gameObject.name} took {damageAmount} points of {damageType} damage!");

            UpdateHealthBar();

            OnTakeDamage?.Invoke(gameObject); // Fire the OnTakeDamage event.

            if (FlashWhenDamaged)
                StartCoroutine(DoDamageFlash()); // NOTE: This coroutine also invokes the OnDeath event.
            else 
                OnDeath?.Invoke(gameObject); // Fire the OnDeath event.

        }

        public void Heal(float healAmount)
        {
            if (healAmount < 0)
                throw new Exception("Health.DealDamage() - The heal amount cannot be negative!");


            _Health = Mathf.Min(_MaxHealth, _Health + healAmount);

            //Debug.LogError($"{gameObject.name} was healed by {healAmount} points!");

            UpdateHealthBar();

            OnHeal?.Invoke(gameObject); // Fire the OnHeal event.
        }

        private void UpdateHealthBar()
        {
            // Do not attempt to display health if no display has been specified.
            if (HealthBar != null)
                HealthBar.fillAmount = _Health / _MaxHealth;
        }


        private IEnumerator DoDamageFlash()
        {
            if (_Renderer == null)
                yield return null;


            float elapsedTime = 0;
            while (elapsedTime < DamageFlashingTotalDuration)
            { 
                _Renderer.color = DamageFlashColor;
                SetSubObjectsTint(DamageFlashColor);
                yield return new WaitForSeconds(SingleDamageFlashTime);
                elapsedTime += SingleDamageFlashTime;

                _Renderer.color = Color.white;
                SetSubObjectsTint(Color.white);
                yield return new WaitForSeconds(SingleDamageFlashOffTime);
                elapsedTime += SingleDamageFlashOffTime;

            } // end while


            if (_Health <= 0)
                OnDeath?.Invoke(gameObject); // Fire the OnDeath event.

        }


        private void SetSubObjectsTint(Color color)
        {
            foreach (SpriteRenderer s in SubObjectsToFlashOnTakingDamage)
                s.color = color;
        }

    }

}
