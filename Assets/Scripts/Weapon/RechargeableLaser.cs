using Fusion;
using UnityEngine;

public class RechargeableLaser : WeaponBase
{
    [SerializeField] float zoomFOV = 20f;

    [Header("차징 세팅")]
    [SerializeField] float chargeSpeed = 50f;
    [SerializeField] float dischargeSpeed = 35f;
    [SerializeField] LayerMask targetLayer;

    [Header("사운드")]
    [SerializeField] AudioSource localAudioSource;     // 2D
    [SerializeField] AudioSource networkAudioSource;   // 3D
    [SerializeField] AudioClip chargeStartSfx; // 좌클릭 홀드 시작 (로컬)
    [SerializeField] AudioClip fullChargeSfx;  // 완충 (로컬)
    [SerializeField] AudioClip zoomInSfx;      // 줌 인 (로컬)
    [SerializeField] AudioClip zoomOutSfx;     // 줌 아웃 (로컬)
    [SerializeField] AudioClip shootSfx;       // 발사 (네트워크 공유)

    private RangedWeapon rangedWeapon;

    private bool isPressing;
    private bool wasFullyCharged;

    //현재 게이지
    [Networked] public float currentGage {  get; set; }

    public enum LaserSfxType
    {
        Shoot
    }

    public override void Init(PlayerWeapon owner, WeaponData data)
    {
        base.Init(owner, data);

        rangedWeapon = data as RangedWeapon;
    }

    protected override void CheckLeftClick(NetworkInputData data, NetworkButtons prevButtons)
    {
        if (data.buttons.WasPressed(prevButtons, MyButtons.LeftClick))
        {
            if (HasInputAuthority)
            {
                PlayLocalSfx(chargeStartSfx);
            }
        }

        //좌클릭이 눌리고 있을때 true
        isPressing = data.buttons.IsSet(MyButtons.LeftClick);
       
        if (data.buttons.WasReleased(prevButtons, MyButtons.LeftClick))
        {
            //만약 총알이 다 채워졌다면 발사가능           
            if(currentGage >= rangedWeapon.MaxAmmo)
            {
                BasicAttack();
                currentGage = 0;
            }

            wasFullyCharged = false;
        }
    }

    protected override void CheckRightClick(NetworkInputData data, NetworkButtons prevButtons)
    {
        if(data.buttons.WasPressed(prevButtons, MyButtons.RightClick))
        {
            //줌 확대됨
            player.TargetFOV = zoomFOV;
            if (HasInputAuthority)
            {
                PlayLocalSfx(zoomInSfx);
            }
        }

        // 누르고 있는 동안은 FOV만 유지
        if (data.buttons.IsSet(MyButtons.RightClick))
        {
            player.TargetFOV = zoomFOV;
        }

        // 줌 해제 순간 1번만
        if (data.buttons.WasReleased(prevButtons, MyButtons.RightClick))
        {
            player.TargetFOV = player.defaultFOV;

            if (HasInputAuthority)
            {
                PlayLocalSfx(zoomOutSfx);
            }
        }
    }

    //게이지 증가 및 감소(총알)
    public override void OnFixedUpdateNetwork()
    {
      
        Debug.Log($"현재 게이지 : {currentGage.ToString("F0")}");

        if(isPressing)
        {
            currentGage += chargeSpeed * Runner.DeltaTime;
            if(currentGage > rangedWeapon.MaxAmmo) currentGage = rangedWeapon.MaxAmmo;

            if (!wasFullyCharged && currentGage >= rangedWeapon.MaxAmmo)
            {
                wasFullyCharged = true;

                if (HasInputAuthority)
                {
                    PlayLocalSfx(fullChargeSfx);
                }
            }
        }
        else
        {
            currentGage -= dischargeSpeed * Runner.DeltaTime;
            if (currentGage < 0) currentGage = 0;
        }
        //int displayGage = Mathf.FloorToInt(currentGage); - 실제 정수로 변환
        CurrentAmmo = Mathf.FloorToInt(currentGage);
    }

    protected override void BasicAttack()
    {
        if (!Object.HasStateAuthority)
            return;

        RPC_PlayNetworkSfx(LaserSfxType.Shoot);
        Shoot();
    }    
    protected override void SecondAttack() { }
    protected override void SkillQ() { }

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
            targetLayer
        );

        if (!isHit)
        {
            Debug.Log("2. 허공에 빗나감");
            return;
        }
        
        if(hit.Hitbox != null)
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
                //추후 화약통한테 데미지 들어가는 코드 조정
                PlayerWeapon targetWeapon = target.GetComponent<PlayerWeapon>();
                if (targetWeapon != null)
                {
                    targetWeapon.RPC_TakeHitLog(myPlayer.gameObject.name);
                }
            }
        }
        else if(hit.Collider != null)
        {
            Debug.Log($"맞은 오브젝트 : {hit.Collider.gameObject.name}");
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

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void RPC_PlayNetworkSfx(LaserSfxType type)
    {
        if (networkAudioSource == null)
            return;

        switch (type)
        {
            case LaserSfxType.Shoot:
                networkAudioSource.PlayOneShot(shootSfx);
                break;
        }
    }

    protected override void CheckReload(NetworkInputData data, NetworkButtons prevButtons)
    {
        
    }
}