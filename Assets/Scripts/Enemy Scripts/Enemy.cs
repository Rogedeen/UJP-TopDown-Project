using UnityEngine;

public class Enemy : EnemyBase
{
    private static readonly int SpeedHash = Animator.StringToHash("speed_f");

    protected override void Start()
    {
        base.Start();
        // Hız SADECE NavMeshAgent Inspector'ından ayarlanır, burada ezilmez!
    }

    void Update()
    {
        if (player != null && !isKnockedBack && health > 0)
        {    
            agent.SetDestination(player.transform.position);

            animator.SetFloat(SpeedHash, agent.velocity.magnitude);

            if (agent.velocity.sqrMagnitude > 0.1f)
            {
                transform.rotation = Quaternion.LookRotation(agent.velocity.normalized);
            }
        }
    }
}