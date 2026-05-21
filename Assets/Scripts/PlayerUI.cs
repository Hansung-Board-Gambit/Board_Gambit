using TMPro;
using UnityEngine;

public class PlayerUI : MonoBehaviour
{
    public static PlayerUI instance;

    [Header("UI ¿¬°á")]
    [SerializeField] TextMeshProUGUI hpText;
    [SerializeField] TextMeshProUGUI ammoText;
    //[SerializeField] TextMeshProUGUI grapplingText;

    private void Awake()
    {
        if(instance == null) instance = this;
        else Destroy(gameObject);
    }
    public void Configure(TextMeshProUGUI hp, TextMeshProUGUI ammo)
    {
        hpText = hp;
        ammoText = ammo;
        instance = this;
        Clear();
    }

    public void Clear()
    {
        if (hpText != null)
            hpText.text = "HP : ";

        if (ammoText != null)
            ammoText.text = "Ammo : ";
    }

    public void UpdateHPText(int currentHP, int maxHP)
    {
        if (hpText != null) hpText.text = $"HP : {currentHP}/{maxHP}";
    }

    public void UpdateAmmoText(int currentAmmo, int maxAmmo)
    {
        if (ammoText != null) ammoText.text = $"Ammo : {currentAmmo}/{maxAmmo}";
    }

    public void UpdateCurrentGrapplingText(int currentGrappling, int maxGrappling)
    {
        //if(grapplingText != null) grapplingText.text = $"Grappling : {currentGrappling}/{maxGrappling}";
    }
}
