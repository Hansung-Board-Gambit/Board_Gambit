using UnityEngine;

public interface INetworkPlacedObject
{
    void InitializeNetworkPlacement(Vector3 position, Quaternion rotation);
    void ResetForPreparationPhase();
}
