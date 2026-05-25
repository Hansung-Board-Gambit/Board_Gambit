using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Ultimatescifiuikit.HoverStoryModeButtonScript
{
    public class HoverStoryModeButtonScript : MonoBehaviour
    {
        public Button ch01;
        public Button ch02;
        public Button ch03;
        public Button ch04;
        public Button ch05;
        public Button ch06;
        public Button ch07;
        // Start is called before the first frame update
        void Start()
        {
            ch01.GetComponent<Animator>().Play("Chap01OnIdleAnim");
            ch02.GetComponent<Animator>().Play("Chap02OnIdleAnim");
            ch03.GetComponent<Animator>().Play("Chap03OnIdleAnim");
            ch04.GetComponent<Animator>().Play("Chap04OnIdleAnim");
            ch05.GetComponent<Animator>().Play("Chap05OnIdleAnim");
            ch06.GetComponent<Animator>().Play("Chap06OnIdleAnim");
            ch07.GetComponent<Animator>().Play("Chap07OnIdleAnim");
        }

        public void Chap01OnPointerEnter()
        {
            ch01.GetComponent<Animator>().Play("Chap01OnHoverAnim");

        }

        public void Chap01OnPointerExit()
        {
            ch01.GetComponent<Animator>().Play("Chap01OffHoverAnim");

        }

        public void Chap01OnPointerClick()
        {
         // ch01.GetComponent<Animator>().Play("Chap01OnClickAnim");
        }

        public void Chap02OnPointerEnter()
        {
            ch02.GetComponent<Animator>().Play("Chap02OnHoverAnim");

        }

        public void Chap02OnPointerExit()
        {
            ch02.GetComponent<Animator>().Play("Chap02OffHoverAnim");

        }

        public void Chap02OnPointerClick()
        {
    //      ch02.GetComponent<Animator>().Play("Chap02OnClickAnim");
        }

        public void Chap03OnPointerEnter()
        {
            ch03.GetComponent<Animator>().Play("Chap03OnHoverAnim");

        }

        public void Chap03OnPointerExit()
        {
            ch03.GetComponent<Animator>().Play("Chap03OffHoverAnim");

        }

        public void Chap03OnPointerClick()
        {
    //      ch03.GetComponent<Animator>().Play("Chap03OnClickAnim");
        }

        public void Chap04OnPointerEnter()
        {
            ch04.GetComponent<Animator>().Play("Chap04OnHoverAnim");

        }

        public void Chap04OnPointerExit()
        {
            ch04.GetComponent<Animator>().Play("Chap04OffHoverAnim");

        }

        public void Chap04OnPointerClick()
        {
    //      ch04.GetComponent<Animator>().Play("Chap04OnClickAnim");
        }

        public void Chap05OnPointerEnter()
        {
            ch05.GetComponent<Animator>().Play("Chap05OnHoverAnim");

        }

        public void Chap05OnPointerExit()
        {
            ch05.GetComponent<Animator>().Play("Chap05OffHoverAnim");

        }

        public void Chap05OnPointerClick()
        {
    //      ch05.GetComponent<Animator>().Play("Chap05OnClickAnim");
        }

        public void Chap06OnPointerEnter()
        {
            ch06.GetComponent<Animator>().Play("Chap06OnHoverAnim");

        }

        public void Chap06OnPointerExit()
        {
            ch06.GetComponent<Animator>().Play("Chap06OffHoverAnim");

        }

        public void Chap06OnPointerClick()
        {
    //      ch06.GetComponent<Animator>().Play("Chap06OnClickAnim");
        }

        public void Chap07OnPointerEnter()
        {
            ch07.GetComponent<Animator>().Play("Chap07OnHoverAnim");

        }

        public void Chap07OnPointerExit()
        {
            ch07.GetComponent<Animator>().Play("Chap07OffHoverAnim");

        }

        public void Chap07OnPointerClick()
        {
    //      ch07.GetComponent<Animator>().Play("Chap07OnClickAnim");
        }
    }
}


