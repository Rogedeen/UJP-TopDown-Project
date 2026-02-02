using UnityEngine;
using System.Collections.Generic;

public class PowerUpManager : MonoBehaviour
{
    public int firstWavePowerUpCount;
    public int powerUpToAddByWave;
    public float spawnRange;
    public List<GameObject> powerUpTypes;

    public void SpawnPowerUp()
    {
            for (int i = 0; i < firstWavePowerUpCount; i++)
            {
                int randIndex = Random.Range(0, powerUpTypes.Count);
                Vector3 randPos = GenerateRandomPositions(spawnRange);
                Instantiate(powerUpTypes[randIndex], randPos, powerUpTypes[randIndex].transform.rotation);
                
            }
            firstWavePowerUpCount += powerUpToAddByWave;
    }

    Vector3 GenerateRandomPositions(float spawnRange)
    {
        float xRange = Random.Range(-spawnRange, spawnRange);
        float zRange = Random.Range(-spawnRange, spawnRange);
        float yRange = 1;

        Vector3 pos = new(xRange, yRange, zRange);

        return pos;
    }
}
