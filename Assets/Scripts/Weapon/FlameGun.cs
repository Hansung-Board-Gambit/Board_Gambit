using Fusion;
using UnityEngine;

public class FlameGun : WeaponBase
{
    [Header("사운드")]
    [SerializeField] AudioSource localAudioSource;     // 재장전용
    [SerializeField] AudioSource networkAudioSource;   // 네트워크 공유용
    [SerializeField] AudioClip shootSfx;       // 좌클릭
    [SerializeField] AudioClip flameBallSfx;   // 우클릭 화염구
    [SerializeField] AudioClip reloadSfx;      // 재장전

    [Header("발사체 세팅")]
    [SerializeField] GameObject flameProjectilePrefab;
    [SerializeField] LayerMask targetLayer;

    [Header("시각효과")]
    [SerializeField] ParticleSystem muzzleFlash;

    private RangedWeapon rangedWeapon;

    public enum FlameSfxType
    {
        Shoot,
        FlameBall
    }

    public override void Init(PlayerWeapon owner, WeaponData data)
    {
        base.Init(owner, data);

        rangedWeapon = data as RangedWeapon;
    }

    protected override void BasicAttack()
    {
        if (CurrentAmmo >= 1 && LeftClickTimer.ExpiredOrNotRunning(Runner))
        {
            CurrentAmmo -= 1;
            RPC_PlayNetworkSfx(FlameSfxType.Shoot);
            Shoot();
            LeftClickTimer = TickTimer.CreateFromSeconds(Runner, rangedWeapon.leftClickCoolTime);
        }
    }
    protected override void SecondAttack()
    {
        if(CurrentAmmo == rangedWeapon.MaxAmmo)
        {
            //추후 소모한 탄약 개수에 따라 장판이 커지게 수정가능, 현재는 모든 탄약 소모
            CurrentAmmo = 0;
            RPC_PlayNetworkSfx(FlameSfxType.FlameBall);
            SpawnProjectile();
        }
    }

    protected override void CheckReload(NetworkInputData data, NetworkButtons prevButtons)
    {
        base.CheckReload(data, prevButtons);

        if (HasInputAuthority &&
            data.buttons.WasPressed(prevButtons, MyButtons.Reload) &&
            CurrentAmmo < rangedWeapon.MaxAmmo)
        {
            PlayLocalSfx(reloadSfx);
        }
    }

    private void SpawnProjectile()
    {
        if(HasStateAuthority)
        {
            if (myPlayer == null || myPlayer.fpsCamera == null) return;

            Vector3 camOrigin = myPlayer.fpsCamera.transform.position;
            Vector3 camForward = myPlayer.fpsCamera.transform.forward;
            Vector3 targetPoint;


            if (Runner.GetPhysicsScene().Raycast(camOrigin, camForward, 
                out RaycastHit hit, rangedWeapon.range, targetLayer))
            {
                targetPoint = hit.point; // 에임에 뭔가 걸리면 거기가 타겟!
            }
            else
            {
                targetPoint = camOrigin + camForward * rangedWeapon.range; // 허공이면 100m 앞의 좌표
            }

            //Vector3 shootDirection = (targetPoint - firePoint.position).normalized;

            Vector3 spawnPos = firePoint.position;
            Vector3 shootDirection = (targetPoint - spawnPos).normalized;

            float distanceToTarget = Vector3.Distance(camOrigin, targetPoint);

            if (distanceToTarget < 0.5f)
            {
                shootDirection = camForward;
            }
            else
            {  
                shootDirection = (targetPoint - spawnPos).normalized;
            }

            //총구가 바닥이나 벽을 뚫었는지 검사
            if (Vector3.Dot(camForward, shootDirection) < 0.5f)
            {
                shootDirection = camForward;

                spawnPos = camOrigin;
            }

            Quaternion spawnRotation = Quaternion.LookRotation(shootDirection);

            NetworkObject obj = Runner.Spawn(flameProjectilePrefab, firePoint.position,
                Quaternion.LookRotation(shootDirection), null);
            FlameProjectile proj = obj.GetComponent<FlameProjectile>();
            proj.InitProjectile(shootDirection, Object.InputAuthority);
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
            Debug.Log($"4. 명중! 대상: {target.name}");

            if (target == myPlayer.gameObject)
            {
                Debug.Log("내가 나를 맞춤 (자해 방지)");
                return;
            }
            //맞췄을때 본인 화면에서 일어나게 할 거 가능
            if (HasInputAuthority)
            {
                Debug.Log($"[내 화면] 내가 {target.name}을(를) 공격함!");
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
        }
        else if (hit.Collider != null)
        {
            Debug.Log($"맞은 오브젝트 : {hit.Collider.gameObject.name}");
            if (HasStateAuthority)
            {
                hit.Collider.GetComponentInParent<ExplosiveBarrel>()?.RPC_TakeDamageBarrel(rangedWeapon.damage, myPlayer.gameObject.name);
            }
        }
    }

    protected override void SkillQ() { }

    void PlayLocalSfx(AudioClip clip)
    {
        if (localAudioSource == null || clip == null)
            return;

        if (Runner.IsResimulation)
            return;

        localAudioSource.PlayOneShot(clip);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    void RPC_PlayNetworkSfx(FlameSfxType type)
    {
        if (networkAudioSource == null)
            return;

        switch (type)
        {
            case FlameSfxType.Shoot: networkAudioSource.PlayOneShot(shootSfx);
                if(muzzleFlash != null && !muzzleFlash.isPlaying)
                {
                    muzzleFlash.Play();
                }
                break;

            case FlameSfxType.FlameBall: networkAudioSource.PlayOneShot(flameBallSfx);
                break;
        }
    }
}
