using Fusion;
using UnityEngine;

public abstract class WeaponBase : NetworkBehaviour
{
    [Header("무기 고유 세팅")]
    [SerializeField] public Transform firePoint;
    protected PlayerWeapon player;
    protected Player myPlayer;
    protected WeaponData baseData;
    protected RangedWeapon rangedData;

    protected Camera fpsCam;

    protected float lastEmptyAmmoTime;


    //본인 컴퓨터에서만 변화가 일어나는게 아닌 서버 전체에서 변화가 일어나게 하려 사용
    //탄약 관리
    [Networked, OnChangedRender(nameof(OnAmmoUIChanged))]
    public int CurrentAmmo {  get; set; }
    //장전 진행 상태 관리
    [Networked] public NetworkBool IsReloading { get; set; }
    [Networked] public TickTimer ReloadTimer { get; set; }
    //각 스킬 쿨타임 관리
    [Networked] public TickTimer LeftClickTimer { get; set; }
    [Networked] public TickTimer RightClickTimer { get; set; }
    [Networked] public TickTimer SkillQTimer { get; set; }
    

    //시점 고정 쿨타임
    [Networked] public TickTimer CamFixTimer { get; set; }

    protected bool HasAmmo()
    {
        if (CurrentAmmo > 0)
            return true;

        if (Time.time - lastEmptyAmmoTime > 0.3f)
        {
            lastEmptyAmmoTime = Time.time;

            if (HasInputAuthority)
                player.PlayEmptyAmmoSound();
        }

        return false;
    }

    public virtual void Init(PlayerWeapon owner, WeaponData data)
    {
        player = owner;
        baseData = data;

        //So가 rangedWeapon이라면 형변환, 아니면 rangedWeapon에 null
        rangedData = data as RangedWeapon;

        myPlayer = owner.GetComponentInParent<Player>();

        if(myPlayer != null)
        {
            fpsCam = myPlayer.fpsCamera;
        }

        if (HasStateAuthority && rangedData != null)
        {
            CurrentAmmo = rangedData.MaxAmmo;
        }

        if(HasInputAuthority)
        {
            OnAmmoUIChanged();
        }
    }

    public virtual void ProcessInput(NetworkInputData data,NetworkButtons prevButtons, NetworkRunner runner)
    {
        //장전 중이면 return - 공격불가
        if (IsReloading) return;

        CheckReload(data, prevButtons);

        //장전 시작되면 공격 못하게 return
        if (IsReloading) return;


        CheckLeftClick(data, prevButtons);
        CheckRightClick(data, prevButtons);
        CheckSkillQ(data, prevButtons);
        
    }
    //기본 클릭 세팅을 지정해두기 -> 클릭 세팅은 override로 변경
    protected virtual void CheckLeftClick(NetworkInputData data, NetworkButtons prevButtons)
    {
        if(data.buttons.IsSet(MyButtons.LeftClick)) BasicAttack();
    }

    protected virtual void CheckRightClick(NetworkInputData data, NetworkButtons prevButtons)
    {
        if (data.buttons.WasPressed(prevButtons, MyButtons.RightClick)) SecondAttack();
    }

    protected virtual void CheckSkillQ(NetworkInputData data, NetworkButtons prevButtons)
    {
        if (data.buttons.WasPressed(prevButtons, MyButtons.SkillQ)) SkillQ();
    }

    protected virtual void CheckReload(NetworkInputData data, NetworkButtons prevButtons)
    {
        if(data.buttons.WasPressed(prevButtons,MyButtons.Reload) && rangedData != null  && CurrentAmmo < rangedData.MaxAmmo)
        {
            IsReloading = true;
            ReloadTimer = TickTimer.CreateFromSeconds(Runner, rangedData.ReloadCoolTime);
            Debug.Log($"장전 시작..{rangedData.ReloadCoolTime}초 대기");
        }
    }

    //PlayerWeapon에서 FixedUpdateNetwork에서 할 일을 배정
    public virtual void OnFixedUpdateNetwork() 
    {
        //매 틱마다 장전 타이머가 끝났는지 확인
        //장전 했을때 현상, expired : 만료됐을때
        if(IsReloading && ReloadTimer.Expired(Runner))
        {
            if(HasStateAuthority) CurrentAmmo = rangedData.MaxAmmo;
            IsReloading = false;
            ReloadTimer = TickTimer.None;
            Debug.Log("장전 완료!");
        }

        if(!IsReloading && CurrentAmmo <= 0 && rangedData != null && ReloadTimer.IsRunning ==  false)
        {
            IsReloading = true;
            ReloadTimer = TickTimer.CreateFromSeconds(Runner, rangedData.ReloadCoolTime);
            Debug.Log("자동 재장전 실행");
        }
    }

    //화면 궤적 그릴 수 있는 함수 
    public virtual void OnRender() { }

    protected abstract void BasicAttack();
    protected abstract void SecondAttack();
    protected abstract void SkillQ();
    //protected abstract void Reload();


    public void OnAmmoUIChanged()
    {
        if(Object.HasInputAuthority && PlayerUI.instance != null && rangedData != null)
        {
            PlayerUI.instance.UpdateAmmoText(CurrentAmmo, rangedData.MaxAmmo);
        }
    }

}
