using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerUI : MonoBehaviour
{
    public static PlayerUI instance;

    [Header("UI ø¨∞·")]
    [SerializeField] private Image hpFillImage;
    [SerializeField] private TextMeshProUGUI ammoText;
    [SerializeField] private TextMeshProUGUI hostNameText;
    [SerializeField] private TextMeshProUGUI guestNameText;
    [SerializeField] private Image myHpFillImage;
    [SerializeField] private Image enemyHpFillImage;
    [SerializeField] private GameObject qSkillRoot;
    [SerializeField] private GameObject rightSkillRoot;
    [SerializeField] private Image qSkillImage;
    [SerializeField] private Image rightSkillImage;
    [SerializeField] private Image qSkillCooldown;
    [SerializeField] private Image rightSkillCooldown;
    //[SerializeField] TextMeshProUGUI grapplingText;

    private float rightCooldownTime;
    private float rightCooldownMax;
    private float qCooldownTime;
    private float qCooldownMax;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject); // «ŸΩ…
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

    private void UpdateCooldownUI()
    {
        if (rightSkillImage.gameObject.activeSelf)
        {
            if (rightCooldownTime > 0f)
            {
                rightCooldownTime -= Time.deltaTime;
                rightSkillCooldown.fillAmount = rightCooldownTime / rightCooldownMax;
            }
        }

        if (qSkillImage.gameObject.activeSelf)
        {
            if (qCooldownTime > 0f)
            {
                qCooldownTime -= Time.deltaTime;
                qSkillCooldown.fillAmount = qCooldownTime / qCooldownMax;
            }
        }
    }

    private void Update()
    {
        UpdateCooldownUI();
    }

    public void StartRightCooldown(float cooldown)
    {
        rightCooldownMax = cooldown;
        rightCooldownTime = cooldown;
    }

    public void StartQCooldown(float cooldown)
    {
        qCooldownMax = cooldown;
        qCooldownTime = cooldown;
    }

    public void SetWeaponUI(WeaponData data)
    {
        Debug.Log("SetWeaponUI »£√‚µ ");
        qSkillRoot.SetActive(data.hasQSkill);
        rightSkillRoot.SetActive(data.hasRightSkill);

        if (data.hasRightSkill)
        {
            rightSkillImage.sprite = data.rightIcon;
            rightCooldownMax = data.rightCooldown;
            rightCooldownTime = 0f;
            rightSkillCooldown.fillAmount = 0f;
        }

        if (data.hasQSkill)
        {
            qSkillImage.sprite = data.qIcon;
            qCooldownMax = data.qCooldown;
            qCooldownTime = 0f;
            qSkillCooldown.fillAmount = 0f;
        }
    }

    public void UpdateHP(int currentHP, int maxHP)
    {
        Debug.Log($"HP UI Update: {currentHP}/{maxHP}");
        if (hpFillImage == null) return;
        float ratio = (float)currentHP / maxHP;
        hpFillImage.fillAmount = (float)currentHP / maxHP;
        Debug.Log($"fillAmount = {hpFillImage.fillAmount}");
    }

    public void UpdateMatchHP(int myCurrent, int myMax, int enemyCurrent, int enemyMax)
    {
        if (myHpFillImage != null)
            myHpFillImage.fillAmount = (float)myCurrent / myMax;

        if (enemyHpFillImage != null)
            enemyHpFillImage.fillAmount = (float)enemyCurrent / enemyMax;
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
