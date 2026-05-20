using Fusion;
using Fusion.LagCompensation;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class TimerTrap : WeaponBase
{
    [Header("타이머 세팅")]
    [SerializeField] GameObject timerPrefab;
    [SerializeField] float stunRadius = 5f; //음파 범위
    [SerializeField] float stunDuration = 1f; // 지속시간
    [SerializeField] float extraSpawnPos = -1.0f;

    [Networked] public NetworkObject DeployedTimer {  get; set; }
    [Networked] public NetworkBool IsDeployed { get; set; } //설치됐는지를 확인하는 변수
    protected override void BasicAttack()
    {
        if(!IsDeployed && HasStateAuthority)
        {
            if (myPlayer == null) return;

            Vector3 spawnPos = myPlayer.transform.position + new Vector3(0,extraSpawnPos,0); //약간 아래로 배치(추후 조정)

            DeployedTimer = Runner.Spawn(timerPrefab, spawnPos, Quaternion.identity, Object.InputAuthority);
            IsDeployed = true;

        }
    }

    protected override void SecondAttack()
    {
       if(IsDeployed && HasStateAuthority)
       {
            if(DeployedTimer != null)
            {
                Runner.Despawn(DeployedTimer); //있으면 삭제
            }
            DeployedTimer = null;
            IsDeployed=false;
       }
    }

    protected override void SkillQ()
    {
        if(IsDeployed && DeployedTimer != null && HasStateAuthority)
        {
            ExplodeStunWave();
        }
    }

    private void ExplodeStunWave()
    {
        Vector3 center = DeployedTimer.transform.position;
        List<LagCompensatedHit> hits = new List<LagCompensatedHit>();

        int count = Runner.LagCompensation.OverlapSphere(center, stunRadius, Object.InputAuthority,
            hits, LayerMask.GetMask("Player"));

        HashSet<GameObject> hitPlayers = new HashSet<GameObject>();

        if(count>0)
        {
            foreach (var hit in hits)
            {
                GameObject target = hit.Hitbox.Root.gameObject;

                if (target == myPlayer.gameObject) continue; //본인은 피해입지 않도록
                 
                if(hitPlayers.Contains(target)) continue; //중복 피격 방지

                PlayerHealth targetHealth = target.GetComponent<PlayerHealth>();
                if(targetHealth != null)
                {
                    targetHealth.RPC_TakeStun(stunDuration);
                    hitPlayers.Add(target);
                }
            }

        }
        Runner.Despawn(DeployedTimer);
        DeployedTimer = null;
        IsDeployed = false; 
        //나중에 스킬을 쓰고 다시 사용하려면 쿨타임 추가기능
    }
}
