using UnityEngine;
using System.Collections.Generic;

public class HealingAura : MonoBehaviour
{
    public int healAmount = 1;
    public float healTickRate = 1.0f; // Kaç saniyede bir can verecek?

    // İçerideki düşmanları ve her birinin en son ne zaman can aldığını takip eder
    private Dictionary<EnemyBase, float> nextHealTimes = new Dictionary<EnemyBase, float>();

    private void OnTriggerStay(Collider other)
    {
        // Sadece Enemy tag'li objelerle ilgileniyoruz
        if (other.CompareTag("Enemy"))
        {
            EnemyBase enemy = other.GetComponent<EnemyBase>();
            if (enemy == null) enemy = other.GetComponentInParent<EnemyBase>();

            if (enemy != null && enemy.health < enemy.maxHealth && enemy.health > 0)
            {
                // Eğer bu düşman listede yoksa veya tick süresi dolmuşsa iyileştir
                if (!nextHealTimes.ContainsKey(enemy) || Time.time >= nextHealTimes[enemy])
                {
                    enemy.Heal(healAmount);
                    // Bu düşman için bir sonraki "tick" zamanını belirle
                    nextHealTimes[enemy] = Time.time + healTickRate;
                }
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Çıkan düşmanı listeden temizle (tekrar girerse anında ilk tick'i alabilsin diye)
        if (other.CompareTag("Enemy"))
        {
            EnemyBase enemy = other.GetComponent<EnemyBase>();
            if (enemy != null && nextHealTimes.ContainsKey(enemy))
            {
                nextHealTimes.Remove(enemy);
            }
        }
    }
}