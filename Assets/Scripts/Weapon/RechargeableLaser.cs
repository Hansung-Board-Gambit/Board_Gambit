using Fusion;
using UnityEngine;

public class RechargeableLaser : WeaponBase
{
    [SerializeField] float zoomFOV = 20f;

    [Header("차징 세팅")]
    [SerializeField] float chargeSpeed = 50f;
    [SerializeField] float dischargeSpeed = 35f;
    [SerializeField] LayerMask targetLayer;

    [Header("사운드")]
    [SerializeField] AudioSource localAudioSource;     // 2D
    [SerializeField] AudioSource networkAudioSource;   // 3D
    [SerializeField] AudioClip chargeStartSfx; // 좌클릭 홀드 시작 (로컬)
    [SerializeField] AudioClip fullChargeSfx;  // 완충 (로컬)
    [SerializeField] AudioClip zoomInSfx;      // 줌 인 (로컬)
    [SerializeField] AudioClip zoomOutSfx;     // 줌 아웃 (로컬)
    [SerializeField] AudioClip shootSfx;       // 발사 (네트워크 공유)

    [Header("시각 효과")]
    [SerializeField] ParticleSystem chargeParticle;
    [SerializeField] ParticleSystem muzzleEffect;
    [SerializeField] float maxParticleScale = 2f;

    [Header("레이저 빔 효과")]
    [SerializeField] LineRenderer laserLine;
    [SerializeField] Transform laserMuzzle;
    [SerializeField] float laserFadeDuration = 0.5f;

    private Coroutine laserFadeCoroutine;

    private RangedWeapon rangedWeapon;

    private bool isPressing;
    private bool wasFullyCharged;

    //현재 게이지
    [Networked, OnChangedRender(nameof(OnGageChanged))]
    public float currentGage {  get; set; }


    public enum LaserSfxType
    {
        Shoot
    }

    public override void Init(PlayerWeapon owner, WeaponData data)
    {
        base.Init(owner, data);

        rangedWeapon = data as RangedWeapon;

        if (chargeParticle != null)
        {
            chargeParticle.Stop();
            chargeParticle.transform.localScale = Vector3.zero;
        }
    }

    protected override void CheckLeftClick(NetworkInputData data, NetworkButtons prevButtons)
    {
        if (data.buttons.WasPressed(prevButtons, MyButtons.LeftClick))
        {
            if (HasInputAuthority)
            {
                PlayLocalSfx(chargeStartSfx);
            }
        }

        //좌클릭이 눌리고 있을때 true
        isPressing = data.buttons.IsSet(MyButtons.LeftClick);
       
        if (data.buttons.WasReleased(prevButtons, MyButtons.LeftClick))
        {
            //만약 총알이 다 채워졌다면 발사가능           
            if(currentGage >= rangedWeapon.MaxAmmo)
            {
                BasicAttack();
                currentGage = 0;
            }

            wasFullyCharged = false;
        }
    }

    protected override void CheckRightClick(NetworkInputData data, NetworkButtons prevButtons)
    {
        if(data.buttons.WasPressed(prevButtons, MyButtons.RightClick))
        {
            //줌 확대됨
            player.TargetFOV = zoomFOV;
            if (HasInputAuthority)
            {
                PlayLocalSfx(zoomInSfx);
            }
        }

        // 누르고 있는 동안은 FOV만 유지
        if (data.buttons.IsSet(MyButtons.RightClick))
        {
            player.TargetFOV = zoomFOV;
        }

        // 줌 해제 순간 1번만
        if (data.buttons.WasReleased(prevButtons, MyButtons.RightClick))
        {
            player.TargetFOV = player.defaultFOV;

            if (HasInputAuthority)
            {
                PlayLocalSfx(zoomOutSfx);
            }
        }
    }

    //게이지 증가 및 감소(총알)
    public override void OnFixedUpdateNetwork()
    {
      
        Debug.Log($"현재 게이지 : {currentGage.ToString("F0")}");

        if(isPressing)
        {
            currentGage += chargeSpeed * Runner.DeltaTime;
            if(currentGage > rangedWeapon.MaxAmmo) currentGage = rangedWeapon.MaxAmmo;

            if (!wasFullyCharged && currentGage >= rangedWeapon.MaxAmmo)
            {
                wasFullyCharged = true;

                if (HasInputAuthority)
                {
                    PlayLocalSfx(fullChargeSfx);
                }
            }
        }
        else
        {
            currentGage -= dischargeSpeed * Runner.DeltaTime;
            if (currentGage < 0) currentGage = 0;
        }
        //int displayGage = Mathf.FloorToInt(currentGage); - 실제 정수로 변환
        CurrentAmmo = Mathf.FloorToInt(currentGage);
    }

    protected override void BasicAttack()
    {
        if (!Object.HasStateAuthority)
            return;

        RPC_PlayNetworkSfx(LaserSfxType.Shoot);
        Shoot();
    }    
    protected override void SecondAttack() { }
    protected override void SkillQ() { }

    private void Shoot()
    {
        if (myPlayer == null || myPlayer.fpsCamera == null) return;

        Vector3 origin = myPlayer.fpsCamera.transform.position;
        Vector3 direction = myPlayer.fpsCamera.transform.forward;

        Vector3 endPoint = origin + (direction * rangedWeapon.range);

        Debug.DrawRay(origin, direction * rangedWeapon.range, Color.red, 2f);

        var hits = new System.Collections.Generic.List<LagCompensatedHit>();

        int hitCount = Runner.LagCompensation.RaycastAll(
            origin,
            direction,
            rangedWeapon.range,
            Object.InputAuthority,
            hits,
            layerMask: targetLayer,
            options: HitOptions.IncludePhysX
        );

        if (hitCount <= 0)
        {
            Debug.Log("2. 허공에 빗나감");
            return;
        }
        if (hitCount > 0)
        {
            //무언가 맞았을때, 끝 지점 변경 
            endPoint = hits[0].Point;
        }

        System.Collections.Generic.HashSet<GameObject> hitPlayers = new System.Collections.Generic.HashSet<GameObject>();
        System.Collections.Generic.HashSet<ExplosiveBarrel> hitBarrels = new System.Collections.Generic.HashSet<ExplosiveBarrel>();

        foreach (var hit in hits)
        {
            GameObject target = null;
            if (hit.Hitbox != null) target = hit.Hitbox.Root.gameObject;
            else if (hit.Collider != null) target = hit.Collider.gameObject;

            if (target == null || target == myPlayer.gameObject) continue;


            if (hit.Hitbox != null)
            {
                PlayerHealth targetHP = target.GetComponent<PlayerHealth>();
                if (targetHP == null) targetHP = target.GetComponentInParent<PlayerHealth>();

                if (targetHP != null && !hitPlayers.Contains(targetHP.gameObject))
                {
                    hitPlayers.Add(targetHP.gameObject); 

                    if (HasStateAuthority)
                    {
                        targetHP.RPC_TakeDamage(rangedWeapon.damage, myPlayer.gameObject.name, Object.InputAuthority);

                        PlayerWeapon targetWeapon = targetHP.GetComponent<PlayerWeapon>();
                        if (targetWeapon != null)
                        {
                            targetWeapon.RPC_TakeHitLog(myPlayer.gameObject.name);
                        }
                    }
                }
            }

            bool isBarrelTag = target.CompareTag("fireBarrel") || (hit.Collider != null && hit.Collider.CompareTag("fireBarrel"));
            ExplosiveBarrel barrel = target.GetComponentInParent<ExplosiveBarrel>();

            if (barrel == null && hit.Collider != null) barrel = hit.Collider.GetComponentInParent<ExplosiveBarrel>();

            if (isBarrelTag || barrel != null)
            {
                if (barrel == null) barrel = target.GetComponentInChildren<ExplosiveBarrel>();
                if (barrel == null && hit.Collider != null) barrel = hit.Collider.GetComponentInChildren<ExplosiveBarrel>();

                if (barrel != null && !hitBarrels.Contains(barrel))
                {
                    hitBarrels.Add(barrel); 

                    if (HasStateAuthority)
                    {
                        barrel.RPC_TakeDamageBarrel(rangedWeapon.damage, myPlayer.gameObject.name);
                    }
                }
            }
        }
        RPC_DrawLaser(laserMuzzle.position, endPoint);
    }

    /*
    if(hit.Hitbox != null)
    {
        GameObject target = hit.Hitbox.Root.gameObject;
        Debug.Log($"4. 명중! 대상: {target.name}");

        if (target == myPlayer.gameObject)
        {
            Debug.Log("내가 나를 맞춤 (자해 방지)");
            return;
        }
        //맞췄을때 본인 화면에서 일어나게 할 거 가능
        if (HasInputAuthority)
        {
            Debug.Log($"[내 화면] 내가 {target.name}을(를) 공격함!");
        }
        if (HasStateAuthority)
        {
            PlayerHealth targetHP = target.GetComponent<PlayerHealth>();
            if (targetHP != null)
            {
                targetHP.RPC_TakeDamage(rangedWeapon.damage, myPlayer.gameObject.name);
            }
            //추후 화약통한테 데미지 들어가는 코드 조정
            PlayerWeapon targetWeapon = target.GetComponent<PlayerWeapon>();
            if (targetWeapon != null)
            {
                targetWeapon.RPC_TakeHitLog(myPlayer.gameObject.name);
            }
        }
    }
    else if(hit.Collider != null)
    {
        Debug.Log($"맞은 오브젝트 : {hit.Collider.gameObject.name}");
    }
    */
    private void OnGageChanged()
    {
        if (chargeParticle == null || rangedWeapon == null) return;

        if (currentGage <= 0)
        {
            if (chargeParticle.isPlaying) chargeParticle.Stop();
            chargeParticle.transform.localScale = Vector3.zero;
            return;
        }

        if (!chargeParticle.isPlaying)
        {
            chargeParticle.Play();
        }

        float chargeRatio = currentGage / rangedWeapon.MaxAmmo;
        float currentScale = Mathf.Lerp(0f, maxParticleScale, chargeRatio);
        chargeParticle.transform.localScale = new Vector3(currentScale, currentScale, currentScale);
    }



    void PlayLocalSfx(AudioClip clip)
    {
        if (clip == null || localAudioSource == null)
            return;

        if (Runner.IsResimulation)
            return;

        localAudioSource.PlayOneShot(clip);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void RPC_PlayNetworkSfx(LaserSfxType type)
    {
        if (networkAudioSource == null)
            return;

        switch (type)
        {
            case LaserSfxType.Shoot:
                networkAudioSource.PlayOneShot(shootSfx);
                break;
        }
    }

    protected override void CheckReload(NetworkInputData data, NetworkButtons prevButtons)
    {
        
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_DrawLaser(Vector3 startPos, Vector3 endPos)
    {
        if (laserLine == null) return;

        // 1. 선의 시작점과 끝점 설정
        laserLine.positionCount = 2;
        laserLine.SetPosition(0, startPos);
        laserLine.SetPosition(1, endPos);

        /*
        // 2. 피격 이펙트 소환 (선택 사항)
        if (laserHitEffectPrefab != null)
        {
            // 맞은 지점(endPos)에 파티클 생성하고 1초 뒤 삭제
            GameObject hitVfx = Instantiate(laserHitEffectPrefab, endPos, Quaternion.identity);
            Destroy(hitVfx, 1f);
        }
        */
        // 3. 기존에 페이드 효과가 돌고 있었다면 끄고 새로 시작 (연사 시 깜빡임 방지)
        if (laserFadeCoroutine != null)
        {
            StopCoroutine(laserFadeCoroutine);
        }
        laserFadeCoroutine = StartCoroutine(FadeLaserRoutine());
    }

    private System.Collections.IEnumerator FadeLaserRoutine()
    {
        laserLine.enabled = true; // 라인 켜기

        // 라인 렌더러의 원래 색상 가져오기
        Color startColor = laserLine.startColor;
        Color endColor = laserLine.endColor;

        float t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime / laserFadeDuration; // laserFadeDuration(예: 0.5초) 동안 서서히 증가

            // 알파(투명도) 값을 1에서 0으로 부드럽게 감소
            float alpha = Mathf.Lerp(1f, 0f, t);

            // 색상에 새로운 투명도 적용
            laserLine.startColor = new Color(startColor.r, startColor.g, startColor.b, alpha);
            laserLine.endColor = new Color(endColor.r, endColor.g, endColor.b, alpha);

            yield return null; // 다음 프레임까지 대기
        }

        laserLine.enabled = false; // 완전히 투명해지면 선 끄기

        // 다음 발사를 위해 색상 투명도 복구 (안 하면 다음번 쏠 때 안 보임!)
        laserLine.startColor = new Color(startColor.r, startColor.g, startColor.b, 1f);
        laserLine.endColor = new Color(endColor.r, endColor.g, endColor.b, 1f);
    }
}