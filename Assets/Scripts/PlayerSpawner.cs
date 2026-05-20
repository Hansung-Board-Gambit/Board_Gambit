using Fusion;
using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    public NetworkPrefabRef playerPrefab;

    public void SpawnPlayer(NetworkRunner runner, PlayerRef player)
    {
        Vector3 spawnPos = new Vector3(
            Random.Range(-3, 3),
            1,
            Random.Range(-3, 3)
        );

        runner.Spawn(
            playerPrefab,
            spawnPos,
            Quaternion.identity,
            player
        );
    }
}