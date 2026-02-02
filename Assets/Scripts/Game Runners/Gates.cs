using UnityEngine;

public class Gates : MonoBehaviour
{
    public bool isActive = true;
    public Transform snapPoint;
    private WaveManager waveManager;

    public void Start()
    {
        waveManager = FindAnyObjectByType<WaveManager>();
    }
    private void OnTriggerEnter(Collider other)
    {
        if (isActive && other.CompareTag("Barrier"))
        {
            isActive = false;
            Debug.Log(gameObject.name + " kapandı!");

            // Fizikleri kapatıyoruz
            Rigidbody barrierRb = other.GetComponent<Rigidbody>();
            barrierRb.isKinematic = true;

            // Barikatı tam olarak SnapPoint'in pozisyonuna ve rotasyonuna kilitliyoruz
            if (snapPoint != null)
            {
                other.transform.SetPositionAndRotation(snapPoint.position, snapPoint.rotation);
            }

            waveManager.CheckForVictory();
        }
    }
}