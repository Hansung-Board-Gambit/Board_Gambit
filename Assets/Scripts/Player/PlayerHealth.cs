using Fusion;
using UnityEngine;

public class PlayerHealth : NetworkBehaviour
{
    [SerializeField] public int maxHP = 100;

    [Networked, OnChangedRender(nameof(OnHPChanged))]
    public int CurrentHP {  get; set; }
    [Networked] public TickTimer StunTimer {  get; set; }

    public bool IsStunned(NetworkRunner runner)
    {
        // 타이머가 세팅된 적이 있고 && 아직 시간이 다 지나지 않았다면 true!
        return StunTimer.IsRunning && !StunTimer.Expired(runner);
    }

    public override void Spawned()
    {
        if(HasStateAuthority)
        {
            CurrentHP = maxHP;
        }

        if(HasInputAuthority && PlayerUI.instance != null)
        {
            PlayerUI.instance.UpdateHP(CurrentHP, maxHP);
        }
    }

    public void ResetForRound()
    {
        if (HasStateAuthority)
        {
            CurrentHP = maxHP;
            StunTimer = TickTimer.None;
        }

        if (HasInputAuthority && PlayerUI.instance != null)
        {
            PlayerUI.instance.UpdateHP(CurrentHP, maxHP);
        }
    }
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_TakeDamage(int damage, string attackerName)
    {
        Debug.Log($"{attackerName}의 공격! {damage} 데미지 받음!");

        int previousHP = CurrentHP;
        CurrentHP -= damage;

        if( CurrentHP <= 0 )
        {
            CurrentHP = 0;
            Debug.Log("플레이어 사망");
        }

        if (previousHP > 0 && CurrentHP <= 0)
        {
            GameRoundFlowController flow = FindFirstObjectByType<GameRoundFlowController>();
            if (flow != null)
                flow.NotifyPlayerHealthDepleted(this);
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_TakeStun(float duration)
    {
        StunTimer = TickTimer.CreateFromSeconds(Runner, duration);
        Debug.Log("기절");
    }

    public void OnHPChanged()
    {
        if (HasInputAuthority && PlayerUI.instance != null)
        {
            PlayerUI.instance.UpdateHP(CurrentHP, maxHP);
        }
    }
}
