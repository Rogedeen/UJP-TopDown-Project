using UnityEngine;
using System.Collections.Generic;

public class PowerUpManager : MonoBehaviour
{
    public int firstWavePowerUpCount;
    public int powerUpToAddByWave;
    public float spawnRange;
    public List<GameObject> powerUpTypes;

    private WaveManager waveManager;


    void Start()
    {
        waveManager = GameObject.Find("WaveManager").GetComponent<WaveManager>();
    }

    void Update()
    {

    }
    public void SpawnPowerUp()
    {
            for (int i = 0; i < firstWavePowerUpCount; i++)
            {
                int randIndex = Random.Range(0, powerUpTypes.Count);
                Vector3 randPos = waveManager.GenerateRandomPositions(spawnRange);
                Instantiate(powerUpTypes[randIndex], randPos, powerUpTypes[randIndex].transform.rotation);
                
            }
            firstWavePowerUpCount += powerUpToAddByWave;
    }
}
