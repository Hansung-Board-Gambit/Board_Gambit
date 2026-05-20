using UnityEngine;
public enum WeaponType {Melee, Ranged, Special}

public class WeaponData : ScriptableObject
{
    [Header("무기 정보")]
    public string weaponName;
    public GameObject weaponPrefab;
    public Sprite weaponImg;
    public WeaponType type;

    [Header("무기 속성")]
    public int damage;
    public float range;
    //public float fireRate;

    //무기 들면 무기가 손에 장착되게끔 조정 -> 애니메이션같은 부분은 일단 대기 
}
