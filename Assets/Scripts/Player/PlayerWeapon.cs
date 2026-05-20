using Fusion;
using UnityEngine;

public class PlayerWeapon : NetworkBehaviour
{
    [Header("각 종 위치")]
    [SerializeField] public Camera fpsCameraObject;
    //[SerializeField] public Transform firePoint;
    [SerializeField] public Transform handPosition;

    [Header("해당 so삽입")]
    [SerializeField] WeaponData weaponData;
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
    [Networked, OnChangedRender(nameof(OnWeaponChanged))] 
    public NetworkObject NetWeaponObj { get; set; }
      

    //추가 및 변경해야 할 부분
    //키보드 키를 누를때 해당 무기가 나올 수 있게끔
    //플레이어1과 2가 각각 다른 무기를 소환시킬 수 있게

    public override void Spawned()
    {      
        TargetFOV = defaultFOV;

        if (HasStateAuthority && weaponData != null)
        {
            EquipWeapon(weaponData);
        }

        //수동으로 붙여줌(손에다가)
        if(NetWeaponObj != null)
        {
            OnWeaponChanged();
        }
    }
    public void EquipWeapon(WeaponData newWeaponData)
    {
        if(currentWeapon != null)
        {
            Runner.Despawn(currentWeapon.Object);
        }
        NetworkObject newWeaponObj = Runner.Spawn(
            newWeaponData.weaponPrefab,
            handPosition.position,
            handPosition.rotation,
            Object.InputAuthority
        );
        //소환된 무기를 모든 클라이언트에게 알림
        NetWeaponObj = newWeaponObj;
       
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

    //무기가 바뀌면 자동으로 불리는 함수
    public void OnWeaponChanged()
    {
        if(NetWeaponObj != null)
        {
            NetWeaponObj.transform.SetParent(handPosition, false);
            NetWeaponObj.transform.localPosition = Vector3.zero;
            NetWeaponObj.transform.localRotation = Quaternion.identity;

            currentWeapon = NetWeaponObj.GetComponent<WeaponBase>();
            currentWeapon.Init(this, weaponData);

            Debug.Log($"무기 장착 완료 : {NetWeaponObj.name}");
        }
    }


    public override void FixedUpdateNetwork()
    {
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
