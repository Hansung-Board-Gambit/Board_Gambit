using Fusion;
using System.Collections;
using UnityEngine;

public class SelfieStick : WeaponBase
{
    private MeleeWeapon meleeWeapon;

    [Header("시각적 모션")]
    [SerializeField] Transform stickModel;
    [SerializeField] float swingSpeed = 12f;
    [SerializeField] float hitDelay = 0.2f;

    [Header("3인칭 카메라 세팅")]
    [SerializeField] Vector3 tpsCameraOffset = new Vector3(1.5f, 0.5f, -3f);
    [SerializeField] float cameraTransitionSpeed = 8f;
    [SerializeField] LayerMask targetLayer;

    [Networked] public TickTimer HitTimer { get; set; }

    private Quaternion originalRotation;
    //3인칭 시점과 관련된 변수
    private bool isTpsMode = false;
    private bool isSwinging = false;

    private Vector3 originalLocalPos;
    public override void Init(PlayerWeapon owner, WeaponData data)
    {
        base.Init(owner, data);
        meleeWeapon = data as MeleeWeapon;

        if (stickModel != null) originalRotation = stickModel.localRotation;

        originalLocalPos = transform.localPosition;
    }

    protected override void CheckRightClick(NetworkInputData data, NetworkButtons prevButtons)
    {
        isTpsMode = data.buttons.IsSet(MyButtons.RightClick);
    }

    protected override void BasicAttack()
    {
        if (LeftClickTimer.ExpiredOrNotRunning(Runner) && !isSwinging)
        {
            if (stickModel != null) StartCoroutine(SwingMotion());
            HitTimer = TickTimer.CreateFromSeconds(Runner, hitDelay);
            LeftClickTimer = TickTimer.CreateFromSeconds(Runner, meleeWeapon.leftClickCoolTime);
        }
    }

    private void Update()
    {
        if (!HasInputAuthority || myPlayer == null) return;

        // 여기서 카메라는 뒤로 도망갑니다!
        Vector3 targetOffset = isTpsMode ? tpsCameraOffset : Vector3.zero;
        myPlayer.weaponCameraOffset = Vector3.Lerp(myPlayer.weaponCameraOffset, targetOffset, Time.deltaTime * cameraTransitionSpeed);

        transform.localPosition = originalLocalPos + Quaternion.Inverse
            (myPlayer.fpsCamera.transform.localRotation) * (-myPlayer.weaponCameraOffset);
    }



    public override void OnFixedUpdateNetwork()
    {
        base.OnFixedUpdateNetwork();

        if (HitTimer.Expired(Runner))
        {
            ExecuteHitLogic();
            HitTimer = TickTimer.None;
        }
    }

    private void ExecuteHitLogic()
    {
        if (myPlayer == null) return;

        Vector3 boxCenter = transform.position + 
            myPlayer.fpsCamera.transform.forward * (meleeWeapon.range / 2f);

        Vector3 boxSize = new Vector3(2f, 2f, meleeWeapon.range);

        var hits = new System.Collections.Generic.List<LagCompensatedHit>();
        Runner.LagCompensation.OverlapBox(boxCenter, boxSize / 2f, myPlayer.fpsCamera.transform.rotation, Object.InputAuthority, hits, targetLayer);

        foreach (var hit in hits)
        {
            if (hit.Hitbox.Root.gameObject == myPlayer.gameObject) continue; // 자해 방지
            if (HasStateAuthority)
            {
                hit.Hitbox.Root.GetComponent<PlayerHealth>()?.RPC_TakeDamage(meleeWeapon.damage, myPlayer.gameObject.name);
            }
        }
    }

    private IEnumerator SwingMotion()
    {
        isSwinging = true;
        Quaternion targetRotation = originalRotation * Quaternion.Euler(30f, -90f, 0f);

        float t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime * swingSpeed;
            stickModel.localRotation = Quaternion.Lerp(originalRotation, targetRotation, Mathf.SmoothStep(0, 1, t));
            yield return null;
        }

        t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime * (swingSpeed * 0.5f);
            stickModel.localRotation = Quaternion.Lerp(targetRotation, originalRotation, Mathf.SmoothStep(0, 1, t));
            yield return null;
        }

        stickModel.localRotation = originalRotation;
        isSwinging = false;
    }

    //다른 무기 교체시 셀카봉 비활성화와 관련된 함수
    private void OnDisable()
    {
        //3인칭 모드 강제 종료
        isTpsMode = false;
        //카메라가 뒤로 빠져있었다면 제자리(0,0,0)로 복구
        if(myPlayer != null)
        {
            myPlayer.weaponCameraOffset = Vector3.zero;
        }
    }

    protected override void SecondAttack() { }
    protected override void SkillQ() { }
}