using UnityEngine;

public class PlaceableObject : MonoBehaviour
{
    [Header("Footprint Size (X,Z)")]
    [Tooltip("Set one axis to 0 to infer it from the prefab renderer aspect ratio.")]
    public Vector2 footprint = Vector2.one;

    [Header("Preview Height")]
    public float yOffset = 0.5f;

    [Header("ID")]
    public string prefabId = "Object";
}
