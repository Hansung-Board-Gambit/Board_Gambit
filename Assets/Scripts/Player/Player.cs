using Fusion;
using UnityEngine;

public class Player : NetworkBehaviour
{
    [Header("카메라 세팅")]
    [SerializeField] public Camera fpsCamera;
    [SerializeField] AudioListener audioListener;

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

    //페인트 탄 관련 네트워크 변수
    [Networked] public TickTimer TrailDebuffTimer {  get; set; }
    [Networked] public Vector3 LastTrailSpawnPos { get; set; }
    [Networked] public PlayerRef MyShooter { get; set; }

    //넉백 관련 네트워크 변수
    [Networked] public TickTimer KnockbackTimer { get; set; }
    [Networked] public Vector3 KnockbackVelocity { get; set; }

    public static float NetworkedYaw = 0f;
    public static float NetworkedPitch;

    //마우스 회전 값 누적해서 저장
    private float playerYaw = 0f;
    private float camPitch = 0f;
    //카메라 위치 변화시킬 수 있게 하는 변수
    public Vector3 weaponCameraOffset = Vector3.zero;
    private bool isCameraLocked = false;
    private bool battleControlActive;
    public bool isDashing = false;

    public float currentHeight = 2f;

    Renderer rend;
    private NetworkCharacterController controller;

    public NetworkCharacterController Controller { get; private set; }

    
    private void Awake()
    {
        controller = GetComponent<NetworkCharacterController>();
        Controller = controller;
    }

    public override void Spawned()
    {
        Debug.Log("Player Spawned. InputAuthority=" + Object.InputAuthority + ", HasInputAuthority=" + HasInputAuthority + ", HasStateAuthority=" + HasStateAuthority);

        rend = GetComponent<Renderer>();
        controller = GetComponent<NetworkCharacterController>();

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

    private void Update()
    {
        if (!HasInputAuthority || !battleControlActive) return;

        bool isStunned = false;
        PlayerHealth health = GetComponent<PlayerHealth>();

        if(health != null && Runner != null)
        {
            isStunned = health.IsStunned(Runner);
        }
        if (!isCameraLocked && !isStunned)
        {

            playerYaw += Input.GetAxis("Mouse X") * mouseSens;
            camPitch -= Input.GetAxis("Mouse Y") * mouseSens;
            camPitch = Mathf.Clamp(camPitch, -90f, 90f);
            // 택배로 보낼 수 있도록 최종 yaw 각도를 갱신
            NetworkedYaw = playerYaw;
            NetworkedPitch = camPitch;
        }
        if(fpsCamera != null)
        {
            // 1인칭 카메라는 여기서 즉시 부드럽게 회전
            fpsCamera.transform.localRotation = Quaternion.Euler(camPitch, 0, 0);
        }
        
        if(Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else if(Input.GetMouseButtonDown(0))
        {
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


    public override void FixedUpdateNetwork()
    {
        if (!battleControlActive)
            return;

        /*
        if (!GetInput(out NetworkInputData data))
            return;

        Vector3 move = new Vector3(data.move.x, 0, data.move.y);

        if (move.sqrMagnitude > 1)
            move.Normalize();

        controller.Move(move * speed * Runner.DeltaTime);
        */
        if(GetComponent<PlayerHealth>().IsStunned(Runner))
        {
            return;
        }
        if(KnockbackTimer.Expired(Runner)) //시간이 다 됐는지 확인
        {
            //넉백 종료 후 브레이크 잡기
            Controller.Velocity = Vector3.zero;
            KnockbackVelocity = Vector3.zero;
            isDashing= false; //wasd 잠금 해제
            KnockbackTimer = TickTimer.None;
        }
        else if(KnockbackTimer.IsRunning) 
        {
            
            //속도 제한을 무시하고 뒤로 날림
            CollisionFlags flags = GetComponent<CharacterController>().Move(KnockbackVelocity * Runner.DeltaTime);      
            Controller.Velocity = Vector3.zero; //유령 가속도 방지

            transform.rotation = Quaternion.Euler(0,NetworkedYaw,0);

            if((flags & CollisionFlags.Sides) != 0)
            {
                KnockbackVelocity = Vector3.zero;
                KnockbackTimer = TickTimer.None;
                isDashing = false;
            }
            
            return;
        }



        if (isDashing) return;
        if (GetInput(out NetworkInputData data))
        {
            Vector3 feetPos = transform.position + Vector3.down * (standHeight / 2f);

            if(HasStateAuthority && TrailDebuffTimer.IsRunning && TrailDebuffTimer.RemainingTime(Runner) > 0)
            {
                if(Vector3.Distance(transform.position, LastTrailSpawnPos) >= trailSpawnDistance)
                {
                    Vector3 spawnPos = feetPos + Vector3.up * 0.5f;

                    NetworkObject obj = Runner.Spawn(trailPrefab, spawnPos, Quaternion.identity, Object.InputAuthority);
                    obj.GetComponent<PaintArea>().SpeedUpPlayer = MyShooter;
                    LastTrailSpawnPos = transform.position;
                }
            }
            //data.yaw는 이제 '더할 값(Delta)'이 아니라 '최종 각도(Absolute)'입니다.
            Quaternion playerRotation = Quaternion.Euler(0, data.yaw, 0);

            // 이동할 방향 계산 (이제 내가 보는 방향 기준 WASD가 완벽히 작동합니다)
            data.direction.Normalize();
            Vector3 moveDirection = playerRotation * data.direction;

            float currentSpeed;
            currentHeight = standHeight;
            
            if(data.sitDown == true)
            {
                currentSpeed = sitDownSpeed;
                controller.maxSpeed = sitDownSpeed; // 최대 속도도 조정 

                currentHeight = sitHeight;

                //추후 물리적 앉는 기능 필요, 현재는 앉는 느낌만 제공
                //Debug.Log("player sit down" + currentSpeed);
            }
            else if(data.speedUp == true)
            {
                currentSpeed = runSpeed;
                controller.maxSpeed = runSpeed;
                //Debug.Log("player is running" + currentSpeed);
            }
            else
            {
                currentSpeed = walkSpeed;
                controller.maxSpeed = walkSpeed;
                //Debug.Log("player is walking" + currentSpeed);
            }
            Collider[] hitColliders = Physics.OverlapSphere(feetPos, 0.7f, trailLayer);
            bool isOnTrail = false;

            foreach(var col in hitColliders)
            {
                PaintArea trail = col.GetComponent<PaintArea>();
                if(trail != null && trail.Object != null && trail.Object.IsValid)
                {
                    if(trail.SpeedUpPlayer == Object.InputAuthority)
                    {
                        isOnTrail = true;
                        break;
                    }
                }
            }

            if (isOnTrail)
            {
                // 흔적을 밟고 있다면 현재 속도를 뻥튀기!
                currentSpeed *= trailSpeedUp;
                controller.maxSpeed = currentSpeed;
                Debug.Log("패시브 발동! 이동 속도 증가: " + currentSpeed);
            }

            //카메라 위치
            if (HasInputAuthority)
            {
                if (fpsCamera != null) // 혹시 모를 에러 방지
                {
                    fpsCamera.transform.localPosition = new Vector3(0, currentHeight * 0.4f, 0) + weaponCameraOffset;
                }
            }
            

            // 이동 실행
            controller.Move(currentSpeed * moveDirection * Runner.DeltaTime);

            // 몸통 회전 (다른 사람들에게 내가 도는 모습을 보여주기 위함)
            transform.rotation = playerRotation;

            // 점프 처리
            if (data.jump && controller.Grounded)
            {
                controller.Jump();
            }
        }
      
    }

    public void StartPlungeCameraLock()
    {
        isCameraLocked = true;
        camPitch = 80f;

        NetworkedPitch = camPitch;

        if(fpsCamera != null)
        {
            fpsCamera.transform.localRotation = Quaternion.Euler(camPitch, 0,0);
        }
    }

    public void ReleasePlungeCameraLock()
    {
        isCameraLocked = false;
    }
}