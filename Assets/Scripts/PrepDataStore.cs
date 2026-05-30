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
    public int selectedEquipmentIndex = -1;

    private void Awake()
    {
        remainingPoints = maxPoints;
    }

    public void ResetStore()
    {
        remainingPoints = maxPoints;
        placedObjects.Clear();
        spawnData = new SpawnPointData();
        selectedEquipmentIndex = -1;
    }

    public void ResetRoundPlacementPoints()
    {
        remainingPoints = maxPoints;
    }

    public bool CanSpendPoint()
    {
        return remainingPoints > 0;
    }

    public void SpendPoint()
    {
        remainingPoints = Mathf.Max(0, remainingPoints - 1);
    }

    public void RefundPoint()
    {
        remainingPoints = Mathf.Min(maxPoints, remainingPoints + 1);
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

    public bool RemovePlacedObject(Vector3 position, float tolerance = 0.2f)
    {
        if (placedObjects == null || placedObjects.Count == 0)
            return false;

        float toleranceSqr = Mathf.Max(0.001f, tolerance) * Mathf.Max(0.001f, tolerance);
        int closestIndex = -1;
        float closestDistanceSqr = float.PositiveInfinity;

        for (int i = 0; i < placedObjects.Count; i++)
        {
            float distanceSqr = (placedObjects[i].position - position).sqrMagnitude;
            if (distanceSqr > toleranceSqr || distanceSqr >= closestDistanceSqr)
                continue;

            closestIndex = i;
            closestDistanceSqr = distanceSqr;
        }

        if (closestIndex < 0)
            return false;

        placedObjects.RemoveAt(closestIndex);
        return true;
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

    public void SaveEquipmentSelection(int equipmentIndex)
    {
        selectedEquipmentIndex = equipmentIndex;
    }
}
