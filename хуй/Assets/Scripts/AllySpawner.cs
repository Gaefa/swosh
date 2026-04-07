using UnityEngine;

public class AllySpawner : MonoBehaviour
{
    public GameObject allyPrefab;
    public Transform allySpawnPoint;

    private GameObject allyInstance;

    public void SpawnOrReset()
    {
        if (allyPrefab == null || allySpawnPoint == null)
        {
            Debug.LogWarning("AllySpawner: allyPrefab or allySpawnPoint is not set.");
            return;
        }

        if (allyInstance == null)
        {
            allyInstance = Instantiate(allyPrefab, allySpawnPoint.position, allySpawnPoint.rotation);
        }
        else
        {
            allyInstance.transform.SetPositionAndRotation(allySpawnPoint.position, allySpawnPoint.rotation);
        }
    }

    public bool IsAlive() => allyInstance != null;
}