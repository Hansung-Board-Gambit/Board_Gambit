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

    public Sprite weaponIcon;
    [TextArea]
    public string weaponDescription;

    [Header("양쪽 무기 전용 칸")]
    public GameObject leftHandVisualPrefab;
}
