using Fusion;
using UnityEngine;

public class HealPack : NetworkBehaviour, INetworkPlacedObject
{
    [Header("힐팩 설정")]
    [SerializeField] int healAmount = 30;
    [SerializeField] float respawnTime = 5f;
    [SerializeField] LayerMask playerLayer;
    [SerializeField] float rotationSpeed = 120f;

    [Networked] public TickTimer RespawnTimer { get; set; }

    // 힐팩이 현재 맵에 존재하는지 여부
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
        if (!HasStateAuthority)
            return;

        IsActive = true;
        RespawnTimer = TickTimer.None;
        ApplyPlacement();
        OnActiveStateChanged();
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

        //부딪힌 오브젝트가 플레이어 레이어인지 확인
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
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
    }

    public void OnActiveStateChanged()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        foreach (var rend in renderers)
            rend.enabled = IsActive;
    }
}