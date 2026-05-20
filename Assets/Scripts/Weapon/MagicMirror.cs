using Fusion;
using UnityEngine;

public class MagicMirror : WeaponBase
{
    [Header("거울 세팅")]
    [SerializeField] GameObject mirrorModel;
    [SerializeField] float chargeTime = 5f;
    [SerializeField] float invisTime = 3f;

    [Networked] public NetworkBool IsHolding {  get; set; }
    [Networked] public NetworkBool IsInvisible { get; set; }
    [Networked] public TickTimer ChargeTimer { get; set; }
    [Networked] public TickTimer InvisTimer { get; set; }
    
    public override void Init(PlayerWeapon owner, WeaponData data)
    {
        base.Init(owner, data);
        if(mirrorModel != null ) mirrorModel.SetActive(false);
    }

    protected override void CheckLeftClick(NetworkInputData data, NetworkButtons prevButtons)
    {
        //누르는 순간 : 거울on 타이머 시작
        if(data.buttons.WasPressed(prevButtons, MyButtons.LeftClick))
        {
            IsHolding = true;
            ChargeTimer = TickTimer.CreateFromSeconds(Runner, chargeTime);   
        }
        //떼는 순간 : 거울 off 타이머 리셋
        else if(data.buttons.WasReleased(prevButtons, MyButtons.LeftClick))
        {
            IsHolding=false;
            ChargeTimer = TickTimer.None;
        }
    }

    protected override void CheckRightClick(NetworkInputData data, NetworkButtons prevButtons)
    {
        if(data.buttons.WasPressed(prevButtons,MyButtons.RightClick))
        {
            if(IsHolding && ChargeTimer.Expired(Runner) && !IsInvisible)
            {
                ActivateSkill();
            }
        }
    }

    public override void OnFixedUpdateNetwork()
    {
        base.OnFixedUpdateNetwork();
        //투명화 타이머 채우면 다시 풀기
        if(IsInvisible && InvisTimer.Expired(Runner))
        {
            IsInvisible = false;
            InvisTimer = TickTimer.None;
        }

        UpdateVisuals();
    }

    private void ActivateSkill()
    {
        //투명화 켜기
        IsInvisible = true;
        InvisTimer = TickTimer.CreateFromSeconds(Runner, invisTime);
        //스킬 사용시 타이머 초기화
        ChargeTimer = TickTimer.None;
    }

    private void UpdateVisuals()
    {
        if(mirrorModel != null)
        {
            mirrorModel.SetActive(IsHolding && !IsInvisible);
        }

        if(myPlayer != null)
        {
            Renderer[] renderers = myPlayer.GetComponentsInChildren<Renderer>();
            foreach(Renderer r in renderers)
            {
                if (mirrorModel != null && r.gameObject == mirrorModel) continue;

                r.enabled = !IsInvisible;
            }
        }
    }


    protected override void BasicAttack() { }
    protected override void SecondAttack() { }    
    protected override void SkillQ() { }
    
}
