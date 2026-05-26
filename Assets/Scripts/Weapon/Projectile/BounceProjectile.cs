using Fusion;
using NUnit.Framework;
using System.Collections.Generic;
using Unity.Jobs;
using UnityEngine;

public class BounceProjectile : NetworkBehaviour
{
    [SerializeField] float speed = 20f;
    [SerializeField] float explosionRadius = 3f;
    [SerializeField] RangedWeapon rangedData;
    public LayerMask hitLayer;

    [Networked] public int BounceCount { get; set; }
    [Networked] public Vector3 Velocity { get; set; }
    [Networked] public PlayerRef Shooter { get; set; }

    public void InitProjectile(Vector3 dir, int bounces, PlayerRef shooter)
    {
        //속도 계산
        Velocity = dir * speed;
        BounceCount = bounces;
        Shooter = shooter;
    }

    public override void FixedUpdateNetwork()
    {
        //포물선 계산
        Velocity += Physics.gravity * Runner.DeltaTime;

        //1틱에 얼마나 이동할지 거리 계산
        Vector3 displacement = Velocity * Runner.DeltaTime;

        //normalized : 날아갈 방향, magnitude : 이동할 거리
        if (Runner.LagCompensation.Raycast(transform.position, Velocity.normalized, displacement.magnitude, Shooter,
      out var hit, hitLayer, HitOptions.IncludePhysX))
        {
            if (hit.Hitbox != null)
            {
                NetworkObject hitObj = hit.Hitbox.Root.GetComponent<NetworkObject>();

                if (hitObj != null && hitObj.InputAuthority == Shooter)
                {

                }
                else
                {
                    Explode();
                    return;
                }
            }
            else if (hit.Collider != null)
            {
                NetworkObject colObj = hit.Collider.GetComponentInParent<NetworkObject>();
                if (colObj != null && colObj.InputAuthority == Shooter && colObj.GetComponent<PlayerHealth>() != null)
                {
                    // 내 캐릭터의 콜라이더라면 무시하고 통과!
                    transform.position += displacement;
                }
                else
                {
                    // 진짜 벽이나 바닥일 때만 튕김 처리
                    BounceCount--;
                    if (BounceCount < 0)
                    {
                        Explode();
                        return;
                    }
                    transform.position = hit.Point + (hit.Normal * 0.05f);
                    //입사각-반사각: 부딪힐때마다 힘을 잃어감(30%씩 잃어감), 튕겨나갈 방향 잡아줌
                    Velocity = Vector3.Reflect(Velocity, hit.Normal) * 0.7f;
                    return;
                } //튕길때마다 살짝 느려지게
            }
        }
        //방향이 바뀌었다면 계산해둔 거리만큼 이동
        transform.position += displacement;
    }

    private void Explode()
    {
        if (!HasStateAuthority) return;

        Debug.Log("투사체가 터졌습니다");

        List<LagCompensatedHit> hits = new List<LagCompensatedHit>();
        int count = Runner.LagCompensation.OverlapSphere(transform.position,
          explosionRadius, Shooter, hits, LayerMask.GetMask("Player"));

        //데미지가 겹쳐서 들어오는걸 막기 위한 방명록
        HashSet<GameObject> hitPlayers = new HashSet<GameObject>();

        if (count > 0)
        {
            foreach (var p in hits)
            {
                GameObject target = p.Hitbox.Root.gameObject;
                if (hitPlayers.Contains(target)) continue; //겹쳐서 두 번 맞는거 방지 

                PlayerHealth health = target.GetComponent<PlayerHealth>();
                if (health != null)
                {
                    health.RPC_TakeDamage(rangedData.damage, "바운스 건");
                    //중복 피격 막음
                    hitPlayers.Add(target);
                }
            }
        }

        Runner.Despawn(Object);
    }
}