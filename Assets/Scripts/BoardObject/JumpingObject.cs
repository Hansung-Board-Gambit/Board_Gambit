using Fusion;
using UnityEngine;

public class JumpingObject : NetworkBehaviour, INetworkPlacedObject
{
    [Header("점프힘 설정")]
    [SerializeField] float jumpForce = 20f;

    [Header("사운드")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip jumpPadSfx;

    [Networked, OnChangedRender(nameof(OnPlacementChanged))]
    public NetworkBool PlacementInitialized { get; set; }

    [Networked, OnChangedRender(nameof(OnPlacementChanged))]
    public Vector3 PlacementPosition { get; set; }

    [Networked, OnChangedRender(nameof(OnPlacementChanged))]
    public Quaternion PlacementRotation { get; set; }

    public override void Spawned()
    {
        if (HasStateAuthority && !PlacementInitialized)
            InitializeNetworkPlacement(transform.position, transform.rotation);

        ApplyPlacement();
        Debug.Log("JumpPad Spawned!");
    }

    public void InitializeNetworkPlacement(Vector3 position, Quaternion rotation)
    {
        if (!HasStateAuthority)
            return;

        PlacementPosition = position;
        PlacementRotation = rotation;
        PlacementInitialized = true;
        transform.SetPositionAndRotation(position, rotation);
    }

    public void ResetForPreparationPhase()
    {
        ApplyPlacement();
    }

    private void OnPlacementChanged()
    {
        ApplyPlacement();
    }

    private void ApplyPlacement()
    {
        if (Object == null || !Object.IsValid)
            return;

        if (!PlacementInitialized)
            return;

        transform.SetPositionAndRotation(PlacementPosition, PlacementRotation);
    }

    private void OnTriggerEnter(Collider other)
    {
        Player player = other.GetComponent<Player>();

        if (player != null)
        {
            player.ApplyJumpPadForce(jumpForce);
            RPC_PlayJumpPadSfx();
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayJumpPadSfx()
    {
        if (audioSource != null && jumpPadSfx != null)
            audioSource.PlayOneShot(jumpPadSfx);
    }
}