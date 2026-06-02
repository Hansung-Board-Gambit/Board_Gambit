using Fusion;
using UnityEngine;

public class ExplosiveBarrel : NetworkBehaviour
{
    [Header("화약통 설정")]
    [SerializeField] float explosionRadius = 5f;
    [SerializeField] int explosionDamage = 50;
    [SerializeField] LayerMask playerLayer;

    [Header("시각 효과")]
    [SerializeField] GameObject explosionVFX; // 화약통 이펙트 추가용

    [Networked, OnChangedRender(nameof(OnExplodedChanged))]
    public NetworkBool IsExploded { get; set; }

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            IsExploded = false;
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_TakeDamageBarrel(int damage, string attackerName)
    {
        if (IsExploded) return;

        //데미지가 들어오기만 하면 바로 폭발
        IsExploded = true;
        Explode();
    }

    private void Explode()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, explosionRadius, playerLayer);
        foreach(var hit in hitColliders)
        {
            PlayerHealth ph = hit.GetComponent<PlayerHealth>();
            if(ph != null && ph.CurrentHP > 0 )
            {
                ph.RPC_TakeDamage(explosionDamage, "화약통");
            }
        }
    }

    //IsExploded 값이 변할 때마다 모든 클라이언트의 화면에서 실행되는 함수
    public void OnExplodedChanged()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        foreach (var rend in renderers)
        {
            rend.enabled = !IsExploded;
        }

        // 터지는 순간 이펙트 생성
        if (IsExploded && explosionVFX != null)
        {
            Instantiate(explosionVFX, transform.position, Quaternion.identity);
        }
    }


}
