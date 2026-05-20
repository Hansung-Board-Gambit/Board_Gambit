using UnityEngine;

public class PlaceableObject : MonoBehaviour
{
    [Header("Footprint Size (X,Z)")]
    public Vector2 footprint = Vector2.one;

    [Header("Preview Height")]
    public float yOffset = 0.5f;

    [Header("ID")]
    public string prefabId = "Object";
}