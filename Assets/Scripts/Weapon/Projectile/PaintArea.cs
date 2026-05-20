using Fusion;
using UnityEngine;

public class PaintArea : NetworkBehaviour
{
    [SerializeField] float lifeTime = 4f;

    [Networked]public TickTimer LifeTimer { get; set; }
    [Networked] public PlayerRef SpeedUpPlayer { get; set; }

    public override void Spawned()
    {
        if(HasStateAuthority)
        {
            LifeTimer = TickTimer.CreateFromSeconds(Runner, lifeTime);
        }
    }

    public override void FixedUpdateNetwork()
    {
        if(HasStateAuthority && LifeTimer.Expired(Runner))
        {
            Runner.Despawn(Object);
        }
    }
}
