using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkillSlotUI : MonoBehaviour
{
    public Image iconImage;
    public Image cooldownMask;

    public void SetSkill(Sprite icon)
    {
        iconImage.sprite = icon;
    }

    public void SetCooldown(float ratio)
    {
        cooldownMask.fillAmount = ratio;
    }
}