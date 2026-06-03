using Fusion;
using System.Collections;
using UnityEngine;

public class Hammer : WeaponBase
{
    private MeleeWeapon meleeWeapon;

    [SerializeField] LayerMask targetLayer;
    [Header("기본공격")]
    [SerializeField] float attackWidth = 2f;

    [Header("대쉬 설정")]
    [SerializeField] float dashForce = 10f;
    //[SerializeField] float dashCoolTime = 0.8f;

    [Header("낙하 공격")]
    [SerializeField] int Damage = 60;
    //뛰기 가능한 높이
    [SerializeField] float minPlungeHeight = 3f;
    [SerializeField] float plungeSpeed = 30f;
    [SerializeField] float slamRadius = 4f;
    [Header("모션")]
    [SerializeField] Transform hammerModel;
    [SerializeField] float swingSpeed = 10f;
    [SerializeField] Vector3 swingRotation = new Vector3(0,-90,0);

    [Header("타격 판정-논리용")]
    [SerializeField] float hitDelay = 0.2f;

    [Header("사운드")]
    [SerializeField] AudioSource localAudioSource;
    [SerializeField] AudioSource networkAudioSource;
    [SerializeField] AudioClip swingSfx;       // 좌클릭 - 네트워크 공유
    [SerializeField] AudioClip dashSfx;        // 우클릭 - 네트워크 공유
    [SerializeField] AudioClip plungeStartSfx; // Q 시작 - 로컬만
    [SerializeField] AudioClip slamSfx;        // 착지 강타 - 네트워크 공유

    [Header("시각 효과")]
    [SerializeField] GameObject swingAssetPrefab;
    [SerializeField] GameObject slamVFXObject;


    //서버용 타이머
    [Networked] public TickTimer HitTimer {  get; set; }
    //원래 망치 각도
    private Quaternion originalRotation;
    //휘두르는 중인지
    private bool isSwinging = false;


    //[Networked] public TickTimer DashTimer {  get; set; }
    [Networked] public bool IsPlunging { get; set; }
    //대쉬 지속 시간 타이머
    [Networked] public TickTimer DashDurationTimer { get; set; }
    //대쉬 방향
    [Networked] public Vector3 DashDir { get; set; }


    public enum HammerSfxType
    {
        Swing,
        Dash,
        Slam
    }

    public override void Init(PlayerWeapon owner, WeaponData data)
    {
        base.Init(owner, data);
        meleeWeapon = data as MeleeWeapon;
        
        if(hammerModel != null ) originalRotation = hammerModel.localRotation;

    }
    protected override void BasicAttack()
    {
        if(LeftClickTimer.ExpiredOrNotRunning(Runner) && !isSwinging)
        {
            RPC_PlayNetworkSfx(HammerSfxType.Swing);
            //나중에 애니메이션 추가후 swingHammer는 제거 -> 애니메이션과 연결, add animation event 연결
            //모션 시작
            //if (hammerModel != null) StartCoroutine(SwingMotion());
            SwingHammer();
            //서버한테 일정 시간 후에 논리적 타격 지시
            //HitTimer = TickTimer.CreateFromSeconds(Runner,hitDelay);
            //전체 쿨타임
            LeftClickTimer = TickTimer.CreateFromSeconds(Runner, meleeWeapon.leftClickCoolTime);
        }
    }

    protected override void SecondAttack()
    {
        if(RightClickTimer.ExpiredOrNotRunning(Runner))
        {
            RPC_PlayNetworkSfx(HammerSfxType.Dash);
            Vector3 tempDir = myPlayer.fpsCamera.transform.forward;
            tempDir.y = 0f;
            if(tempDir.sqrMagnitude > 0.01f)
            {
                tempDir.Normalize();
            }
            else
            {
                tempDir = transform.forward;
                tempDir.y = 0f; // 혹시 모르니 몸통 방향도 Y축 제거
                tempDir.Normalize();
            }

            DashDir = tempDir;

            //0.2초동안 질주
            DashDurationTimer = TickTimer.CreateFromSeconds(Runner, 0.2f);

            RightClickTimer = TickTimer.CreateFromSeconds(Runner,meleeWeapon.rightClickCoolTime);
            myPlayer.isDashing = true;
        }
    }

    protected override void SkillQ()
    {
        if(!SkillQTimer.ExpiredOrNotRunning(Runner))
            return;
        Vector3 rayStartPos = transform.position + (Vector3.down * 1.1f);
        int floorMask = ~LayerMask.GetMask("Player");

        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 100f, floorMask))
        {
            float actualHeight = hit.distance + 1.1f;

            if (actualHeight >= minPlungeHeight && !IsPlunging)
            {
                PlayLocalSfx(plungeStartSfx);
                StartPlunge();
                SkillQTimer = TickTimer.CreateFromSeconds(Runner, meleeWeapon.skillQCoolTime);
            }
            else if (actualHeight < minPlungeHeight)
            {
                Debug.Log("높이가 너무 낮습니다! 현재 높이: " + actualHeight);
            }
        }
    }

    private void SwingHammer()
    {
        if (myPlayer == null) return;

        Vector3 boxCenter = myPlayer.fpsCamera.transform.position + myPlayer.fpsCamera.transform.forward * (meleeWeapon.range / 2f);
        Vector3 boxSize = new Vector3(attackWidth, 2f, meleeWeapon.range);

        var hits = new System.Collections.Generic.List<LagCompensatedHit>();
        Runner.LagCompensation.OverlapBox(boxCenter, boxSize / 2f,
            myPlayer.fpsCamera.transform.rotation, Object.InputAuthority, hits, targetLayer);

        foreach(var hit in hits)
        {
            if (hit.Hitbox.Root.gameObject == myPlayer.gameObject) continue;
            if (HasStateAuthority)
            {
                hit.Hitbox.Root.GetComponent<PlayerHealth>()?.RPC_TakeDamage(meleeWeapon.damage, myPlayer.gameObject.name, Object.InputAuthority);
                hit.Hitbox.Root.GetComponent<ExplosiveBarrel>()?.RPC_TakeDamageBarrel(meleeWeapon.damage, myPlayer.gameObject.name);
            }
        }
        RPC_SpawnSwingVFX(boxCenter, myPlayer.fpsCamera.transform.rotation, boxSize);
    }

    

    //낙하시작
    private void StartPlunge()
    {
        IsPlunging = true;
        if (HasInputAuthority && myPlayer != null) myPlayer.StartPlungeCameraLock();

        if (myPlayer != null && myPlayer.Controller != null)
        {
            myPlayer.Controller.Velocity = Vector3.zero;
        }
    }

    public override void OnFixedUpdateNetwork()
    {
        /*
        if(HitTimer.Expired(Runner))
        {
            //정해둔 시간이 지났을때 : 투명상자 소환후 피격 계산
            SwingHammer();
            //타이머 끄기
            HitTimer = TickTimer.None;
        }
        */
        if(IsPlunging && myPlayer != null)
        {
            myPlayer.Controller.Velocity = Vector3.down * plungeSpeed;

            if (myPlayer.Controller.Grounded && HasStateAuthority)
            {
                LandSlam();
            }
        }

        if(DashDurationTimer.IsRunning && myPlayer != null)
        {           
            if(DashDurationTimer.Expired(Runner))
            {
                myPlayer.Controller.Velocity = Vector3.zero;
                myPlayer.isDashing = false;
                DashDurationTimer = TickTimer.None;
            }
            else
            {
                myPlayer.GetComponent<CharacterController>().Move(DashDir * dashForce * Runner.DeltaTime);
                myPlayer.Controller.Velocity = Vector3.zero;
            }
        
        }       
    }

    //착지해서 피해
    private void LandSlam()
    {
        IsPlunging = false;
        if (myPlayer != null && myPlayer.Controller != null)
        {
            myPlayer.Controller.Velocity = Vector3.zero;
        }
        RPC_PlayNetworkSfx(HammerSfxType.Slam);

        RPC_PlaySlamVFX(transform.position, slamRadius);
        if (HasStateAuthority)
        {
            RPC_PlaySlamSfx();
            var hits = new System.Collections.Generic.List<LagCompensatedHit>();
            Runner.LagCompensation.OverlapSphere(transform.position, slamRadius,
                Object.InputAuthority, hits, targetLayer);

            foreach(var h in hits)
            {
                if(h.Hitbox.Root.gameObject == myPlayer.gameObject) continue;
                h.Hitbox.Root.GetComponent<PlayerHealth>()?.RPC_TakeDamage(meleeWeapon.damage, myPlayer.gameObject.name, Object.InputAuthority);
                h.Hitbox.Root.GetComponent<ExplosiveBarrel>()?.RPC_TakeDamageBarrel(meleeWeapon.damage, myPlayer.gameObject.name);
            }
        }
        
    }

    public override void Render()
    {
        if (myPlayer == null || !HasInputAuthority) return;

        // 떨어지는 중(IsPlunging)이면 무조건 잠그고, 아니면 무조건 풀어버립니다.
        if (IsPlunging)
        {
            myPlayer.StartPlungeCameraLock();
        }
        else
        {
            myPlayer.ReleasePlungeCameraLock();
        }
    }

    void PlayLocalSfx(AudioClip clip)
    {
        if (clip == null || localAudioSource == null)
            return;

        if (Runner.IsResimulation)
            return;

        localAudioSource.spatialBlend = 0f; // 2D 강제
        localAudioSource.PlayOneShot(clip);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    void RPC_PlayNetworkSfx(HammerSfxType type)
    {
        if (networkAudioSource == null)
            return;

        switch (type)
        {
            case HammerSfxType.Swing: networkAudioSource.PlayOneShot(swingSfx);
                break;

            case HammerSfxType.Dash: networkAudioSource.PlayOneShot(dashSfx);
                break;
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void RPC_PlaySlamSfx()
    {
        networkAudioSource?.PlayOneShot(slamSfx);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    private void RPC_SpawnSwingVFX(Vector3 centerPosition, Quaternion cameraRotation, Vector3 targetScale)
    {
        if (swingAssetPrefab != null)
        {
            // 1. 각도를 90도 숙여서 소환
            Quaternion tiltedRotation = cameraRotation * Quaternion.Euler(90f, 0f, 0f);
            GameObject vfx = Instantiate(swingAssetPrefab, centerPosition, tiltedRotation);

            // 2. 박스 크기 무시하고, 무조건 전체 크기를 0.7배로 고정!
            vfx.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);

            // 3. 2초 뒤 삭제
            Destroy(vfx, 2f);
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    private void RPC_PlaySlamVFX(Vector3 slamPosition, float radius)
    {
        if (slamVFXObject != null)
        {
            Quaternion flatRotation = Quaternion.Euler(0f, 0f, 0f);

            // 2. 바닥 위치(slamPosition)에 눕힌 각도로 프리팹 짠! 소환
            GameObject vfx = Instantiate(slamVFXObject, slamPosition, flatRotation);

            // 3. 판정 범위 무시하고, 무조건 전체 크기를 2.3배로 고정!
            vfx.transform.localScale = new Vector3(2.3f, 2.3f, 2.3f);

            // 4. 메모리 정리를 위해 2초 뒤 깔끔하게 삭제
            Destroy(vfx, 2f);
        }
    }

}
