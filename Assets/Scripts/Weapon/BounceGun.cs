using Fusion;
using UnityEngine;

public class BounceGun : WeaponBase
{
    [Header("발사 세팅")]
    [SerializeField] GameObject bulletPrefab;
    [SerializeField] LayerMask targetLayer;

    [Header("튕기는 발사체 세팅")]
    [SerializeField] LineRenderer trajectoryLine;
    [SerializeField] int bounceTime = 3;
    [SerializeField] int lineSegments = 30;


    private RangedWeapon rangedWeapon;
    public override void Init(PlayerWeapon owner, WeaponData data)
    {
        base.Init(owner, data);

        rangedWeapon = data as RangedWeapon;
    }

    protected override void BasicAttack()
    {
        if(CurrentAmmo >= 1 && LeftClickTimer.ExpiredOrNotRunning(Runner))
        {
            CurrentAmmo -= 1;
            SpawnProjectile(0);
            LeftClickTimer = TickTimer.CreateFromSeconds(Runner, rangedData.leftClickCoolTime);
        }
    }

    protected override void SecondAttack()
    {
        //탄약이 2발씩 사라짐
        if(CurrentAmmo >= 2)
        {
            CurrentAmmo -= 2;
            SpawnProjectile(bounceTime);
        }
    }

    private void SpawnProjectile(int bounceCount)
    {
        if(HasStateAuthority)
        {
            if (myPlayer == null || myPlayer.fpsCamera == null) return;

            Vector3 camOrigin = myPlayer.fpsCamera.transform.position;
            Vector3 camForward = myPlayer.fpsCamera.transform.forward;
            Vector3 targetPoint;

            if(Runner.GetPhysicsScene().Raycast(camOrigin, camForward, out RaycastHit hit, 
                rangedWeapon.range, targetLayer))
            {
                targetPoint = hit.point; 
            }
            else
            {
                targetPoint = camOrigin + camForward * rangedWeapon.range;
            }

            Vector3 shootDirection = (targetPoint - firePoint.position).normalized;
         
            //허공에 투사체 프리팹 소환
            NetworkObject obj = Runner.Spawn(bulletPrefab,firePoint.position, Quaternion.LookRotation(shootDirection), null);

            //소환된 투사체에게 방향과 튕길 횟수 전달
            BounceProjectile proj = obj.GetComponent<BounceProjectile>();
            proj.InitProjectile(shootDirection, bounceCount, Object.InputAuthority);

        }
    }

    /*
    public override void OnRender()
    {
        //아직 미정
        if (Input.GetMouseButton(1)) Debug.Log("우클릭 인식됨!");

        //궤적 포물선 구현
        if (HasInputAuthority && Input.GetMouseButton(1) && trajectoryLine != null)
        {
            Debug.Log("궤적 그리기 코드 실행 중!!!");
            trajectoryLine.positionCount = lineSegments;
            Vector3 currentPos = player.firePoint.position;

            Vector3 currentVel = player.firePoint.forward * 20f;
            float timeStep = 0.1f;

            for(int i = 0; i< lineSegments; i++)
            {
                trajectoryLine.SetPosition(i, currentPos);
                currentVel += Physics.gravity * timeStep;
                currentPos += currentVel * timeStep;
            }
        }
        else if(trajectoryLine != null)
        {
            //선 지우기
            trajectoryLine.positionCount = 0;
        }
    }
    */
    protected override void SkillQ() { }
   
}
