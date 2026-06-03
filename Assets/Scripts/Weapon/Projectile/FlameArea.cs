using Fusion;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class FlameArea : NetworkBehaviour
{
    [Header("사운드")]
    [SerializeField] AudioSource audioSource;
    [SerializeField] AudioClip flameAreaSfx;

    [Header("장판 설정")]
    [SerializeField] float radius = 3f;
    [SerializeField] float lifeTime = 5f;
    [SerializeField] float damageInterval = 0.05f;
    [SerializeField] int tickDamage = 3;
    //[SerializeField] ParticleSystem flameParticle;
    [Networked] public TickTimer LifeTimer { get; set; }
    [Networked] public TickTimer DamageTimer { get; set; }
    [Networked] public PlayerRef Shooter { get; set; }

    public override void Spawned()
    {
        transform.localScale = new Vector3(radius * 2f, 0.05f, radius * 2f);
        //if(flameParticle != null) { var shape = flameParticle.shape; shape.radius = this.radius } 

        if (audioSource != null && flameAreaSfx != null)
        {
            audioSource.PlayOneShot(flameAreaSfx);
        }

        if (HasStateAuthority)
        {
            Shooter = Object.InputAuthority;
            LifeTimer = TickTimer.CreateFromSeconds(Runner, lifeTime);
            DamageTimer = TickTimer.CreateFromSeconds(Runner, damageInterval);
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        if(LifeTimer.Expired(Runner))
        {
            Runner.Despawn(Object);
            return;
        }

        if(DamageTimer.Expired(Runner))
        {
            BurnEnemies();
            DamageTimer = TickTimer.CreateFromSeconds(Runner, damageInterval);
        }
    }

    private void BurnEnemies()
    {
        List<LagCompensatedHit> hits = new List<LagCompensatedHit>();
        int layerMask = LayerMask.GetMask("Player", "Default", "PlacedObject");
        int count = Runner.LagCompensation.OverlapSphere(transform.position, radius, Shooter, 
            hits, layerMask);
        //파티클로 할땐
        //Vector3 extents = new Vector3(radius, 10f, radius);
        //int count = Runner.LagCompensation.OverlapBox 로 교체

        HashSet<GameObject> burnedPlayers = new HashSet<GameObject>();

        if(count > 0)
        {
            foreach(var p in hits)
            {
                GameObject target = p.Hitbox.Root.gameObject;

                //부위별로 여러번 데미지가 들어가는것 방지
                if (burnedPlayers.Contains(target)) continue;

                PlayerHealth health = target.GetComponent<PlayerHealth>();
                if(health != null)
                {
                    health.RPC_TakeDamage(tickDamage, "화염 장판");
                    burnedPlayers.Add(target);
                }
                target.GetComponent<ExplosiveBarrel>()?.RPC_TakeDamageBarrel(tickDamage, gameObject.name);
            }
        }
    }
}
