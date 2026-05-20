using UnityEngine;

[CreateAssetMenu(fileName = "New Ranged", menuName = "FPS/Weapon/Ranged")]
public class RangedWeapon : WeaponData
{
    [Header("무기 특성")]
    public int MaxAmmo = 30;
    [Header("스킬 쿨타임")]
    public float leftClickCoolTime;
    public float rightClickCoolTime;
    public float skillQCoolTime;
    public float ReloadCoolTime;
}
