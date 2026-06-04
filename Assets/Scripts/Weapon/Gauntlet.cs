using Fusion;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class Gauntlet : WeaponBase
{
    private MeleeWeapon meleeWeapon;

    [Header("기본공격")]
    [SerializeField] LayerMask targetLayer;
    [SerializeField] float punchWidth = 2f;
    [Header("충격파 설정")]
    [SerializeField] float hitRange = 8f;  //공격 사거리 : 이 거리 안에 있어야 충격파 맞음
    [SerializeField] float shockwaveRaius = 4f; //충격파 범위 : 두께
    [SerializeField] float knockbackDistance = 8f; //넉백 거리 : 날라갈 거리
    [SerializeField] float shockPower = 20f;
    [Header("데미지 설정")]
    [SerializeField] int shockwaveBaseDamage = 0;
    [SerializeField] int wallDamage = 50;
    [SerializeField] LayerMask environmentLayer;
    [Header("사운드")]
    [SerializeField] AudioSource networkAudioSource;
    [SerializeField] AudioClip punchSfx;
    [SerializeField] AudioClip shockwaveSfx;
    [Header("시각효과")]
    [SerializeField] GameObject hitEffect;
    [SerializeField] GameObject shockwaveVFX;
    public enum GauntletSfxType
    {
        Punch,
        Shockwave
    }

    public override void Init(PlayerWeapon owner, WeaponData data)
    {
        base.Init(owner, data);

        meleeWeapon = data as MeleeWeapon;
    }

    protected override void BasicAttack()
    {
        if(LeftClickTimer.ExpiredOrNotRunning(Runner))
        {
            RPC_PlayNetworkSfx(GauntletSfxType.Punch);
            Punch();
            LeftClickTimer = TickTimer.CreateFromSeconds(Runner, meleeWeapon.leftClickCoolTime);
        }
    }

    protected override void SecondAttack()
    {
       if(RightClickTimer.ExpiredOrNotRunning(Runner))
       {
            RPC_PlayNetworkSfx(GauntletSfxType.Shockwave);
            ShockWave();
            RightClickTimer = TickTimer.CreateFromSeconds(Runner, meleeWeapon.rightClickCoolTime);
       }
    }

    private void Punch()
    {
        if (myPlayer == null) return;

        Vector3 boxCenter = myPlayer.fpsCamera.transform.position +
            myPlayer.fpsCamera.transform.forward * (meleeWeapon.range / 2f);
        Vector3 boxSize = new Vector3(punchWidth, 2f, meleeWeapon.range);

        var hits = new System.Collections.Generic.List<LagCompensatedHit>();
        Runner.LagCompensation.OverlapBox(boxCenter, boxSize / 2f,
            myPlayer.fpsCamera.transform.rotation, Object.InputAuthority, hits, targetLayer);

        foreach (var hit in hits)
        {
            if (hit.Hitbox.Root.gameObject == myPlayer.gameObject) continue;
            if (HasStateAuthority)
            {
                hit.Hitbox.Root.GetComponent<PlayerHealth>()?.RPC_TakeDamage(meleeWeapon.damage, myPlayer.gameObject.name, Object.InputAuthority);
                hit.Hitbox.Root.GetComponent<ExplosiveBarrel>()?.RPC_TakeDamageBarrel(meleeWeapon.damage, myPlayer.gameObject.name);
            }
        }
        RPC_SpawnPunchVFX(boxCenter, myPlayer.fpsCamera.transform.rotation);
    }

    private void ShockWave()
    {
        if(myPlayer == null) return;

        Vector3 startPos = myPlayer.fpsCamera.transform.position;
        Vector3 direction = myPlayer.fpsCamera.transform.forward;

        Vector3 effectCenter = startPos + (direction * (hitRange / 2f));

        var hits = new List<LagCompensatedHit>();
        Runner.LagCompensation.OverlapSphere(startPos + (direction * (hitRange / 2f)),
            shockwaveRaius, Object.InputAuthority, hits, targetLayer);

        HashSet<GameObject> hitPlayers = new HashSet<GameObject>();

        foreach(var hit in hits)
        {
            if (hit.Hitbox.Root.gameObject == myPlayer.gameObject) continue;

            Player targetPlayer = hit.Hitbox.Root.GetComponent<Player>();
            if(targetPlayer != null)
            {
                if(hitPlayers.Contains(targetPlayer.gameObject)) continue;
                hitPlayers.Add(targetPlayer.gameObject);

                Vector3 pushDir = (targetPlayer.transform.position - myPlayer.transform.position).normalized;
                pushDir.y = 0;
                pushDir.Normalize();

                if(HasStateAuthority)
                {

                    int finalDamage = shockwaveBaseDamage;                

                    if(Physics.Raycast(targetPlayer.transform.position, pushDir,
                        out RaycastHit wallHit, knockbackDistance, environmentLayer))
                    {
                        finalDamage += wallDamage;
                        Debug.Log("벽 충돌!");
                    }
                    targetPlayer.GetComponent<PlayerHealth>()?.RPC_TakeDamage(finalDamage, myPlayer.gameObject.name, Object.InputAuthority);
                    
                    //넉백 시간 계산 : 시간 = 거리/속도
                    float duration = 0.1f;
                    if(shockPower > 0)
                    {
                        duration = knockbackDistance / shockPower;
                    }              
                
                    targetPlayer.RPC_ApplyKnockback(pushDir * shockPower, duration);
                }
            }
        }
        RPC_SpawnShockwaveVFX(effectCenter, myPlayer.fpsCamera.transform.rotation);
    }

    protected override void SkillQ() { }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    void RPC_PlayNetworkSfx(GauntletSfxType type)
    {
        if (networkAudioSource == null)
            return;

        switch (type)
        {
            case GauntletSfxType.Punch: networkAudioSource.PlayOneShot(punchSfx);
                break;

            case GauntletSfxType.Shockwave: networkAudioSource.PlayOneShot(shockwaveSfx);
                break;
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    private void RPC_SpawnPunchVFX(Vector3 centerPosition, Quaternion cameraRotation)
    {
        if (hitEffect != null)
        {
            // 1. 아직 에셋 방향을 모르니, 기본적으로 카메라가 보는 각도를 그대로 씁니다.
            // (나중에 에셋이 누워있거나 서 있다면 * Quaternion.Euler(90f, 0f, 0f) 를 추가하세요!)
            Quaternion spawnRotation = cameraRotation;

            // 2. 이펙트 생성
            GameObject vfx = Instantiate(hitEffect, centerPosition, spawnRotation);

            // 3. 일단 기본 크기(1배수)로 소환합니다. 나중에 에셋에 맞춰서 이 숫자를 조절하세요!
            vfx.transform.localScale = new Vector3(1f, 1f, 1f);

            // 4. 2초 뒤 삭제
            Destroy(vfx, 2f);
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    private void RPC_SpawnShockwaveVFX(Vector3 centerPosition, Quaternion cameraRotation)
    {
        if (shockwaveVFX != null)
        {
            // 1. 기본적으로 카메라가 보는 각도 유지 
            // (만약 에셋이 바닥에 깔려야 한다면 cameraRotation * Quaternion.Euler(90f, 0f, 0f) 로 변경하세요)
            Quaternion spawnRotation = cameraRotation;

            // 2. 계산된 위치(카메라 앞 4m)에 생성
            GameObject vfx = Instantiate(shockwaveVFX, centerPosition, spawnRotation);

            // 3. 일단 1배수로 둡니다.
            // (만약 8m 판정에 꽉 차게 만들고 싶다면 new Vector3(8f, 8f, 8f) 로 조절해보세요!)
            vfx.transform.localScale = new Vector3(1f, 1f, 1f);

            // 4. 2초 뒤 삭제
            Destroy(vfx, 2f);
        }
    }


}
