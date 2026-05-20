using UnityEngine;

[CreateAssetMenu(fileName = "New Melee", menuName = "FPS/Weapon/Melee")]
public class MeleeWeapon : WeaponData
{
    [Header("스킬 쿨타임")]
    public float leftClickCoolTime;
    public float rightClickCoolTime;
    public float skillQCoolTime;
}
