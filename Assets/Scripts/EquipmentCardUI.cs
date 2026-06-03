using TMPro;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class EquipmentCardUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descriptionText;
    public Image weaponIconImage;

    [Header("Hover Description")]
    public bool showDescriptionOnHover = true;
    public float hoverFadeDuration = 0.18f;
    public Color hoverOverlayColor = new Color(0f, 0f, 0f, 0.88f);
    public Color hoverTextColor = Color.white;
    public int hoverDescriptionFontSize = 24;
    public Vector2 hoverTextPadding = new Vector2(28f, 28f);

    private CanvasGroup hoverCanvasGroup;
    private Image hoverOverlayImage;
    private Text hoverDescriptionText;
    private Coroutine hoverRoutine;
    private string hoverDescription = "";
    private static Font hoverKoreanFont;

    private void Awake()
    {
        EnsureHoverOverlay();
        SetHoverAlpha(0f);
    }

    private void OnEnable()
    {
        EnsureHoverOverlay();
        SetHoverAlpha(0f);
    }

    private void OnDisable()
    {
        if (hoverRoutine != null)
        {
            StopCoroutine(hoverRoutine);
            hoverRoutine = null;
        }

        SetHoverAlpha(0f);
    }

    public void SetWeaponData(WeaponData data)
    {
        if (nameText != null)
            nameText.text = data != null ? data.weaponName : "Empty";

        if (weaponIconImage != null)
        {
            bool hasIcon = data != null && data.weaponIcon != null;
            weaponIconImage.sprite = hasIcon ? data.weaponIcon : null;
            weaponIconImage.enabled = hasIcon;
        }

        SetDescription(GetHoverDescription(data));
    }

    public void SetDescription(string description)
    {
        hoverDescription = description ?? "";

        if (descriptionText != null)
            descriptionText.text = hoverDescription;

        if (hoverDescriptionText != null)
            hoverDescriptionText.text = hoverDescription;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!showDescriptionOnHover || string.IsNullOrWhiteSpace(hoverDescription))
            return;

        FadeHoverOverlay(1f);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        FadeHoverOverlay(0f);
    }

    private void EnsureHoverOverlay()
    {
        if (hoverCanvasGroup != null && hoverDescriptionText != null)
            return;

        Image cardImage = GetComponent<Image>();
        if (cardImage != null)
            cardImage.raycastTarget = true;

        Transform existingOverlay = transform.Find("WeaponDescriptionHoverOverlay");
        GameObject overlayObject = existingOverlay != null ? existingOverlay.gameObject : new GameObject("WeaponDescriptionHoverOverlay");
        overlayObject.transform.SetParent(transform, false);
        overlayObject.transform.SetAsLastSibling();

        RectTransform overlayRect = overlayObject.GetComponent<RectTransform>();
        if (overlayRect == null)
            overlayRect = overlayObject.AddComponent<RectTransform>();

        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;
        overlayRect.localScale = Vector3.one;

        hoverOverlayImage = overlayObject.GetComponent<Image>();
        if (hoverOverlayImage == null)
            hoverOverlayImage = overlayObject.AddComponent<Image>();

        hoverOverlayImage.color = hoverOverlayColor;
        hoverOverlayImage.raycastTarget = false;

        hoverCanvasGroup = overlayObject.GetComponent<CanvasGroup>();
        if (hoverCanvasGroup == null)
            hoverCanvasGroup = overlayObject.AddComponent<CanvasGroup>();

        hoverCanvasGroup.blocksRaycasts = false;
        hoverCanvasGroup.interactable = false;

        Transform existingText = overlayObject.transform.Find("DescriptionText");
        GameObject textObject = existingText != null ? existingText.gameObject : new GameObject("DescriptionText");
        textObject.transform.SetParent(overlayObject.transform, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        if (textRect == null)
            textRect = textObject.AddComponent<RectTransform>();

        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = hoverTextPadding;
        textRect.offsetMax = -hoverTextPadding;
        textRect.localScale = Vector3.one;

        hoverDescriptionText = textObject.GetComponent<Text>();
        if (hoverDescriptionText == null)
            hoverDescriptionText = textObject.AddComponent<Text>();

        hoverDescriptionText.text = hoverDescription;
        hoverDescriptionText.color = hoverTextColor;
        hoverDescriptionText.fontSize = hoverDescriptionFontSize;
        hoverDescriptionText.font = GetHoverKoreanFont();
        hoverDescriptionText.alignment = TextAnchor.MiddleCenter;
        hoverDescriptionText.horizontalOverflow = HorizontalWrapMode.Wrap;
        hoverDescriptionText.verticalOverflow = VerticalWrapMode.Truncate;
        hoverDescriptionText.supportRichText = false;
        hoverDescriptionText.raycastTarget = false;
    }

    private Font GetHoverKoreanFont()
    {
        if (hoverKoreanFont != null)
            return hoverKoreanFont;

        hoverKoreanFont = Font.CreateDynamicFontFromOSFont(
            new[]
            {
                "Malgun Gothic",
                "Apple SD Gothic Neo",
                "Noto Sans CJK KR",
                "Noto Sans KR",
                "NanumGothic",
                "Arial Unicode MS"
            },
            Mathf.Max(1, hoverDescriptionFontSize)
        );

        if (hoverKoreanFont == null)
            hoverKoreanFont = Resources.GetBuiltinResource<Font>("Arial.ttf");

        return hoverKoreanFont;
    }

    private string GetHoverDescription(WeaponData data)
    {
        if (data == null)
            return "";

        return data.weaponDescription ?? "";
    }

    private void FadeHoverOverlay(float targetAlpha)
    {
        EnsureHoverOverlay();

        if (hoverRoutine != null)
            StopCoroutine(hoverRoutine);

        hoverRoutine = StartCoroutine(FadeHoverOverlayRoutine(targetAlpha));
    }

    private IEnumerator FadeHoverOverlayRoutine(float targetAlpha)
    {
        float startAlpha = hoverCanvasGroup != null ? hoverCanvasGroup.alpha : 0f;
        float duration = Mathf.Max(0.01f, hoverFadeDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            SetHoverAlpha(Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration));
            yield return null;
        }

        SetHoverAlpha(targetAlpha);
        hoverRoutine = null;
    }

    private void SetHoverAlpha(float alpha)
    {
        if (hoverCanvasGroup == null)
            return;

        hoverCanvasGroup.alpha = alpha;
    }
}
