using Fusion;
using UnityEngine;

public class FlameProjectile : NetworkBehaviour
{
    [SerializeField] float speed = 15f;
    [SerializeField] GameObject flameAreaPrefab;
    [SerializeField] LayerMask hitLayer;

    [Networked] public Vector3 Velocity { get; set; }
    [Networked] public PlayerRef Shooter { get; set; }

    public void InitProjectile(Vector3 dir, PlayerRef shooter)
    {
        Velocity = dir * speed;
        Shooter = shooter;
    }

    public override void FixedUpdateNetwork()
    {
        Velocity += Physics.gravity * Runner.DeltaTime;
        Vector3 displacement = Velocity * Runner.DeltaTime;

        if(Runner.LagCompensation.Raycast(transform.position, Velocity.normalized, 
            displacement.magnitude, Shooter, out var hit, hitLayer, HitOptions.IncludePhysX))
        {
            if(hit.Hitbox != null)
            {
                NetworkObject hitNetObj = hit.Hitbox.Root.GetComponent<NetworkObject>();
                if (hitNetObj != null && hitNetObj.InputAuthority == Shooter) { }
                else
                {
                    CreateFlameArea(hit.Point, hit.Normal);
                    return;
                }
            }
            else if(hit.Collider != null)
            {
                NetworkObject colNetObj = hit.Collider.GetComponent<NetworkObject>();
                if(colNetObj != null && colNetObj.InputAuthority == Shooter) { }
                else
                {
                    CreateFlameArea(hit.Point, hit.Normal);
                    return;
                }
            }
        }
        transform.position += displacement;
    }

    private void CreateFlameArea(Vector3 hitPoint, Vector3 hitNormal)
    {
        if(HasStateAuthority)
        {
            Vector3 spawnPos = hitPoint;
            Vector3 floorNormal = hitNormal;

            if(Vector3.Angle(Vector3.up, hitNormal) > 45f)
            {
                if(Physics.Raycast(hitPoint, Vector3.down, out RaycastHit floorHit
                    , 10f, LayerMask.GetMask("Default")))
                {
                    spawnPos = floorHit.point;
                    floorNormal = floorHit.normal;
                }
                    

            }
            spawnPos += floorNormal * 0.05f;

            Quaternion flatRotation =  Quaternion.FromToRotation(Vector3.up, floorNormal);

            Runner.Spawn(flameAreaPrefab, spawnPos, flatRotation, Object.InputAuthority);
            Runner.Despawn(Object);
        }

    }
}



