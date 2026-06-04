using Fusion;
using UnityEngine;

public class BounceGun : WeaponBase
{
    [Header("발사 세팅")]
    [SerializeField] GameObject bulletPrefab;
    [SerializeField] LayerMask targetLayer;

    [Header("튕기는 발사체 세팅")]
    [SerializeField] LineRenderer trajectoryLine;
    [SerializeField] int bounceTime = 3;
    [SerializeField] int lineSegments = 30;

    [Header("사운드")]
    [SerializeField] AudioSource localAudioSource;    // 2D
    [SerializeField] AudioSource networkAudioSource;  // 3D
    [SerializeField] AudioClip normalShotSfx; // 좌클릭 발사
    [SerializeField] AudioClip bounceShotSfx; // 우클릭 발사
    [SerializeField] AudioClip reloadSfx;     // 재장전 (로컬)


    private RangedWeapon rangedWeapon;

    public enum BounceGunSfxType
    {
        NormalShot,
        BounceShot,
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
            RPC_PlayNetworkSfx(BounceGunSfxType.NormalShot);
            SpawnProjectile(0);
            LeftClickTimer = TickTimer.CreateFromSeconds(Runner, rangedData.leftClickCoolTime);
        }
    }

    protected override void CheckRightClick(NetworkInputData data, NetworkButtons prevButtons)
    {
        if (data.buttons.IsSet(MyButtons.RightClick))
        {
            SecondAttack();
        }
    }

    protected override void SecondAttack()
    {
        //탄약이 2발씩 사라짐
        if(CurrentAmmo >= 2 && RightClickTimer.ExpiredOrNotRunning(Runner))
        {
            CurrentAmmo -= 2;
            RPC_PlayNetworkSfx(BounceGunSfxType.BounceShot);
            SpawnProjectile(bounceTime);

            RightClickTimer = TickTimer.CreateFromSeconds(Runner, rangedWeapon.rightClickCoolTime);
        }
    }

    protected override void CheckReload(NetworkInputData data, NetworkButtons prevButtons)
    {
        // 기존 WeaponBase 재장전 실행
        base.CheckReload(data, prevButtons);

        // 로컬 효과음만 재생
        if (HasInputAuthority && data.buttons.WasPressed(prevButtons, MyButtons.Reload) && CurrentAmmo < rangedWeapon.MaxAmmo)
        {
            PlayLocalSfx(reloadSfx);
        }
    }

    public override void OnFixedUpdateNetwork()
    {
        bool wasReloading = IsReloading;
        base.OnFixedUpdateNetwork();
        if (!wasReloading && IsReloading)
        {
            if (HasInputAuthority)
            {
                PlayLocalSfx(reloadSfx);
            }
        }
    }

    private void SpawnProjectile(int bounceCount)
    {
        if(HasStateAuthority)
        {
            if (myPlayer == null || myPlayer.fpsCamera == null) return;

            Vector3 camOrigin = myPlayer.fpsCamera.transform.position;
            Vector3 camForward = myPlayer.fpsCamera.transform.forward;
            Vector3 targetPoint;

            if(Runner.GetPhysicsScene().Raycast(camOrigin, camForward, out RaycastHit hit, 
                rangedWeapon.range, targetLayer))
            {
                targetPoint = hit.point; 
            }
            else
            {
                targetPoint = camOrigin + camForward * rangedWeapon.range;
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

            //허공에 투사체 프리팹 소환
            NetworkObject obj = Runner.Spawn(bulletPrefab,firePoint.position, Quaternion.LookRotation(shootDirection), null);

            //소환된 투사체에게 방향과 튕길 횟수 전달
            BounceProjectile proj = obj.GetComponent<BounceProjectile>();
            proj.InitProjectile(shootDirection, bounceCount, Object.InputAuthority);
        }
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
    void RPC_PlayNetworkSfx(BounceGunSfxType type)
    {
        if (networkAudioSource == null)
            return;

        switch (type)
        {
            case BounceGunSfxType.NormalShot: networkAudioSource.PlayOneShot(normalShotSfx);
                break;

            case BounceGunSfxType.BounceShot: networkAudioSource.PlayOneShot(bounceShotSfx);
                break;
        }
    }

    /*
    public override void OnRender()
    {
        //아직 미정
        if (Input.GetMouseButton(1)) Debug.Log("우클릭 인식됨!");

        //궤적 포물선 구현
        if (HasInputAuthority && Input.GetMouseButton(1) && trajectoryLine != null)
        {
            Debug.Log("궤적 그리기 코드 실행 중!!!");
            trajectoryLine.positionCount = lineSegments;
            Vector3 currentPos = player.firePoint.position;

            Vector3 currentVel = player.firePoint.forward * 20f;
            float timeStep = 0.1f;

            for(int i = 0; i< lineSegments; i++)
            {
                trajectoryLine.SetPosition(i, currentPos);
                currentVel += Physics.gravity * timeStep;
                currentPos += currentVel * timeStep;
            }
        }
        else if(trajectoryLine != null)
        {
            //선 지우기
            trajectoryLine.positionCount = 0;
        }
    }
    */
    protected override void SkillQ() { }
   
}
