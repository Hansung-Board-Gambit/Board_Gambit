using Fusion;
using UnityEngine;

public enum GrappleState
{
    None,
    Firing,
    Pulling
}

public class Grappling : WeaponBase
{
    private MeleeWeapon meleeWeapon;
    [Header("기본 공격")]
    [SerializeField] LayerMask targetLayer;
    [SerializeField] float attackWidth = 1.5f;
    [Header("그래플링 설정")]
    [SerializeField] LayerMask environmentLayer;
    [SerializeField] float grappleRange = 40f;
    [SerializeField] float grappleSpeed = 15f;
    [SerializeField] float ableClimbHeight = 1f;
    [Header("추가 설정")]
    [SerializeField] float hookProjectileSpeed = 40f; //갈고리 날아가는 속도
    [SerializeField] float vaultUpPower = 12f; //옥상 도착 시 위로 붕 뜨는 힘
    [SerializeField] float vaultForwardPower = 8f; //옥상 도착 시 앞으로 넘어가는 힘
    [Header("그래플링 충전 시스템")]
    [SerializeField] int maxGrappleCharges = 2;
    [SerializeField] float grappleRechargeTime = 4f;
    [Header("그래플링 시각 효과")]
    [SerializeField] LineRenderer lr;
    [SerializeField] Transform grappleMuzzle;

    [Networked] public int GrappleCharges {  get; set; } //갈고리 개수
    [Networked] public TickTimer GrappleRechargeTimer { get; set; } //충전 타이머

    [Networked] public GrappleState CurrentState { get; set; } //상태 관리
    [Networked] public TickTimer HookTravelTimer { get; set; } //갈고리 날아가는 시간
    [Networked] public bool NeedsVault { get; set; } //반동 파쿠르가 필요한 옥상인지 

    //[Networked] public bool IsGrappling { get; set; } // 날아가는 중인지
    [Networked] public Vector3 GrappleTarget { get; set; } // 최종적으로 날아갈 목적지
    [Networked] public bool IsInitialized { get; set; } //초기화 체크
    [Networked] public float InitialTravelTime { get; set; } // 밧줄 연출 계산

    public override void Init(PlayerWeapon owner, WeaponData data)
    {
        base.Init(owner, data);

        meleeWeapon = data as MeleeWeapon;

    }
    protected override void BasicAttack()
    {
        if(LeftClickTimer.ExpiredOrNotRunning(Runner) && CurrentState == GrappleState.None)
        {
            SwingGrappling();
            LeftClickTimer = TickTimer.CreateFromSeconds(Runner, meleeWeapon.leftClickCoolTime);
        }
    }

    protected override void SecondAttack()
    {
       if(GrappleCharges >0 &&  RightClickTimer.ExpiredOrNotRunning(Runner))
       {
            Vector3 camPos = myPlayer.fpsCamera.transform.position;
            Vector3 camDir = myPlayer.fpsCamera.transform.forward;
            if (Physics.Raycast(camPos, camDir, out RaycastHit hit, grappleRange, environmentLayer))
            {
                Vector3 finalTarget = hit.point;
                NeedsVault = false;

                // 수직 벽(옥상 끄트머리) 검사 로직
                if (Mathf.Abs(hit.normal.y) < 0.3f)
                {
                    Vector3 roofCheckOrigin = hit.point + (Vector3.up * 2f) + (camDir * 0.05f);
                    if (Physics.Raycast(roofCheckOrigin, Vector3.down, out RaycastHit roofHit, 3f, environmentLayer))
                    {
                        finalTarget = roofHit.point;
                        NeedsVault = true;
                    }
                }

                GrappleTarget = finalTarget;

                // 투사체 지연 시간 계산
                float distance = Vector3.Distance(camPos, finalTarget);
                float travelTime = distance / hookProjectileSpeed;

                HookTravelTimer = TickTimer.CreateFromSeconds(Runner, travelTime);
                InitialTravelTime = travelTime; // 연출을 위해 초기 시간을 저장해둡니다.

                CurrentState = GrappleState.Firing;

                GrappleCharges--;
                RightClickTimer = TickTimer.CreateFromSeconds(Runner, meleeWeapon.rightClickCoolTime);
            }
        }
    }


    private void SwingGrappling()
    {
        if (myPlayer == null) return;

        Vector3 boxCenter = myPlayer.fpsCamera.transform.position + myPlayer.fpsCamera.transform.forward * (meleeWeapon.range / 2f);
        Vector3 boxSize = new Vector3(attackWidth, 1.5f, meleeWeapon.range);

        var hits = new System.Collections.Generic.List<LagCompensatedHit>();
        Runner.LagCompensation.OverlapBox(boxCenter, boxSize / 2f, myPlayer.fpsCamera.transform.rotation,
            Object.InputAuthority, hits, targetLayer);

        foreach(var hit in hits)
        {
            if(hit.Hitbox.Root.gameObject == myPlayer.gameObject) continue;
            if (HasStateAuthority)
            {
                hit.Hitbox.Root.GetComponent<PlayerHealth>()?.RPC_TakeDamage(meleeWeapon.damage, myPlayer.gameObject.name);
            }
        }

    }

    public override void OnFixedUpdateNetwork()
    {
        if(!IsInitialized)
        {
            GrappleCharges = maxGrappleCharges;
            IsInitialized = true;
        }
        //갈고리 충전
        if(GrappleCharges < maxGrappleCharges)
        {
            if(!GrappleRechargeTimer.IsRunning)
            {
                //타이머가 안 돌고 있으면 시작
                GrappleRechargeTimer = TickTimer.CreateFromSeconds(Runner, grappleRechargeTime);               
            }
            else if(GrappleRechargeTimer.Expired(Runner))
            {
                //타이머가 끝나면 1개 충전
                GrappleCharges++;

                //최대치가 아니면 다시 타이머 시작
                if (GrappleCharges < maxGrappleCharges)
                    GrappleRechargeTimer = TickTimer.CreateFromSeconds(Runner, grappleRechargeTime);
                else
                    GrappleRechargeTimer = TickTimer.None;
            }
        }
        if (myPlayer == null) return;

        //갈고리 이동 물리 로직
        //1단계 : 갈고리 투사체 날아가는 중
        if(CurrentState == GrappleState.Firing)
        {
           if(HookTravelTimer.Expired(Runner))
           {
                CurrentState = GrappleState.Pulling;
                myPlayer.isDashing = true;  //wasd잠금
                HookTravelTimer = TickTimer.None;
           }
        }
        //플레이어가 당겨지는 중
        else if(CurrentState == GrappleState.Pulling)
        {
            Vector3 currentPos = myPlayer.transform.position;
            float distanceToTarget = Vector3.Distance(currentPos, GrappleTarget);

            //목적지에 거의 다 왔을때 : 기존보다 판정을 살짝 넓게 줘서 자연스럽게 넘어감
            if(distanceToTarget <= 2f)
            {
                CurrentState = GrappleState.None;
                myPlayer.isDashing = false;

                //멈추기 말고 가속도(Velocity)를 쏴서 날아오르게 조정
                if(NeedsVault)
                {
                    //옥상 파쿠르 : 위로 크게 앞으로 살짝
                    Vector3 lookForward = myPlayer.fpsCamera.transform.forward;
                    lookForward.y = 0; //수평 방향만 추출

                    myPlayer.Controller.Velocity = (Vector3.up * vaultUpPower) + (lookForward.normalized * vaultForwardPower);
                }
                else
                {
                    //일반 벽 : 멈추지 말고 살짝 위로 튕겨서 체공 시간 확보
                    myPlayer.Controller.Velocity = Vector3.up * 4f;
                }
            }
            else
            {
                // 바닥 긁힘을 방지하는 아주 부드러운 당기기 로직
                Vector3 moveDir = (GrappleTarget - currentPos).normalized;

                // 만약 목표가 바닥 쪽이라도 억지로 파고들지 않게 최소 수평을 유지
                // 아래로 내려꽂는 그래플링일 때는 moveDir.y를 0으로 만들지 않습니다.
                if (GrappleTarget.y >= currentPos.y && moveDir.y < 0f)
                {
                    moveDir.y = 0f;
                }
                moveDir.Normalize();
                myPlayer.Controller.Move(moveDir * grappleSpeed * Runner.DeltaTime);

                myPlayer.Controller.Velocity = moveDir * grappleSpeed;
            }
                          
        }

    }

    public override void Render()
    {
        if (lr == null || grappleMuzzle == null) return;

        if (CurrentState == GrappleState.None)
        {
            lr.enabled = false; // 대기 중일 땐 선 끄기
            return;
        }

        lr.enabled = true; // 쏘거나 당길 땐 선 켜기
        lr.SetPosition(0, grappleMuzzle.position); // 무조건 0번 점은 내 총구 끝!

        if (CurrentState == GrappleState.Firing)
        {
            // 갈고리가 뻗어나가는 연출 (Lerp 사용)
            float remainTime = HookTravelTimer.RemainingTime(Runner) ?? 0f;
            float progress = InitialTravelTime > 0f ? 1f - (remainTime / InitialTravelTime) : 1f; 
            // InitialTravelTime이 0보다 클 때만 나누고, 아니면 바로 100%(1f)로 처리!
            progress = Mathf.Clamp01(progress); // 안전장치

            // 총구에서부터 타겟까지 선이 점점 길어집니다.
            Vector3 currentRopeEnd = Vector3.Lerp(grappleMuzzle.position, GrappleTarget, progress);
            lr.SetPosition(1, currentRopeEnd);
        }
        else if (CurrentState == GrappleState.Pulling)
        {
            // 이미 꽂혀서 당겨지는 중에는 목적지에 선 끝을 팽팽하게 고정
            lr.SetPosition(1, GrappleTarget);
        }
    }

    public float GetRecharge()
    {
        if(GrappleRechargeTimer.IsRunning)
            return 1f-(GrappleRechargeTimer.RemainingTime(Runner) ?? 0f) / grappleRechargeTime;
        return GrappleCharges == maxGrappleCharges ? 1f : 0f;
    }
    protected override void SkillQ() { }
    
}
