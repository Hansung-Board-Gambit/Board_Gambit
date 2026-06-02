using Fusion;
using UnityEngine;

public enum MyButtons
{
    LeftClick,
    RightClick,
    SkillQ,
    Reload,
    Jump
}

public struct NetworkInputData : INetworkInput
{
    public Vector3 direction;

    public float yaw;
    public float pitch;
    public NetworkBool jump;

    public NetworkBool speedUp;
    public NetworkBool sitDown;

    //버튼 클릭 상태 압축
    public NetworkButtons buttons;
}
