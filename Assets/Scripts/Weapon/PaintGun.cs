using Fusion;
using UnityEngine;

public class PaintGun : WeaponBase
{
    [Header("사운드")]
    [SerializeField] AudioSource localAudioSource;     // 2D
    [SerializeField] AudioSource networkAudioSource;   // 3D

    [SerializeField] AudioClip shootSfx;      
    [SerializeField] AudioClip reloadSfx;     

    [Header("추적 흔적")]
    [SerializeField] float debuffDuration = 5f;
    [SerializeField] LayerMask targetLayer;

    [Header("시각효과")]
    [SerializeField] ParticleSystem muzzleFlash;

    private RangedWeapon rangedWeapon;

    public enum PaintGunSfxType
    {
        Shoot
    }

    public override void Init(PlayerWeapon owner, WeaponData data)
    {
        base.Init(owner, data);
        rangedWeapon = data as RangedWeapon;
    }

    protected override void BasicAttack()
    {
        if(CurrentAmmo >= 1 && LeftClickTimer.ExpiredOrNotRunning(Runner))
        {
            CurrentAmmo -= 1;
            RPC_PlayNetworkSfx(PaintGunSfxType.Shoot);
            Shoot();
            LeftClickTimer = TickTimer.CreateFromSeconds(Runner, rangedData.leftClickCoolTime);
        }
    }

    private void Shoot()
    {
        if (myPlayer == null || myPlayer.fpsCamera == null) return;

        Vector3 origin = myPlayer.fpsCamera.transform.position;
        Vector3 direction = myPlayer.fpsCamera.transform.forward;

        Debug.DrawRay(origin, direction * rangedWeapon.range, Color.red, 2f);

        bool isHit = Runner.LagCompensation.Raycast(
            origin,
            direction,
            rangedWeapon.range,
            Object.InputAuthority,
            out LagCompensatedHit hit,
            targetLayer,
            HitOptions.IncludePhysX
        );

        if (!isHit)
        {
            Debug.Log("2. 허공에 빗나감");
            return;
        }

        if (hit.Hitbox != null)
        {

            GameObject target = hit.Hitbox.Root.gameObject;
            //Debug.Log($"4. 명중! 대상: {target.name}");

            if (target == myPlayer.gameObject)
            {
                //Debug.Log("내가 나를 맞춤 (자해 방지)");
                return;
            }
            //맞췄을때 본인 화면에서 일어나게 할 거 가능
            if (HasInputAuthority)
            {
                //Debug.Log($"[내 화면] 내가 {target.name}을(를) 공격함!");
            }
            if (HasStateAuthority)
            {
                PlayerHealth targetHP = target.GetComponent<PlayerHealth>();
                if (targetHP != null)
                {
                    targetHP.RPC_TakeDamage(rangedWeapon.damage, myPlayer.gameObject.name, Object.InputAuthority);
                }
                target.GetComponent<ExplosiveBarrel>()?.RPC_TakeDamageBarrel(rangedWeapon.damage, myPlayer.gameObject.name);

                PlayerWeapon targetWeapon = target.GetComponent<PlayerWeapon>();
                if (targetWeapon != null)
                {
                    targetWeapon.RPC_TakeHitLog(myPlayer.gameObject.name);
                }
            }
            //추후 필요하면 HasState안에 넣어주기
            Player targetPlayer = target.GetComponent<Player>();
            if(targetPlayer != null)
            {
                targetPlayer.RPC_ApplyTrailDebuff(debuffDuration, Object.InputAuthority);
                Debug.Log($"[{target.name}]에게 이동 흔적 디버프 부여!");
            }
        }
        else if (hit.Collider != null)
        {
            //Debug.Log($"맞은 오브젝트 : {hit.Collider.gameObject.name}");
            if (HasStateAuthority)
            {
                hit.Collider.GetComponentInParent<ExplosiveBarrel>()?.RPC_TakeDamageBarrel(rangedWeapon.damage, myPlayer.gameObject.name);
            }
        }
    }

    protected override void CheckReload(NetworkInputData data, NetworkButtons prevButtons)
    {
        bool canReload = data.buttons.WasPressed(prevButtons, MyButtons.Reload) && rangedData != null && CurrentAmmo < rangedData.MaxAmmo;

        if (canReload && HasInputAuthority && !IsReloading)
        {
            PlayLocalSfx(reloadSfx);
        }

        base.CheckReload(data, prevButtons);
    }

    void PlayLocalSfx(AudioClip clip)
    {
        if (clip == null || localAudioSource == null)
            return;

        if (Runner.IsResimulation)
            return;

        localAudioSource.PlayOneShot(clip);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    void RPC_PlayNetworkSfx(PaintGunSfxType type)
    {
        if (networkAudioSource == null)
            return;

        switch (type)
        {
            case PaintGunSfxType.Shoot: networkAudioSource.PlayOneShot(shootSfx);
                if (muzzleFlash != null && !muzzleFlash.isPlaying)
                {
                    muzzleFlash.Play();
                }
                break;
        }
    }

    protected override void SecondAttack() { }

    protected override void SkillQ() { }
}
