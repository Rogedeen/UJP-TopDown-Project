using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Projedeki Instantiate ve Destroy işlemlerinin getirdiği ağır işlem yükünü (ve GC spike'ları)
/// engellemek için kurulan temel Obje Havuzu (Object Pool) sistemi.
/// </summary>
public class PoolManager : MonoBehaviour
{
    public static PoolManager Instance;

    // Her prefab'ın ismi anahtar (Key) olacak şekilde havuzları Tutan Dictionary
    private Dictionary<string, Queue<GameObject>> poolDictionary = new Dictionary<string, Queue<GameObject>>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Instantiate yerine bunu kullanın.
    /// </summary>
    public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        string key = prefab.name;

        if (!poolDictionary.ContainsKey(key))
        {
            poolDictionary.Add(key, new Queue<GameObject>());
        }

        if (poolDictionary[key].Count > 0)
        {
            GameObject objToSpawn = poolDictionary[key].Dequeue();
            objToSpawn.transform.position = position;
            objToSpawn.transform.rotation = rotation;
            objToSpawn.SetActive(true);
            return objToSpawn;
        }

        // Havuz boşsa yeni yarat ve isminin "(Clone)" eklentisi almasını engelle
        GameObject newObj = Instantiate(prefab, position, rotation);
        newObj.name = prefab.name; 
        return newObj;
    }

    /// <summary>
    /// Destroy yerine bunu kullanın.
    /// </summary>
    public void Despawn(GameObject obj)
    {
        obj.SetActive(false);
        string key = obj.name;

        if (!poolDictionary.ContainsKey(key))
        {
            poolDictionary.Add(key, new Queue<GameObject>());
        }

        poolDictionary[key].Enqueue(obj);
    }
}
