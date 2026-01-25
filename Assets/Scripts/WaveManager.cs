using UnityEngine;

public class WaveManager : MonoBehaviour
{
    public int firstWave = 2;
    public int enemyToAddByWave = 1;
    public GameObject enemyPrefab;
    //baska enemy type olacaksa da bu şekilde atansin sonra kod icerisinde hangi prefabdan kac tane basacağını yazariz
    public float spawnRange;
    public static int activeEnemyCount = 0;
    public bool isAllEnemiesDefeated;

    private PowerUpManager powerUpManager;
    void Start()
    {
        activeEnemyCount = 0;
        powerUpManager = GameObject.Find("Power Up Manager").GetComponent<PowerUpManager>();
    }

void SpawnEnemies()
    {
        if (GameManager.isGameActive == true)
        {
            for (int i = 0; i < firstWave; i++)
            {
                Instantiate(enemyPrefab, GenerateRandomPositions(spawnRange), enemyPrefab.transform.rotation);
                activeEnemyCount++;
            }
            firstWave += enemyToAddByWave;
        }
    }

    public Vector3 GenerateRandomPositions(float spawnRange)
    {
        float xRange = Random.Range(-spawnRange, spawnRange);
        float zRange = Random.Range(-spawnRange, spawnRange);
        float yRange = 1;

        Vector3 pos = new(xRange, yRange, zRange);

        return pos;
    }

    void Update()
    {
        if (GameManager.isGameActive == false) return;

        else
        {
            if (activeEnemyCount == 0)
            {
                SpawnEnemies();
                powerUpManager.SpawnPowerUp();
            }
        }
    }
}
