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
    [Networked] public TickTimer StunTimer {  get; set; }
    private Coroutine flameLoopRoutine;
    private bool isPlaying;
    private float flameTimer;

    private Renderer[] renderers;


    private IEnumerator FlameWait()
    {
        yield return new WaitForSeconds(flameClip.length);

        isPlaying = false;
    }

    private IEnumerator HitFlashRoutine()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);

        foreach (var r in renderers)
        {
            if (r != null)
                r.enabled = false;
        }

        yield return new WaitForSeconds(0.05f);

        if (this == null || !gameObject)
            yield break;

        foreach (var r in renderers)
        {
            if (r != null)
                r.enabled = true;
        }
    }

    public void SetFlameHit()
    {
        flameTimer = 0.25f;
    }

    private void Update()
    {
        if (!HasInputAuthority) return;

        flameTimer -= Time.deltaTime;

        if (flameTimer > 0f)
        {
            if (!hitConfirmAudioSource.isPlaying)
            {
                hitConfirmAudioSource.clip = flameClip;
                hitConfirmAudioSource.loop = true;
                hitConfirmAudioSource.Play();
            }
        }
        else
        {
            if (hitConfirmAudioSource.isPlaying && hitConfirmAudioSource.clip == flameClip)
            {
                hitConfirmAudioSource.Stop();
                hitConfirmAudioSource.loop = false;
                hitConfirmAudioSource.clip = null;
            }
        }
    }

    public bool IsStunned(NetworkRunner runner)
    {
        // 타이머가 세팅된 적이 있고 && 아직 시간이 다 지나지 않았다면 true!
        return StunTimer.IsRunning && !StunTimer.Expired(runner);
    }

    public override void Spawned()
    {
        renderers = GetComponentsInChildren<Renderer>(true);

        if (HasStateAuthority)
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

        flameTimer = 0f;

        if (hitConfirmAudioSource != null)
        {
            hitConfirmAudioSource.Stop();
            hitConfirmAudioSource.loop = false;
            hitConfirmAudioSource.clip = null;
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
        RPC_PlayHitFlash();

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
    private void RPC_PlayHitFlash()
    {
        StartCoroutine(HitFlashRoutine());
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
