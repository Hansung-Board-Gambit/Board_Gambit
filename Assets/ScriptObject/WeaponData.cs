using UnityEngine;
public enum WeaponType {Melee, Ranged, Special}

public class WeaponData : ScriptableObject
{
    [Header("公扁 沥焊")]
    public string weaponName;
    public GameObject weaponPrefab;
    public Sprite weaponImg;
    public WeaponType type;

    [Header("公扁 加己")]
    public int damage;
    public float range;

    public Sprite weaponIcon;
    [TextArea]
    public string weaponDescription;
}
