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

    public void UpdateHPText(int currentHP, int maxHP)
    {
        hpText.text = $"HP: {currentHP}/{maxHP}";
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
