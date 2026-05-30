using Fusion;
using UnityEngine;

public class Player : NetworkBehaviour
{
    [Header("카메라 세팅")]
    [SerializeField] public Camera fpsCamera;
    [SerializeField] AudioListener audioListener;
    [SerializeField] Animator characterAnimator;

    [Header("카메라 감도 설정")]
    //[SerializeField] float speed = 6f;
    //[SerializeField] float jumpForce = 7f;
    [SerializeField] float mouseSens = 2f;
    [Header("플레이어 이동 설정")]
    [SerializeField] float walkSpeed = 5f;
    [SerializeField] float runSpeed = 7.5f;
    [SerializeField] float sitDownSpeed = 2f;
    [Header("앉기 관련 설정")] //앉기 관련 설정 나중에 다시 조정
    [SerializeField] float standHeight = 2f;
    [SerializeField] float sitHeight = 1f;
    [Header("페인트 총 관련 패시브/디버프 설정")]
    [SerializeField] GameObject trailPrefab;
    [SerializeField] LayerMask trailLayer;
    [SerializeField] float trailSpeedUp = 10f;
    [SerializeField] float trailSpawnDistance = 1f;
    [Header("건틀릿 날아가는 시간")]
    [SerializeField] public float flyingTime = 0.25f;
    [Header("사운드")]
    [SerializeField] AudioSource localAudioSource;
    [SerializeField] AudioClip footstepSfx;
    [SerializeField] AudioClip trailEnterSfx;
    [SerializeField] AudioClip jumpSfx;

    [SerializeField] float footstepDistance = 1.3f;

    private Vector3 lastFootstepPosition;
    private bool wasOnTrail;

    //페인트 탄 관련 네트워크 변수
    [Networked] public TickTimer TrailDebuffTimer {  get; set; }
    [Networked] public Vector3 LastTrailSpawnPos { get; set; }
    [Networked] public PlayerRef MyShooter { get; set; }

    //넉백 관련 네트워크 변수
    [Networked] public TickTimer KnockbackTimer { get; set; }
    [Networked] public Vector3 KnockbackVelocity { get; set; }
    [Networked] private float AnimSpeed { get; set; }
    [Networked] private float AnimMoveZ { get; set; }
    [Networked] private NetworkBool AnimIsRunning { get; set; }
    [Networked] private NetworkBool AnimIsCrouching { get; set; }
    [Networked] private int AnimJumpCount { get; set; }

    public static float NetworkedYaw = 0f;
    public static float NetworkedPitch;

    //마우스 회전 값 누적해서 저장
    private float playerYaw = 0f;
    private float camPitch = 0f;
    //카메라 위치 변화시킬 수 있게 하는 변수
    public Vector3 weaponCameraOffset = Vector3.zero;
    private bool isCameraLocked = false;
    private bool battleControlActive;
    public bool isDashing {  get; set; }
    [Networked] public bool isGrappling { get; set; }

    public float currentHeight = 2f;

    Renderer rend;
    private NetworkCharacterController controller;
    private int lastRenderedJumpCount;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int MoveZHash = Animator.StringToHash("MoveZ");
    private static readonly int IsRunningHash = Animator.StringToHash("IsRunning");
    private static readonly int IsCrouchingHash = Animator.StringToHash("IsCrouching");
    private static readonly int JumpHash = Animator.StringToHash("Jump");

    public NetworkCharacterController Controller { get; private set; }

    
    private void Awake()
    {
        controller = GetComponent<NetworkCharacterController>();
        Controller = controller;

        if (characterAnimator == null)
            characterAnimator = GetComponentInChildren<Animator>(true);
    }

    public override void Spawned()
    {
        Debug.Log("Player Spawned. InputAuthority=" + Object.InputAuthority + ", HasInputAuthority=" + HasInputAuthority + ", HasStateAuthority=" + HasStateAuthority);

        rend = GetComponent<Renderer>();
        controller = GetComponent<NetworkCharacterController>();
        if (characterAnimator == null)
            characterAnimator = GetComponentInChildren<Animator>(true);

        lastFootstepPosition = transform.position;

        // Host 색상
        if (Object.InputAuthority.PlayerId == 1)
        {
            rend.material.color = Color.red;
        }
        else
        {
            int id = Object.InputAuthority.PlayerId % 3;

            if (id == 0)
                rend.material.color = Color.blue;
            else if (id == 1)
                rend.material.color = Color.yellow;
            else
                rend.material.color = Color.green;
        }

        SetBattleControlActive(ShouldStartVisibleInBattle());
    }

    public override void Render()
    {
        UpdateAnimatorParameters();
    }

    private bool ShouldStartVisibleInBattle()
    {
        GameRoundFlowController flow = FindFirstObjectByType<GameRoundFlowController>();
        return flow != null && flow.CurrentPhase == GameRoundPhase.Battle;
    }
    public bool IsBattleControlActive()
    {
        return battleControlActive;
    }

    public void SetBattleControlActive(bool active)
    {
        battleControlActive = active;
        SetBattlePresentationActive(active);

        if (!HasInputAuthority)
        {
            if (fpsCamera != null) fpsCamera.enabled = false;
            if (audioListener != null) audioListener.enabled = false;
            return;
        }

        if (fpsCamera != null) fpsCamera.enabled = active;
        if (audioListener != null) audioListener.enabled = active;

        Cursor.lockState = active ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !active;

        if (active)
        {
            playerYaw = transform.eulerAngles.y;
            NetworkedYaw = playerYaw;
        }
    }

    private void SetBattlePresentationActive(bool active)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
            renderers[i].enabled = active;

        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
            colliders[i].enabled = active;
    }

    public void TeleportForBattle(Vector3 position, Quaternion rotation)
    {
        KnockbackTimer = TickTimer.None;
        TrailDebuffTimer = TickTimer.None;
        KnockbackVelocity = Vector3.zero;
        isDashing = false;
        currentHeight = standHeight;
        weaponCameraOffset = Vector3.zero;

        if (controller != null)
        {
            controller.Velocity = Vector3.zero;
            controller.Teleport(position, rotation);
        }
        else
        {
            transform.SetPositionAndRotation(position, rotation);
        }
    }
    private void OnApplicationFocus(bool hasFocus)
    {
        // 내 캐릭터가 맞고, 지금 배틀 중인데, 게임 창을 클릭했다면?
        if (hasFocus && HasInputAuthority && battleControlActive)
        {
            // 도망간 마우스 커서를 강제로 다시 잡아옵니다.
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }


    //무기 맞았을때 실행할 디버프 부여 함수
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_ApplyTrailDebuff(float duration, PlayerRef shooter)
    {
        TrailDebuffTimer = TickTimer.CreateFromSeconds(Runner, duration);
        LastTrailSpawnPos = transform.position; 
        MyShooter = shooter;
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_ApplyKnockback(Vector3 force, float duration)
    {
        KnockbackVelocity = force;
        //날라가는 시간
        KnockbackTimer = TickTimer.CreateFromSeconds(Runner, duration);
        isDashing = true;
    }


    // ========================================================
    // 1. Update() : 내 화면에서만 카메라와 몸통을 144Hz로 아주 부드럽게 덮어씌웁니다!
    // ========================================================
    private void Update()
    {
        if (!HasInputAuthority) return;
        if (!battleControlActive && ShouldStartVisibleInBattle())
        {
            SetBattleControlActive(true);
        }
        if (!battleControlActive) return;

        bool isStunned = false;
        PlayerHealth health = GetComponent<PlayerHealth>();

        if (health != null && Runner != null)
        {
            isStunned = health.IsStunned(Runner);
        }

        if (!isCameraLocked && !isStunned)
        {
            playerYaw += Input.GetAxis("Mouse X") * mouseSens;
            camPitch -= Input.GetAxis("Mouse Y") * mouseSens;
            camPitch = Mathf.Clamp(camPitch, -90f, 90f);

            // 네트워크 전송을 위해 저장
            NetworkedYaw = playerYaw;
            NetworkedPitch = camPitch;
        }

        // 핵심 방어막: 내 화면(Local)에서는 60Hz로 뚝뚝 끊기는 FUN의 회전을 무시하고, 
        // 매 모니터 프레임마다 몸통(Y축)과 카메라(X축)를 최고로 부드럽게 실시간 회전시킵니다!
        //transform.rotation = Quaternion.Euler(0, playerYaw, 0);

        if (fpsCamera != null)
        {
            fpsCamera.transform.localRotation = Quaternion.Euler(camPitch, 0, 0);
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else if (Input.GetMouseButtonDown(0))
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    // ========================================================
    // 2. FixedUpdateNetwork() : 물리 이동과 서버 동기화만 완벽하게 처리!
    // ========================================================
    public override void FixedUpdateNetwork()
    {
        if (!battleControlActive)
        {
            SetAnimationState(0f, 0f, false, false);
            return;
        }

        if (GetComponent<PlayerHealth>().IsStunned(Runner))
        {
            SetAnimationState(0f, 0f, false, false);
            return;
        }

        // --- 넉백 로직 (원래 코드 그대로) ---
        if (KnockbackTimer.Expired(Runner))
        {
            Controller.Velocity = Vector3.zero;
            KnockbackVelocity = Vector3.zero;
            isDashing = false;
            KnockbackTimer = TickTimer.None;
        }
        else if (KnockbackTimer.IsRunning)
        {
            SetAnimationState(0f, 0f, false, false);
            CollisionFlags flags = GetComponent<CharacterController>().Move(KnockbackVelocity * Runner.DeltaTime);
            Controller.Velocity = Vector3.zero;

            transform.rotation = Quaternion.Euler(0, NetworkedYaw, 0);

            if ((flags & CollisionFlags.Sides) != 0)
            {
                KnockbackVelocity = Vector3.zero;
                KnockbackTimer = TickTimer.None;
                isDashing = false;
            }
            return;
        }

        // 대쉬 중일 때는 통제권 상실
        if (isDashing)
        {
            SetAnimationState(0f, 0f, false, false);
            return;
        }

        if (GetInput(out NetworkInputData data))
        {
            // 1. 회전값 계산만 미리 해둡니다. (적용은 맨 마지막에!)
            Quaternion playerRotation = Quaternion.Euler(0, data.yaw, 0);

            // 2. 카메라 위치는 무조건 갱신해 줍니다.
            if (HasInputAuthority)
            {
                if (fpsCamera != null)
                {
                    fpsCamera.transform.localPosition = new Vector3(0, currentHeight * 0.4f, 0) + weaponCameraOffset;
                }
            }

            //3. 그래플링 중이 아닐 때만 WASD 물리 이동 실행!
            if (!isGrappling)
            {
                Vector3 feetPos = transform.position + Vector3.down * (standHeight / 2f);

                // --- 패시브 트레일 생성 ---
                if (HasStateAuthority && TrailDebuffTimer.IsRunning && TrailDebuffTimer.RemainingTime(Runner) > 0)
                {
                    if (Vector3.Distance(transform.position, LastTrailSpawnPos) >= trailSpawnDistance)
                    {
                        Vector3 spawnPos = feetPos + Vector3.up * 0.5f;
                        int floorMask = ~(LayerMask.GetMask("Player") | trailLayer.value);

                        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 100f, floorMask, QueryTriggerInteraction.Ignore))
                        {
                            // 레이저가 진짜 바닥(지형)에 닿았다면?
                            // 바닥 좌표(hit.point)를 가져오고, 바닥에 파묻히지 않게 아주 살짝(0.05) 위로 띄워줍니다.
                            spawnPos = hit.point + (Vector3.up * 0.05f);
                        }
                        else
                        {
                            // 만약 100m 아래에도 땅이 없다면 (맵 밖으로 떨어지는 중 등)
                            spawnPos = feetPos; // 허공에 붕 뜨는 오프셋 제거
                        }


                        NetworkObject obj = Runner.Spawn(trailPrefab, spawnPos, Quaternion.identity, Object.InputAuthority);
                        obj.GetComponent<PaintArea>().SpeedUpPlayer = MyShooter;
                        LastTrailSpawnPos = transform.position;
                    }
                }

                // 이동할 방향 계산
                bool isMoving = data.direction.sqrMagnitude > 0.0001f;
                float moveZ = isMoving ? Mathf.Clamp(data.direction.z, -1f, 1f) : 0f;
                data.direction.Normalize();
                Vector3 moveDirection = playerRotation * data.direction;

                float currentSpeed;
                currentHeight = standHeight;

                if (data.sitDown == true)
                {
                    currentSpeed = sitDownSpeed;
                    controller.maxSpeed = sitDownSpeed;
                    currentHeight = sitHeight;
                }
                else if (data.speedUp == true)
                {
                    currentSpeed = runSpeed;
                    controller.maxSpeed = runSpeed;
                }
                else
                {
                    currentSpeed = walkSpeed;
                    controller.maxSpeed = walkSpeed;
                }

                // --- 패시브 트레일 밟기 ---
                Collider[] hitColliders = Physics.OverlapSphere(feetPos, 0.7f, trailLayer);
                bool isOnTrail = false;

                foreach (var col in hitColliders)
                {
                    PaintArea trail = col.GetComponent<PaintArea>();
                    if (trail != null && trail.Object != null && trail.Object.IsValid)
                    {
                        if (trail.SpeedUpPlayer == Object.InputAuthority)
                        {
                            isOnTrail = true;
                            break;
                        }
                    }
                }

                if (isOnTrail && !wasOnTrail)
                {
                    if (HasInputAuthority)
                    {
                        localAudioSource.PlayOneShot(trailEnterSfx);
                    }
                }

                wasOnTrail = isOnTrail;

                if (isOnTrail)
                {
                    currentSpeed *= trailSpeedUp;
                    controller.maxSpeed = currentSpeed;
                }

                // 최종 물리 이동 실행 (반드시 회전보다 먼저 일어나야 합니다!)
                controller.Move(currentSpeed * moveDirection * Runner.DeltaTime);
                SetAnimationState(isMoving ? 1f : 0f, moveZ, data.speedUp && isMoving, data.sitDown);

                if (HasInputAuthority && controller.Grounded && moveDirection.sqrMagnitude > 0.01f)
                {
                    float movedDistance = Vector3.Distance(transform.position, lastFootstepPosition);

                    if (movedDistance >= footstepDistance)
                    {
                        localAudioSource.PlayOneShot(footstepSfx);
                        lastFootstepPosition = transform.position;
                    }
                }
                else
                {
                    lastFootstepPosition = transform.position;
                }

                // 점프 처리
                if (data.jump && controller.Grounded)
                {
                    controller.Jump();
                    AnimJumpCount++;

                    if (HasInputAuthority)
                    {
                        localAudioSource.PlayOneShot(jumpSfx);
                    }
                }
            }
            else
            {
                SetAnimationState(0f, 0f, false, data.sitDown);
            }

            //4. 몸통 회전 (유저님의 오리지널 코드처럼 무조건 Move 다음에 와야 화면이 안 튕깁니다!)
            transform.rotation = playerRotation;
        }
    }

    private void SetAnimationState(float speed, float moveZ, bool isRunning, bool isCrouching)
    {
        AnimSpeed = speed;
        AnimMoveZ = moveZ;
        AnimIsRunning = isRunning;
        AnimIsCrouching = isCrouching;
    }

    private void UpdateAnimatorParameters()
    {
        if (characterAnimator == null)
            return;

        characterAnimator.SetFloat(SpeedHash, AnimSpeed);
        characterAnimator.SetFloat(MoveZHash, AnimMoveZ);
        characterAnimator.SetBool(IsRunningHash, AnimIsRunning);
        characterAnimator.SetBool(IsCrouchingHash, AnimIsCrouching);

        if (lastRenderedJumpCount != AnimJumpCount)
        {
            lastRenderedJumpCount = AnimJumpCount;
            characterAnimator.SetTrigger(JumpHash);
        }
    }

    public void StartPlungeCameraLock()
    {
        isCameraLocked = true;
    }

    public void ReleasePlungeCameraLock()
    {
        isCameraLocked = false;
    }

    public void ApplyJumpPadForce(float force)
    {
        // 퓨전 네트워크 동기화: 내 화면이거나 서버일 때만 튕겨오름을 적용
        if (HasStateAuthority || HasInputAuthority)
        {
            if (Controller != null)
            {              
                Controller.Velocity = new Vector3(Controller.Velocity.x, force, Controller.Velocity.z);
            }
        }
    }
}
