using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public partial class PlayerController : MonoBehaviour
{
    void OnAttack(InputAction.CallbackContext ctx)
    {
        if (!GameManager.isGameActive) return;
        
        HandleLook();

        if (!animator.GetBool(IsAttackingHash) && comboStep < maxCombo)
        {
            StartCoroutine(AttackRoutine());
        }
    }

    IEnumerator AttackRoutine()
    {
        lastAttackTime = Time.time;
        comboStep++;
        
        animator.SetInteger(ComboStepHash, comboStep);
        animator.SetBool(IsAttackingHash, true);

        int attackLayerIndex = animator.GetLayerIndex("Attack Layer");
        if (attackLayerIndex >= 0)
        {
            yield return null;
            animator.Play("Attack" + comboStep, attackLayerIndex, 0f);
        }

        if (whooshSounds.Length > 0)
        {
            int soundIndex = (comboStep - 1) % whooshSounds.Length;
            audioSource.PlayOneShot(whooshSounds[soundIndex]);
        }

        yield return new WaitForSeconds(0.20f);

        GameObject vfxToSpawn = (activeWeapon.damage >= damageUpgradeThreshold) ? fireVFXPrefab : windVFXPrefab;

        if (vfxToSpawn != null && vfxSpawnPoint != null)
        {
            GameObject vfx = Instantiate(vfxToSpawn, vfxSpawnPoint.position, vfxSpawnPoint.rotation, vfxSpawnPoint);
            
            if (comboSlashAngles.Length > 0)
            {
                int angleIndex = (comboStep - 1) % comboSlashAngles.Length;
                vfx.transform.localEulerAngles = comboSlashAngles[angleIndex];
            }

            Destroy(vfx, 1.5f);
        }

        List<Component> hitEnemiesInThisSwing = new();
        float timer = 0f;
        float attackDuration = 0.3f; 
        float currentKnockbackMultiplier = (comboStep == 4) ? heavyFinisherKnockbackMultiplier : 1f;

        while (timer < attackDuration)
        {
            timer += Time.deltaTime;
            Vector3 hitCenter = transform.position + transform.forward * hitOffset;
            Collider[] hitColliders = Physics.OverlapSphere(hitCenter, hitRadius);

            foreach (var col in hitColliders)
            {
                if (col.CompareTag("Enemy") && col.TryGetComponent<EnemyBase>(out var enemyBase))
                {
                    if (!hitEnemiesInThisSwing.Contains(enemyBase))
                    {
                        Vector3 directionToEnemy = col.transform.position - transform.position;
                        float distanceToEnemy = directionToEnemy.magnitude;

                        if (Physics.Raycast(transform.position + Vector3.up, directionToEnemy, out RaycastHit hit, distanceToEnemy))
                        {
                            if (hit.collider.CompareTag("Barrier"))
                            {
                                continue;
                            }
                        }
                        
                        enemyBase.TakeDamage(activeWeapon.damage, transform.position, currentKnockbackMultiplier);
                        hitEnemiesInThisSwing.Add(enemyBase);
                        
                        float shakeMag = (comboStep == 4) ? 0.3f : 0.1f;
                        float shakeDur = (comboStep == 4) ? 0.25f : 0.15f;
                        camScript.TriggerShake(shakeMag, shakeDur);
                    }
                }
                else if (col.CompareTag("Barrel") && col.TryGetComponent<ExplosiveBarrel>(out var barrel))
                {
                    if (!hitEnemiesInThisSwing.Contains(barrel))
                    {
                        barrel.TakeDamage(activeWeapon.damage, transform.position, currentKnockbackMultiplier);
                        hitEnemiesInThisSwing.Add(barrel);
                    }
                }
            }
            yield return null;
        }

        animator.SetBool(IsAttackingHash, false);
    }

    public void TakeDamageEffect()
    {
        if (animator != null)
        {
            animator.SetTrigger(TakeDamageHash);
        }

        if (camScript != null) camScript.TriggerShake(0.15f, 0.2f);
    }

    public IEnumerator ApplySlow(float slowModifier, float slowDuration)
    {
        if (activeSlowCoroutine != null)
        {
            StopCoroutine(activeSlowCoroutine);
            speed = originalSpeed;
        }

        speed = originalSpeed * slowModifier;

        yield return new WaitForSecondsRealtime(slowDuration);

        speed = originalSpeed;
        activeSlowCoroutine = null;
    }

    public void StartSlow(float slowModifier, float slowDuration)
    {
        if (activeSlowCoroutine != null)
        {
            StopCoroutine(activeSlowCoroutine);
        }
        activeSlowCoroutine = StartCoroutine(ApplySlow(slowModifier, slowDuration));
    }
}
