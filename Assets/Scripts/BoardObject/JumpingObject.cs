using Fusion;
using UnityEngine;

public class JumpingObject : NetworkBehaviour
{
    [Header("점프힘 설정")]
    [SerializeField] float jumpForce = 20f;

    [Header("사운드")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip jumpPadSfx;

    public override void Spawned()
    {
        Debug.Log("JumpPad Spawned!");
    }

    private void OnTriggerEnter(Collider other)
    {
        Player player = other.GetComponent<Player>();

        if(player != null )
        {
            player.ApplyJumpPadForce(jumpForce);
            
            RPC_PlayJumpPadSfx();
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayJumpPadSfx()
    {
        audioSource.PlayOneShot(jumpPadSfx);
    }
}
