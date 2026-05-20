using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class ButtonHoverUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Image buttonImage;
    private TMP_Text buttonText;

    public Color normalBg = new Color(1, 1, 1, 0);
    public Color hoverBg = new Color(1f, 0.4f, 0.7f, 1f);

    public Color normalText = Color.black;
    public Color hoverText = Color.white;

    void Awake()
    {
        buttonImage = GetComponent<Image>();
        buttonText = GetComponentInChildren<TMP_Text>();
    }

    void Start()
    {
        ResetState();
    }

    void OnEnable()
    {
        ResetState();
    }

    void OnDisable()
    {
        ResetState();
    }

    void ResetState()
    {
        if (buttonImage != null)
            buttonImage.color = normalBg;

        if (buttonText != null)
            buttonText.color = normalText;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (buttonImage != null)
            buttonImage.color = hoverBg;

        if (buttonText != null)
            buttonText.color = hoverText;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ResetState();
    }
}