using System.Collections;
using UnityEngine;

public class FollowPlayer : MonoBehaviour
{
    public Transform player;
    public Vector3 offset;

    private Vector3 shakeOffset = Vector3.zero;
    void LateUpdate()
    {
        gameObject.transform.position = player.transform.position + offset + shakeOffset;
    }

    public void TriggerShake(float duration, float magnitude)
    {
        StartCoroutine(ShakeRoutine(duration, magnitude));
    }

    private IEnumerator ShakeRoutine(float duration, float magnitude)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (Time.timeScale > 0f)
            {
                float x = Random.Range(-1f, 1f) * magnitude;
                float y = Random.Range(-1f, 1f) * magnitude;
                shakeOffset = new Vector3(x, y, 0);
                elapsed += Time.deltaTime;
            }
            else
            {
                // Oyun durdurulduysa sallanmayı sıfırla
                shakeOffset = Vector3.zero;
            }

            yield return null; 
        }

        shakeOffset = Vector3.zero;
    }
}
