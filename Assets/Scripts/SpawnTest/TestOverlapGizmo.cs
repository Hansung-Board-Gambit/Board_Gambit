using UnityEngine;

public class TestOverlapGizmo : MonoBehaviour
{
    [Header("시각화 켜기/끄기")]
    public bool showBox = true;
    public bool showSphere = true;

    [Header("1. 상자 (기본 공격 / OverlapBox)")]
    [Tooltip("상자의 가로 너비")]
    public float attackWidth = 2f;
    [Tooltip("상자의 앞뒤 길이 (사거리)")]
    public float attackRange = 1.5f;
    [Tooltip("상자의 높이")]
    public float attackHeight = 2f;

    [Header("2. 구체 (내려찍기 / OverlapSphere)")]
    [Tooltip("구체의 반지름 (지름은 이 값의 2배)")]
    public float slamRadius = 4f;

    [Header("기준 위치 (안 넣으면 이 스크립트가 달린 오브젝트 기준)")]
    public Transform referenceTransform;

    private void OnDrawGizmos()
    {
        // 기준점이 설정 안 되어 있으면 스크립트가 달린 오브젝트를 기준으로 삼습니다.
        Transform origin = referenceTransform != null ? referenceTransform : transform;

        // --------------------------------------------------
        //상자 (OverlapBox) 그리기
        // -----------------------------------------------------
        if (showBox)
        {
            // 중심점 계산: 기준점으로부터 바라보는 방향(forward)으로 '사거리의 절반'만큼 이동
            Vector3 boxCenter = origin.position + origin.forward * (attackRange / 2f);
            Vector3 boxSize = new Vector3(attackWidth, attackHeight, attackRange);

            // 기즈모 색상 (반투명 빨간색) 및 테두리 색상
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);

            // 박스를 내 회전 각도에 맞게 눕힙니다.
            Gizmos.matrix = Matrix4x4.TRS(boxCenter, origin.rotation, Vector3.one);

            // 색칠된 박스와 선명한 테두리를 같이 그립니다.
            Gizmos.DrawCube(Vector3.zero, boxSize);
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(Vector3.zero, boxSize);
        }

        // -----------------------------------------------------
        //구체 (OverlapSphere) 그리기
        // -----------------------------------------------------
        if (showSphere)
        {
            // 구체는 회전이 필요 없으므로 원래 축으로 되돌립니다.
            Gizmos.matrix = Matrix4x4.identity;

            // 기즈모 색상 (반투명 파란색)
            Gizmos.color = new Color(0f, 0.5f, 1f, 0.3f);
            Gizmos.DrawSphere(origin.position, slamRadius);

            // 선명한 테두리
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(origin.position, slamRadius);
        }
    }
}
