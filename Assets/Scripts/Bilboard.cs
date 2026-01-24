using UnityEngine;

public class Billboard : MonoBehaviour
{
    void LateUpdate()
    {
        // Kameranın pozisyonuna değil, baktığı yöne odaklanıyoruz
        transform.LookAt(transform.position + Camera.main.transform.forward);
    }
}
