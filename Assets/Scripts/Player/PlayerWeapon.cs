using Fusion;
using UnityEngine;

public class PlayerWeapon : NetworkBehaviour
{
    [Header("각 종 위치")]
    [SerializeField] public Camera fpsCameraObject;
    //[SerializeField] public Transform firePoint;
    [SerializeField] public Transform handPosition;
    [SerializeField] public Transform leftHandPosition;

    [Header("해당 so삽입")]
    [SerializeField] WeaponData weaponData;
    [SerializeField] WeaponData[] weaponCatalog;
    //나중에 다양한 무기들을 어떻게 교환 할 지 생각

    [Header("줌 카메라 세팅")]
    [SerializeField] Camera camrea;
    [SerializeField] public float defaultFOV = 60f;
    [SerializeField] float zoomSpeed = 10f;

    //목표 시야각
    public float TargetFOV { get; set; }

    private WeaponBase currentWeapon;
    private Player myPlayer;
    [Networked] public NetworkButtons PrevButtons {  get; set; }

    //소환한 무기를 모두에게 알려줌
    [Networked, OnChangedRender(nameof(OnWeaponIndexChanged))] 
    public NetworkObject NetWeaponObj { get; set; }

    [Networked, OnChangedRender(nameof(OnWeaponIndexChanged))]
    public int SyncWeaponIndex { get; set; } = -1;

    public GameObject currentLeftHandVisual;

    private void OnEnable()
    {
        LobbyState.PrepEquipmentAllReady += UpdateWeaponForNewRound;
    }

    private void OnDisable()
    {
        LobbyState.PrepEquipmentAllReady -= UpdateWeaponForNewRound;
    }

    private void UpdateWeaponForNewRound()
    {
        // 내 캐릭터가 맞다면, 새로 갱신된 메모장을 읽고 무기를 바꿔달라고 떼를 씁니다.
        if (HasInputAuthority)
        {
            int myNewChoice = LocalPlayerData.SelectedWeaponMasterIndex;
            RPC_RequestEquipWeapon(myNewChoice);
            Debug.Log($"<color=cyan><b>[새 라운드 시작] 새로운 무기 번호({myNewChoice})로 교체 요청!</b></color>");
        }
    }


    public override void Spawned()
    {      
        TargetFOV = defaultFOV;
        /*
        if (HasStateAuthority && weaponData != null)
        {
            EquipWeapon(weaponData);
        }

        //수동으로 붙여줌(손에다가)
        if(NetWeaponObj != null)
        {
            OnWeaponChanged();
        }
        */
        if (HasInputAuthority)
        {
            // 아까 로컬 메모장에 적어둔 번호를 꺼냅니다.
            int myChoice = LocalPlayerData.SelectedWeaponMasterIndex;

            // 내 캐릭터가 직접 서버에 무기 달라고 편지(RPC) 보냄!
            RPC_RequestEquipWeapon(myChoice);
        }

        OnWeaponIndexChanged();
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_RequestEquipWeapon(int requestIndex)
    {
        if (requestIndex >= 0 && requestIndex < weaponCatalog.Length)
        {
            SyncWeaponIndex = requestIndex; // 모두에게 공유할 변수에 세팅
            EquipWeapon(weaponCatalog[requestIndex]); // 찐으로 무기 스폰!
        }
    }
    public void EquipWeapon(WeaponData newWeaponData)
    {
        if (newWeaponData == null || newWeaponData.weaponPrefab == null)
            return;

        weaponData = newWeaponData;

        if(currentWeapon != null)
        {
            Runner.Despawn(currentWeapon.Object);
            currentWeapon = null;
        }
        else if(NetWeaponObj != null && NetWeaponObj.IsValid)
        {
            Runner.Despawn(NetWeaponObj);
        }

        NetworkObject newWeaponObj = Runner.Spawn(
            newWeaponData.weaponPrefab,
            handPosition.position,
            handPosition.rotation,
            Object.InputAuthority
        );
        //소환된 무기를 모든 클라이언트에게 알림
        NetWeaponObj = newWeaponObj;
        OnWeaponIndexChanged();

        if (myPlayer != null)
        {
            //myPlayer.isDashing = false;
            myPlayer.isGrappling = false; // 그래플링 쓰다 바꿨어도 바로 걸을 수 있게!
            myPlayer.ReleasePlungeCameraLock(); // 망치 찍다 바꿨어도 시야 돌아오게!
        }
    }

    public void SetWeaponDataAndEquip(WeaponData newWeaponData)
    {
        if (!HasStateAuthority)
            return;

        EquipWeapon(newWeaponData);
    }

    public override void Render()
    {
        if(camrea != null)
        {
            camrea.fieldOfView = Mathf.Lerp(camrea.fieldOfView, TargetFOV, Time.deltaTime * zoomSpeed);
        }

        if(currentWeapon != null)
        {
            currentWeapon.OnRender();
        }
    }

    public void OnWeaponIndexChanged()
    {
        TargetFOV = defaultFOV;

        // 1. 서버가 지정한 도감 번호를 꺼냄
        if (SyncWeaponIndex >= 0 && SyncWeaponIndex < weaponCatalog.Length)
        {
            weaponData = weaponCatalog[SyncWeaponIndex];
        }
        if (currentLeftHandVisual != null)
        {
            Destroy(currentLeftHandVisual);
            currentLeftHandVisual = null;
        }

        // 2. 무기 손에 붙이고 초기화
        if (NetWeaponObj != null)
        {
            NetWeaponObj.transform.SetParent(handPosition, false);
            NetWeaponObj.transform.localPosition = Vector3.zero;
            NetWeaponObj.transform.localRotation = Quaternion.identity;

            NetWeaponObj.transform.localScale = new Vector3(100f, 100f, 100f);

            if (weaponData != null && weaponData.leftHandVisualPrefab != null && leftHandPosition != null)
            {
                // 왼손에 가짜 껍데기를 소환하고 위치/크기를 맞추기
                currentLeftHandVisual = Instantiate(weaponData.leftHandVisualPrefab, leftHandPosition);
                currentLeftHandVisual.transform.localPosition = Vector3.zero;
                currentLeftHandVisual.transform.localRotation = Quaternion.identity;

                currentLeftHandVisual.transform.localScale = new Vector3(100f, 100f, 100f);
            }

            currentWeapon = NetWeaponObj.GetComponent<WeaponBase>();
            if (currentWeapon != null && weaponData != null)
            {
                currentWeapon.Init(this, weaponData);
                Debug.Log($"<color=green><b>[무기 장착 끝!] {weaponData.name}</b></color>");
            }
        }
    }

    public void SetLeftHandVisualActive(bool isActive)
    {
        if (currentLeftHandVisual != null)
        {
            currentLeftHandVisual.SetActive(isActive);
        }
    }
    /*
    //무기가 바뀌면 자동으로 불리는 함수
    public void OnWeaponChanged()
    {
        if(NetWeaponObj != null)
        {
            NetWeaponObj.transform.SetParent(handPosition, false);
            NetWeaponObj.transform.localPosition = Vector3.zero;
            NetWeaponObj.transform.localRotation = Quaternion.identity;

            currentWeapon = NetWeaponObj.GetComponent<WeaponBase>();
            WeaponData resolvedData = ResolveWeaponDataForCurrentWeapon();
            if (resolvedData != null)
                weaponData = resolvedData;

            if (currentWeapon != null && weaponData != null)
            {
                currentWeapon.Init(this, weaponData);
                Debug.Log($"무기 장착 완료 : {NetWeaponObj.name} / Data : {weaponData.name}");
            }
            else
            {
                Debug.LogWarning("무기 장착 실패: WeaponBase 또는 WeaponData가 없습니다. " + NetWeaponObj.name);
            }
        }
    }

    private WeaponData ResolveWeaponDataForCurrentWeapon()
    {
        if (NetWeaponObj == null)
            return weaponData;

        WeaponBase netWeapon = NetWeaponObj.GetComponent<WeaponBase>();
        if (netWeapon == null)
            return weaponData;

        if (DoesWeaponDataMatch(weaponData, netWeapon))
            return weaponData;

        if (weaponCatalog != null)
        {
            for (int i = 0; i < weaponCatalog.Length; i++)
            {
                WeaponData candidate = weaponCatalog[i];
                if (DoesWeaponDataMatch(candidate, netWeapon))
                    return candidate;
            }
        }

        PrepPhaseFlowUI prepFlow = FindFirstObjectByType<PrepPhaseFlowUI>();
        if (prepFlow == null || prepFlow.equipmentPool == null)
            return weaponData;

        for (int i = 0; i < prepFlow.equipmentPool.Length; i++)
        {
            WeaponData candidate = prepFlow.equipmentPool[i];
            if (DoesWeaponDataMatch(candidate, netWeapon))
                return candidate;
        }

        return weaponData;
    }

    private bool DoesWeaponDataMatch(WeaponData data, WeaponBase netWeapon)
    {
        if (data == null || data.weaponPrefab == null || netWeapon == null)
            return false;

        WeaponBase prefabWeapon = data.weaponPrefab.GetComponent<WeaponBase>();
        return prefabWeapon != null && prefabWeapon.GetType() == netWeapon.GetType();
    }
    */
    public override void FixedUpdateNetwork()
    {
        if (HasInputAuthority && SyncWeaponIndex != -1)
        {
            // 내 로컬 메모장 번호와, 현재 서버가 인지하는 내 무기 네트워크 번호가 다르다면?
            if (LocalPlayerData.SelectedWeaponMasterIndex != SyncWeaponIndex)
            {
                // 실시간 정정 요청을 강제로 때려 박습니다.
                RPC_RequestEquipWeapon(LocalPlayerData.SelectedWeaponMasterIndex);
            }
        }

        if (myPlayer == null)
            myPlayer = GetComponentInParent<Player>();

        if (myPlayer != null && !myPlayer.IsBattleControlActive())
            return;

        if(currentWeapon != null)
        {
            currentWeapon.OnFixedUpdateNetwork();
        }

        if(GetInput(out NetworkInputData data))
        {
            myPlayer = GetComponentInParent<Player>();
            if(myPlayer != null && myPlayer.fpsCamera != null)
            {
                myPlayer.fpsCamera.transform.localRotation = Quaternion.Euler(data.pitch,0,0);
            }
            /*
            if(firePoint != null)
            {
                firePoint.localRotation = Quaternion.Euler(data.pitch, 0, 0);
            }
            */
            if(currentWeapon != null)
            {
                currentWeapon.ProcessInput(data, PrevButtons ,Runner);
            }
            PrevButtons = data.buttons;
        }

    }

    [Rpc(RpcSources.All, RpcTargets.InputAuthority)]
    public void RPC_TakeHitLog(string attackerName)
    {
        Debug.Log($"{attackerName}한테 공격 당함");
    }
}
