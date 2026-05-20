using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PlacedObjectData
{
    public string prefabId;
    public Vector3 position;
    public Quaternion rotation;
}

[System.Serializable]
public class SpawnPointData
{
    public bool hasMySpawn;
    public bool hasOpponentSpawn;
    public Vector3 mySpawnPosition;
    public Vector3 opponentSpawnPosition;
}

public class PrepDataStore : MonoBehaviour
{
    [Header("Placement Points")]
    public int maxPoints = 5;
    public int remainingPoints;

    [Header("Saved Data")]
    public List<PlacedObjectData> placedObjects = new List<PlacedObjectData>();
    public SpawnPointData spawnData = new SpawnPointData();

    private void Awake()
    {
        remainingPoints = maxPoints;
    }

    public void ResetStore()
    {
        remainingPoints = maxPoints;
        placedObjects.Clear();
        spawnData = new SpawnPointData();
    }

    public bool CanSpendPoint()
    {
        return remainingPoints > 0;
    }

    public void SpendPoint()
    {
        remainingPoints = Mathf.Max(0, remainingPoints - 1);
    }

    public void SavePlacedObject(string prefabId, Vector3 position, Quaternion rotation)
    {
        placedObjects.Add(new PlacedObjectData
        {
            prefabId = prefabId,
            position = position,
            rotation = rotation
        });
    }

    public void SaveMySpawn(Vector3 position)
    {
        spawnData.hasMySpawn = true;
        spawnData.mySpawnPosition = position;
    }

    public void SaveOpponentSpawn(Vector3 position)
    {
        spawnData.hasOpponentSpawn = true;
        spawnData.opponentSpawnPosition = position;
    }
}