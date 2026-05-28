using Fusion;
using UnityEngine;

public class JumpingObject : NetworkBehaviour
{
    [Header("점프힘 설정")]
    [SerializeField] float jumpForce = 20f;

    private void OnTriggerEnter(Collider other)
    {
        Player player = other.GetComponent<Player>();

        if(player != null )
        {
            player.ApplyJumpPadForce(jumpForce);
        }
    }
}
