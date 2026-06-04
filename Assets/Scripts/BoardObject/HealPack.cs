using Fusion;
using UnityEngine;

public class HealPack : NetworkBehaviour, INetworkPlacedObject
{
    [Header("Heal Pack Settings")]
    [SerializeField] int healAmount = 30;
    [SerializeField] float respawnTime = 5f;
    [SerializeField] LayerMask playerLayer;
    [SerializeField] float rotationSpeed = 120f;
    [SerializeField] Transform visualRoot;
    [SerializeField] Collider pickupCollider;

    [Networked] public TickTimer RespawnTimer { get; set; }

    // Whether the heal pack is currently available in battle.
    [Networked, OnChangedRender(nameof(OnActiveStateChanged))]
    public NetworkBool IsActive { get; set; }

    [Networked, OnChangedRender(nameof(OnPlacementChanged))]
    public NetworkBool PlacementInitialized { get; set; }

    [Networked, OnChangedRender(nameof(OnPlacementChanged))]
    public Vector3 PlacementPosition { get; set; }

    [Networked, OnChangedRender(nameof(OnPlacementChanged))]
    public Quaternion PlacementRotation { get; set; }

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            IsActive = true;
            RespawnTimer = TickTimer.None;
            if (!PlacementInitialized)
                InitializeNetworkPlacement(transform.position, transform.rotation);
        }

        ApplyPlacement();
        OnActiveStateChanged();
        DisableBlockingColliders();
    }

    public void InitializeNetworkPlacement(Vector3 position, Quaternion rotation)
    {
        if (!HasStateAuthority)
            return;

        PlacementPosition = position;
        PlacementRotation = rotation;
        PlacementInitialized = true;
        transform.SetPositionAndRotation(position, rotation);
        DisableBlockingColliders();
        OnActiveStateChanged();
    }

    public void ResetForPreparationPhase()
    {
        if (!HasStateAuthority)
            return;

        IsActive = true;
        RespawnTimer = TickTimer.None;
        ApplyPlacement();
        OnActiveStateChanged();
        DisableBlockingColliders();
    }

    private void OnPlacementChanged()
    {
        ApplyPlacement();
    }

    private void ApplyPlacement()
    {
        if (!PlacementInitialized)
            return;

        transform.SetPositionAndRotation(PlacementPosition, PlacementRotation);
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        if (!IsActive && RespawnTimer.Expired(Runner))
        {
            IsActive = true;
            RespawnTimer = TickTimer.None;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (!HasStateAuthority || !IsActive) return;

        // Check whether the touched object is on the player layer.
        if ((playerLayer.value & (1 << other.gameObject.layer)) > 0)
        {
            PlayerHealth ph = other.GetComponentInParent<PlayerHealth>();

            if (ph != null && ph.CurrentHP > 0 && ph.CurrentHP < ph.maxHP)
            {
                ph.RPC_Heal(healAmount);
                IsActive = false;
                RespawnTimer = TickTimer.CreateFromSeconds(Runner, respawnTime);
            }
        }
    }

    public override void Render()
    {
        if (IsActive)
            GetVisualRoot().Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
    }

    public void OnActiveStateChanged()
    {
        Transform targetRoot = GetVisualRoot();

        Renderer[] renderers = targetRoot.GetComponentsInChildren<Renderer>(true);
        foreach (var rend in renderers)
            rend.enabled = IsActive;

        DisableBlockingColliders();
    }

    private void DisableBlockingColliders()
    {
        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        foreach (var col in colliders)
        {
            if (col == pickupCollider)
            {
                col.isTrigger = true;
                col.enabled = IsActive;
                continue;
            }

            col.enabled = false;
        }
    }

    private Transform GetVisualRoot()
    {
        return visualRoot != null ? visualRoot : transform;
    }
}
