using UnityEngine;

public class Enemy : EnemyBase
{
    public float speed = 4f;

    private GameObject player;

    protected override void Awake()
    {
        base.Awake();
    }

    protected override void Start()
    {
        base.Start();
        player = GameObject.FindGameObjectWithTag("Player");
    }

    void FixedUpdate()
    {
        if (player != null && !isKnockedBack && health > 0)
        {
            Vector3 dir = (player.transform.position - transform.position).normalized;
            dir.y = 0;

            enemyRb.linearVelocity = new(dir.x * speed,
                                          enemyRb.linearVelocity.y,
                                          dir.z * speed);

            transform.LookAt(player.transform.position);
            animator.SetFloat("speed_f", speed);
        }
    }
}
