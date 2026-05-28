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
            //나중에 애니메이션 추가후 swingHammer는 제거 -> 애니메이션과 연결, add animation event 연결
            //모션 시작
            if (hammerModel != null) StartCoroutine(SwingMotion());
            //서버한테 일정 시간 후에 논리적 타격 지시
            HitTimer = TickTimer.CreateFromSeconds(Runner,hitDelay);
            //전체 쿨타임
            LeftClickTimer = TickTimer.CreateFromSeconds(Runner, meleeWeapon.leftClickCoolTime);
        }
    }

    protected override void SecondAttack()
    {
        if(RightClickTimer.ExpiredOrNotRunning(Runner))
        {
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
        {
            return;
        }
        Vector3 rayStartPos = transform.position + (Vector3.down * 1.1f);
        int floorMask = ~LayerMask.GetMask("Player");

        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 100f,floorMask))
        {
            float actualHeight = hit.distance + 1.1f;

            if (actualHeight >= minPlungeHeight && !IsPlunging)
            {
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

        Vector3 boxCenter = myPlayer.fpsCamera.transform.position +
            myPlayer.fpsCamera.transform.forward * (meleeWeapon.range / 2f);
        Vector3 boxSize = new Vector3(attackWidth, 2f, meleeWeapon.range);

        var hits = new System.Collections.Generic.List<LagCompensatedHit>();
        Runner.LagCompensation.OverlapBox(boxCenter, boxSize / 2f,
            myPlayer.fpsCamera.transform.rotation, Object.InputAuthority, hits, targetLayer);

        foreach(var hit in hits)
        {
            if (hit.Hitbox.Root.gameObject == myPlayer.gameObject) continue;
            if (HasStateAuthority)
            {
                hit.Hitbox.Root.GetComponent<PlayerHealth>()?.RPC_TakeDamage(meleeWeapon.damage, myPlayer.gameObject.name);
            }
        }
        //휘두르는 모션
    }

    //휘두르기 모션 코루틴
    private IEnumerator SwingMotion()
    {
        isSwinging = true;
        //출발점 기억하기
        Vector3 startPos = hammerModel.localPosition;
        Quaternion startRot = hammerModel.localRotation;
        //목표 계산
        Vector3 swingTargetPos = startPos + new Vector3(-0.2f, 0f, 0.5f);

        // 2. 각도 회전: 망치를 가로로 눕히고(Z축), 플레이어 왼쪽으로 휙 돌립니다(Y축).
        // (주의: 박스 조립 방향에 따라 각도가 다를 수 있습니다. 이상하게 돌면 아래 90, -90 숫자들을 요리조리 바꿔보세요!)
        Quaternion swingTargetRot = startRot * Quaternion.Euler(10f, -90f, 90f);

        // 1단계: 부채꼴 휘두르기 (오른쪽 -> 왼쪽)
        float t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime * swingSpeed; // (swingSpeed는 12 정도로 추천)
            float smoothT = Mathf.SmoothStep(0, 1, t);

            // 위치는 부드럽게 이동 (거의 제자리)
            hammerModel.localPosition = Vector3.Lerp(startPos, swingTargetPos, smoothT);

            //핵심: Slerp를 사용해서 손잡이 중심의 완벽한 둥근 궤적(부채꼴)을 만듭니다!
            hammerModel.localRotation = Quaternion.Slerp(startRot, swingTargetRot, smoothT);

            yield return null;
        }

        // 2단계: 원래 자리로 회수 (스르륵)
        t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime * (swingSpeed * 0.5f);
            float smoothT = Mathf.SmoothStep(0, 1, t);

            hammerModel.localPosition = Vector3.Lerp(swingTargetPos, startPos, smoothT);
            hammerModel.localRotation = Quaternion.Slerp(swingTargetRot, startRot, smoothT);

            yield return null;
        }
        hammerModel.localPosition = startPos;
        hammerModel.localRotation = startRot;

        isSwinging = false;
    }

    //낙하시작
    private void StartPlunge()
    {
        IsPlunging = true;
        if (HasInputAuthority && myPlayer != null) myPlayer.StartPlungeCameraLock();
    }

    public override void OnFixedUpdateNetwork()
    {
        if(HitTimer.Expired(Runner))
        {
            //정해둔 시간이 지났을때 : 투명상자 소환후 피격 계산
            SwingHammer();
            //타이머 끄기
            HitTimer = TickTimer.None;
        }
        if(IsPlunging && myPlayer != null)
        {
            myPlayer.Controller.Move(Vector3.down * plungeSpeed * Runner.DeltaTime);

            if(myPlayer.Controller.Grounded)
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
        if(HasStateAuthority)
        {
            var hits = new System.Collections.Generic.List<LagCompensatedHit>();
            Runner.LagCompensation.OverlapSphere(transform.position, slamRadius,
                Object.InputAuthority, hits, targetLayer);

            foreach(var h in hits)
            {
                if(h.Hitbox.Root.gameObject == myPlayer.gameObject) continue;
                h.Hitbox.Root.GetComponent<PlayerHealth>()?.RPC_TakeDamage(meleeWeapon.damage, myPlayer.gameObject.name);
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
}
