using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Ultimatescifiuikit.AudioManager;

namespace Ultimatescifiuikit.HoverButtonScrip
{
    public class HoverButtonScript : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        public RectTransform Button;
        public Ultimatescifiuikit.AudioManager.AudioManager audioManager;

        // Start is called before the first frame update
        void Start()
        {
            Button.GetComponent<Animator>().Play("DeathmatchPanelIdleAnim");
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            Button.GetComponent<Animator>().Play("DeathmatchPanelOnHoverAnim");
            audioManager.MenuHover();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            Button.GetComponent<Animator>().Play("DeathmatchPanelOffHoverAnim");
        }

        public void OnPointerClick(PointerEventData pointerEventData)
        {
            //      Button.GetComponent<Animator>().Play("DeathmatchPanelOffHoverAnim");
            audioManager.ButtonClick();
        }
    }
}

