using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;


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


    private void Start()
    {
        _Health = _MaxHealth;        
    }



    public void DealDamage(float damageAmount)
    {
        _Health = Mathf.Max(0, _Health - damageAmount);
        Debug.Log("Player Damaged: " + _Health);
        UpdateHealthBar();

        OnTakeDamage?.Invoke(this); // Fire the OnTakeDamage event.

        if (_Health <= 0)
            OnDeath?.Invoke(this); // Fire the OnDeath event.
    }

    public void Heal(float healAmount)
    {
        _Health = Mathf.Min(_MaxHealth, _Health + healAmount);
        Debug.Log("Player Healed: " + _Health);

        UpdateHealthBar();

        OnHeal?.Invoke(this); // Fire the OnHeal event.
    }

    private void UpdateHealthBar()
    {
        HealthBar.fillAmount = _Health / _MaxHealth;
    }

}
