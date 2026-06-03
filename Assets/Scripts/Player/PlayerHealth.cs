using Fusion;
using UnityEngine;
using System.Collections;

public class PlayerHealth : NetworkBehaviour
{
    [SerializeField] private AudioSource hitConfirmAudioSource;
    [SerializeField] private AudioClip hitConfirmSfx;
    [SerializeField] private AudioClip flameClip;

    [SerializeField] public int maxHP = 100;

    [Networked, OnChangedRender(nameof(OnHPChanged))]
    public int CurrentHP {  get; set; }
    public bool IsInFlameArea { get; private set; }
    [Networked] public TickTimer StunTimer {  get; set; }
    private Coroutine flameLoopRoutine;
    private float flameTimer;
    private const float flameTimeout = 0.3f;

    private IEnumerator FlameLoop()
    {
        while (true)
        {
            hitConfirmAudioSource.PlayOneShot(flameClip);

            // 클립 끝날 때까지 대기
            yield return new WaitForSeconds(flameClip.length);
        }
    }

    public void StartFlameSfx()
    {
        if (!HasInputAuthority) return;

        if (flameLoopRoutine != null)
            return;

        flameLoopRoutine = StartCoroutine(FlameLoop());
    }

    public void StopFlameSfx()
    {
        if (!HasInputAuthority) return;

        if (flameLoopRoutine != null)
        {
            StopCoroutine(flameLoopRoutine);
            flameLoopRoutine = null;
        }
    }

    private void Update()
    {
        if (!HasInputAuthority) return;

        flameTimer -= Time.deltaTime;

        if (flameTimer <= 0f)
        {
            StopFlameSfx();
        }
    }

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
    public void RPC_TakeDamage(int damage, string attackerName, PlayerRef attacker)
    {
        Debug.Log($"{attackerName}의 공격! {damage} 데미지 받음!");

        int previousHP = CurrentHP;
        CurrentHP -= damage;
        RPC_PlayHitConfirm(attacker);

        if ( CurrentHP <= 0 )
        {
            CurrentHP = 0;
            if (HasInputAuthority && PlayerUI.instance != null)
            {
                PlayerUI.instance.UpdateHP(0, maxHP);
            }
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

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_Heal(int amount)
    {
        if (CurrentHP <= 0) return;
        CurrentHP += amount;
        if(CurrentHP > maxHP)
        {
            CurrentHP = maxHP;
        }

    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayHitConfirm(PlayerRef attacker)
    {
        if (Runner.LocalPlayer != attacker)
            return;

        if (hitConfirmAudioSource != null && hitConfirmSfx != null)
            hitConfirmAudioSource.PlayOneShot(hitConfirmSfx);
    }

    public void OnHPChanged()
    {
        if (HasInputAuthority && PlayerUI.instance != null)
        {
            PlayerUI.instance.UpdateHP(CurrentHP, maxHP);
        }
    }
}
