using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class FlameTrap : NetworkBehaviour, INetworkPlacedObject
{
    [Header("Flame Trap Settings")]
    [SerializeField] int tickDamage = 5;
    [SerializeField] float damageInterval = 0.5f;
    [SerializeField] LayerMask playerLayer;
    [SerializeField] Collider damageTrigger;
    [SerializeField] ParticleSystem[] flameParticles;

    [Networked, OnChangedRender(nameof(OnPlacementChanged))]
    public NetworkBool PlacementInitialized { get; set; }

    [Networked, OnChangedRender(nameof(OnPlacementChanged))]
    public Vector3 PlacementPosition { get; set; }

    [Networked, OnChangedRender(nameof(OnPlacementChanged))]
    public Quaternion PlacementRotation { get; set; }

    private readonly Dictionary<PlayerHealth, TickTimer> damageCooldowns = new Dictionary<PlayerHealth, TickTimer>();
    private GameRoundFlowController roundFlow;

    public override void Spawned()
    {
        if (HasStateAuthority && !PlacementInitialized)
            InitializeNetworkPlacement(transform.position, transform.rotation);

        ApplyPlacement();
        ConfigureDamageTrigger();
        PlayParticles();
    }

    public void InitializeNetworkPlacement(Vector3 position, Quaternion rotation)
    {
        if (!HasStateAuthority)
            return;

        PlacementPosition = position;
        PlacementRotation = rotation;
        PlacementInitialized = true;
        transform.SetPositionAndRotation(position, rotation);
        ConfigureDamageTrigger();
    }

    public void ResetForPreparationPhase()
    {
        ApplyPlacement();
        ConfigureDamageTrigger();
        PlayParticles();
        damageCooldowns.Clear();
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

    private void OnTriggerStay(Collider other)
    {
        if (!HasStateAuthority)
            return;

        if (!IsBattlePhase())
        {
            return;
        }

        if ((playerLayer.value & (1 << other.gameObject.layer)) == 0)
            return;

        PlayerHealth health = other.GetComponentInParent<PlayerHealth>();
        if (health == null || health.CurrentHP <= 0)
            return;

        health.SetFlameHit();

        if (damageCooldowns.TryGetValue(health, out TickTimer cooldown) && !cooldown.ExpiredOrNotRunning(Runner))
            return;

        damageCooldowns[health] = TickTimer.CreateFromSeconds(Runner, damageInterval);
        health.RPC_TakeDamage(tickDamage, "Flame Trap", Object.InputAuthority);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!HasStateAuthority)
            return;

        PlayerHealth health = other.GetComponentInParent<PlayerHealth>();
    }

    private void ConfigureDamageTrigger()
    {
        if (damageTrigger == null)
            damageTrigger = GetComponent<Collider>();

        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        foreach (Collider col in colliders)
        {
            if (col == damageTrigger)
            {
                col.isTrigger = true;
                col.enabled = true;
                continue;
            }

            col.enabled = false;
        }
    }

    private void PlayParticles()
    {
        if (flameParticles == null)
            return;

        for (int i = 0; i < flameParticles.Length; i++)
        {
            ParticleSystem particle = flameParticles[i];
            if (particle == null)
                continue;

            if (!particle.isPlaying)
                particle.Play(true);
        }
    }

    private bool IsBattlePhase()
    {
        if (roundFlow == null)
            roundFlow = FindFirstObjectByType<GameRoundFlowController>();

        return roundFlow == null || roundFlow.CurrentPhase == GameRoundPhase.Battle;
    }
}
