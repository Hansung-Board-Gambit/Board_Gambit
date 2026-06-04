using UnityEngine;
using UnityEngine.Serialization;

public enum PlaceableSizeControlMode
{
    ManualFootprint = 0,
    ScaleMultiplier = 1
}

public enum PlaceableSpawnMode
{
    LocalInstantiate = 0,
    NetworkSpawn = 1
}

public class PlaceableObject : MonoBehaviour
{
    [Header("Footprint Size (X,Z)")]
    [Tooltip("Set one axis to 0 to infer it from the prefab renderer aspect ratio.")]
    public Vector2 footprint = Vector2.one;

    [Header("Size Control")]
    [Tooltip("ManualFootprint uses Footprint directly. ScaleMultiplier multiplies Footprint and visual size together.")]
    public PlaceableSizeControlMode sizeControlMode = PlaceableSizeControlMode.ManualFootprint;

    [Header("Footprint Scale")]
    [Tooltip("Only used in ScaleMultiplier mode. Multiplies occupied footprint and fitted visual size together.")]
    [FormerlySerializedAs("visualScaleMultiplier")]
    public float footprintScaleMultiplier = 1f;

    [Header("Preview Height")]
    public float yOffset = 0.5f;

    [Header("Surface Alignment")]
    [Tooltip("Applied after automatic board-surface alignment. Use a small negative value to sink a model into the board.")]
    public float surfaceOffset = 0f;

    [Header("Spawn")]
    public PlaceableSpawnMode spawnMode = PlaceableSpawnMode.LocalInstantiate;

    [Header("ID")]
    public string prefabId = "Object";
}
