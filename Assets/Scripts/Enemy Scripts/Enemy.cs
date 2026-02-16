using UnityEngine;

public class Enemy : EnemyBase
{
    protected override void Start()
    {
        base.Start();
        if (agent != null) agent.speed = 4f;
    }

    void Update()
    {
        if (player != null && !isKnockedBack && health > 0)
        {    
            agent.SetDestination(player.transform.position);

            animator.SetFloat("speed_f", agent.velocity.magnitude);

            if (agent.velocity.sqrMagnitude > 0.1f)
            {
                transform.rotation = Quaternion.LookRotation(agent.velocity.normalized);
            }
        }
    }
}