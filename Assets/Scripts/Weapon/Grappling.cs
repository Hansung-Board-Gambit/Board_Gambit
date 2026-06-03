using Fusion;
using UnityEngine;

public class Grappling : WeaponBase
{
    private MeleeWeapon meleeWeapon;

    [Header("기본 공격")]
    [SerializeField] LayerMask targetLayer;
    [SerializeField] float attackWidth = 1.5f;

    [Header("그래플링 설정")]
    [SerializeField] LayerMask environmentLayer;
    [SerializeField] float grappleRange = 40f;
    [SerializeField] float grappleSpeed = 100f;
    [SerializeField] float ableClimbHeight = 1f;

    [Header("추가 설정")]
    [SerializeField] float hookProjectileSpeed = 80f;
    [SerializeField] float vaultUpPower = 12f;
    [SerializeField] float vaultForwardPower = 8f;

    [Header("그래플링 충전 시스템")]
    [SerializeField] int maxGrappleCharges = 2;
    [SerializeField] float grappleRechargeTime = 4f;

    [Header("그래플링 시각 효과")]
    [SerializeField] LineRenderer lr;
    [SerializeField] Transform grappleMuzzle;
    [SerializeField] GameObject flyingKunaiPrefab;

    [Header("사운드")]
    [SerializeField] AudioSource networkAudioSource;
    [SerializeField] AudioClip swingSfx;
    [SerializeField] AudioClip grappleSfx;

    [Header("시각 효과")]
    [SerializeField] ParticleSystem hitEffect;

    [Networked] public int GrappleCharges { get; set; }
    [Networked] public TickTimer GrappleRechargeTimer { get; set; }
    [Networked] public bool IsInitialized { get; set; }

    //핵심 변경점: Enum을 없애고 [발사]와 [당기기]를 분리하여 동시에 일어날 수 있게 만듭니다.
    [Networked] public bool IsFiring { get; set; }  // 투사체가 날아가는 중인가?
    [Networked] public bool IsPulling { get; set; } // 플레이어가 당겨지는 중인가?

    [Networked] public Vector3 FireTarget { get; set; } // 날아가고 있는 '새로운' 갈고리의 목적지
    [Networked] public Vector3 PullTarget { get; set; } // 현재 플레이어를 '당기고 있는' 목적지

    [Networked] public bool NextNeedsVault { get; set; } // 다음에 도착할 곳의 옥상 파쿠르 여부
    [Networked] public bool CurrentNeedsVault { get; set; } // 현재 당겨지는 곳의 옥상 파쿠르 여부

    [Networked] public TickTimer HookTravelTimer { get; set; }
    [Networked] public float InitialTravelTime { get; set; }

    private GameObject activeFlyingKunai;

    public enum GrappleSfxType
    {
        Swing,
        Grapple
    }

    public override void Init(PlayerWeapon owner, WeaponData data)
    {
        base.Init(owner, data);
        meleeWeapon = data as MeleeWeapon;
    }

    protected override void BasicAttack()
    {
        // 당겨지는 중(IsPulling)에도 평타를 칠 수 있게 조건 완화
        if (LeftClickTimer.ExpiredOrNotRunning(Runner) && !IsFiring)
        {
            RPC_PlayNetworkSfx(GrappleSfxType.Swing);
            SwingGrappling();
            LeftClickTimer = TickTimer.CreateFromSeconds(Runner, meleeWeapon.leftClickCoolTime);
        }
    }

    protected override void SecondAttack()
    {
        // 갈고리가 있고, 우클릭 쿨타임이 끝났다면 (당겨지는 와중에도 쏠 수 있습니다!)
        if (GrappleCharges > 0 && RightClickTimer.ExpiredOrNotRunning(Runner))
        {
            Vector3 camPos = myPlayer.fpsCamera.transform.position;
            Vector3 camDir = myPlayer.fpsCamera.transform.forward;
            int grappleMask = environmentLayer.value | LayerMask.GetMask("Default", "Board", "PlacedObject");

            if (Physics.Raycast(camPos, camDir, out RaycastHit hit, grappleRange, grappleMask))
            {
                RPC_PlayNetworkSfx(GrappleSfxType.Grapple);
                Vector3 finalTarget = hit.point;
                bool needsVaultCheck = false;

                if (Mathf.Abs(hit.normal.y) < 0.3f)
                {
                    Vector3 roofCheckOrigin = hit.point + (Vector3.up * 2f) + (camDir * 0.05f);
                    if (Physics.Raycast(roofCheckOrigin, Vector3.down, out RaycastHit roofHit, 3f, grappleMask))
                    {
                        finalTarget = roofHit.point;
                        needsVaultCheck = true;
                    }
                }

                // 중요: '현재 당겨지는 목적지'를 덮어씌우지 않고, '새로운 목적지(FireTarget)'로 저장합니다!
                FireTarget = finalTarget;
                NextNeedsVault = needsVaultCheck;

                float distance = Vector3.Distance(camPos, finalTarget);
                float travelTime = distance / hookProjectileSpeed;

                HookTravelTimer = TickTimer.CreateFromSeconds(Runner, travelTime);
                InitialTravelTime = travelTime;

                IsFiring = true; // 투사체 발사 시작 (IsPulling은 건드리지 않으므로, 기존 방향으로 계속 날아갑니다!)

                GrappleCharges--;
                RightClickTimer = TickTimer.CreateFromSeconds(Runner, meleeWeapon.rightClickCoolTime);
            }
            else
            {
                Debug.Log("Grappling missed environment.");
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

        foreach (var hit in hits)
        {
            if (hit.Hitbox.Root.gameObject == myPlayer.gameObject) continue;
            if (HasStateAuthority)
            {
                hit.Hitbox.Root.GetComponent<PlayerHealth>()?.RPC_TakeDamage(meleeWeapon.damage, myPlayer.gameObject.name, Object.InputAuthority);
                hit.Hitbox.Root.GetComponent<ExplosiveBarrel>()?.RPC_TakeDamageBarrel(meleeWeapon.damage, myPlayer.gameObject.name);
            }
        }
    }

    public override void OnFixedUpdateNetwork()
    {
        if (!IsInitialized)
        {
            GrappleCharges = maxGrappleCharges;
            IsInitialized = true;
        }

        // --- 갈고리 충전 로직 ---
        if (GrappleCharges < maxGrappleCharges)
        {
            if (!GrappleRechargeTimer.IsRunning)
            {
                GrappleRechargeTimer = TickTimer.CreateFromSeconds(Runner, grappleRechargeTime);
            }
            else if (GrappleRechargeTimer.Expired(Runner))
            {
                GrappleCharges++;
                if (GrappleCharges < maxGrappleCharges)
                    GrappleRechargeTimer = TickTimer.CreateFromSeconds(Runner, grappleRechargeTime);
                else
                    GrappleRechargeTimer = TickTimer.None;
            }
        }

        if (myPlayer == null) return;

        // --- 1단계: 쏜 갈고리가 벽에 닿았을 때의 처리 ---
        if (IsFiring)
        {
            if (HookTravelTimer.Expired(Runner))
            {
                IsFiring = false;       // 발사 연출 끝
                IsPulling = true;       // 이제부터 '진짜' 당겨지기 시작!

                PullTarget = FireTarget; // 1번 방향으로 날아가고 있었더라도, 이 순간 2번 방향으로 목적지가 즉시 갱신됩니다!
                CurrentNeedsVault = NextNeedsVault;

                myPlayer.isGrappling = true;  
                HookTravelTimer = TickTimer.None;
            }
        }

        // --- 2단계: 플레이어가 당겨지는 물리 처리 (IsFiring과 독립적으로 계~속 실행됨!) ---
        if (IsPulling)
        {
            Vector3 currentPos = myPlayer.transform.position;

            //Vector3 horizontalCurrent = new Vector3(currentPos.x, 0, currentPos.z);
            //Vector3 horizontalTarget = new Vector3(PullTarget.x, 0, PullTarget.z);
            
            float distanceToTarget = Vector3.Distance(currentPos, PullTarget);

            if (distanceToTarget <= 2f)
            {
                IsPulling = false;
                myPlayer.isGrappling = false;

                if (CurrentNeedsVault)
                {
                    Vector3 lookForward = myPlayer.fpsCamera.transform.forward;
                    lookForward.y = 0;
                    myPlayer.Controller.Velocity = (Vector3.up * vaultUpPower) + (lookForward.normalized * vaultForwardPower);
                }
                else
                {
                    myPlayer.Controller.Velocity = Vector3.up * 4f;
                }
            }
            else
            {
                Vector3 moveDir = (PullTarget - currentPos).normalized;
                if (PullTarget.y >= currentPos.y && moveDir.y < 0f)
                {
                    moveDir.y = 0f;
                }
                moveDir.Normalize();
                myPlayer.Controller.maxSpeed = grappleSpeed;
                myPlayer.Controller.Move(moveDir * grappleSpeed * Runner.DeltaTime);
                myPlayer.Controller.Velocity = moveDir * grappleSpeed;
            }
            
            /*
            float horizontalDistance = Vector3.Distance(horizontalCurrent, horizontalTarget);

            //벽 껍질(콜라이더 두께)을 감안해서 평면상으로 1.5m 이내에 도달했다면? 
            // = "높이가 어떻든 간에 일단 벽에 완전히 갖다 박았다!" -> 즉시 줄 끊기!
            if (horizontalDistance <= 1.5f)
            {
                FinishPulling(); // 아래에 따로 빼둔 줄 끊기 함수 실행
                return; // 여기서 멈춤 (더 이상 당기지 않음)
            }

            // 아직 벽에 안 닿았다면 계속 당기기
            Vector3 moveDir = (PullTarget - currentPos).normalized;
            if (PullTarget.y >= currentPos.y && moveDir.y < 0f)
            {
                moveDir.y = 0f; // 바닥으로 내리꽂히는 것 방지
            }
            moveDir.Normalize();

            // 부드러운 이동
            myPlayer.Controller.Move(moveDir * grappleSpeed * Runner.DeltaTime);
            */
        }
    }
    /*
    private void FinishPulling()
    {
        IsPulling = false;
        myPlayer.isGrappling = false;

        if (CurrentNeedsVault)
        {
            // 옥상 바닥이 감지되었을 때는 위+앞으로 파쿠르!
            Vector3 lookForward = myPlayer.fpsCamera.transform.forward;
            lookForward.y = 0;
            myPlayer.Controller.Velocity = (Vector3.up * vaultUpPower) + (lookForward.normalized * vaultForwardPower);
        }
        else
        {
            // 일반 벽일 때는 부딪히고 살짝 위로 튕겨오름
            myPlayer.Controller.Velocity = Vector3.up * 4f;
        }
    }
    */

    private void OnDisable()
    {
        if (activeFlyingKunai != null)
        {
            Destroy(activeFlyingKunai);
        }
    }

    public override void Render()
    {
        if (lr == null || grappleMuzzle == null) return;

        if (!IsFiring && !IsPulling)
        {
            lr.enabled = false;
            if (activeFlyingKunai != null)
            {
                Destroy(activeFlyingKunai);

                PlayerWeapon ownerWeapon = GetComponentInParent<PlayerWeapon>();
                if (ownerWeapon != null) ownerWeapon.SetLeftHandVisualActive(true);
            }
            return;
        }

        lr.enabled = true;
        lr.SetPosition(0, grappleMuzzle.position);

        if (activeFlyingKunai == null && flyingKunaiPrefab != null)
        {
            activeFlyingKunai = Instantiate(flyingKunaiPrefab, grappleMuzzle.position, Quaternion.identity);
            activeFlyingKunai.transform.localScale = new Vector3(7f, 7f, 7f);
            PlayerWeapon ownerWeapon = GetComponentInParent<PlayerWeapon>();
            if (ownerWeapon != null) ownerWeapon.SetLeftHandVisualActive(false);
        }

        //시각 연출: 날아가던 도중 2번째 갈고리를 쏘면, 기존 줄이 사라지고 2번째 줄이 뻗어나가는 걸 보여줍니다.
        if (IsFiring)
        {
            float remainTime = HookTravelTimer.RemainingTime(Runner) ?? 0f;
            float progress = InitialTravelTime > 0f ? 1f - (remainTime / InitialTravelTime) : 1f;
            progress = Mathf.Clamp01(progress);

            Vector3 currentRopeEnd = Vector3.Lerp(grappleMuzzle.position, FireTarget, progress);
            lr.SetPosition(1, currentRopeEnd);

            if (activeFlyingKunai != null)
            {
                activeFlyingKunai.transform.position = currentRopeEnd;
                // 쿠나이가 날아가는 방향을 바라보게 회전
                if (FireTarget - grappleMuzzle.position != Vector3.zero)
                    activeFlyingKunai.transform.forward = (FireTarget - grappleMuzzle.position).normalized;
            }
        }
        else if (IsPulling)
        {
            lr.SetPosition(1, PullTarget);

            if (activeFlyingKunai != null)
            {
                activeFlyingKunai.transform.position = PullTarget;
            }
        }
    }

    public float GetRecharge()
    {
        if (GrappleRechargeTimer.IsRunning)
            return 1f - (GrappleRechargeTimer.RemainingTime(Runner) ?? 0f) / grappleRechargeTime;
        return GrappleCharges == maxGrappleCharges ? 1f : 0f;
    }

    protected override void SkillQ() { }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    void RPC_PlayNetworkSfx(GrappleSfxType type)
    {
        if (networkAudioSource == null)
            return;

        switch (type)
        {
            case GrappleSfxType.Swing: networkAudioSource.PlayOneShot(swingSfx);
                hitEffect.Play();
                break;

            case GrappleSfxType.Grapple: networkAudioSource.PlayOneShot(grappleSfx);
                break;
        }
    }
}