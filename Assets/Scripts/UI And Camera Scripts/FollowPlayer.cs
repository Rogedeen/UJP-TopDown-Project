using UnityEngine;

public class FollowPlayer : MonoBehaviour
{
    public Transform player;
    public Vector3 offset;

    void LateUpdate()
    {
        gameObject.transform.position = player.transform.position + offset;
    }
}
