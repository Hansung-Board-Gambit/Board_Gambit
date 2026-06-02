using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUI : MonoBehaviour
{
    public static PlayerUI instance;

    [Header("UI ¿¬°á")]
    [SerializeField] private Image hpFillImage;
    [SerializeField] private TextMeshProUGUI ammoText;
    [SerializeField] private TextMeshProUGUI hostNameText;
    [SerializeField] private TextMeshProUGUI guestNameText;
    //[SerializeField] TextMeshProUGUI grapplingText;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject); // ÇÙ½É
    }

    public void Configure(TextMeshProUGUI hp, TextMeshProUGUI ammo)
    {
        ammoText = ammo;
        instance = this;
        Clear();
    }

    public void Clear()
    {
        if (ammoText != null)
            ammoText.text = "Ammo : ";
    }

    public void UpdateHP(int currentHP, int maxHP)
    {
        Debug.Log($"HP UI Update: {currentHP}/{maxHP}");
        if (hpFillImage == null) return;
        float ratio = (float)currentHP / maxHP;
        hpFillImage.fillAmount = (float)currentHP / maxHP;
        Debug.Log($"fillAmount = {hpFillImage.fillAmount}");
    }

    public void UpdateAmmoText(int currentAmmo, int maxAmmo)
    {
        if (ammoText != null) ammoText.text = $"Ammo : {currentAmmo}/{maxAmmo}";
    }

    public void UpdateNames(string hostName, string guestName)
    {
        if (hostNameText != null)
            hostNameText.text = hostName;

        if (guestNameText != null)
            guestNameText.text = guestName;
    }

    public void UpdateCurrentGrapplingText(int currentGrappling, int maxGrappling)
    {
        //if(grapplingText != null) grapplingText.text = $"Grappling : {currentGrappling}/{maxGrappling}";
    }
}
