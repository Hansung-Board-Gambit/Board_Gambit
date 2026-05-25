using UnityEngine;
using System.Collections;

namespace Ultimatescifiuikit.ImageRotation
{
    public class ImageRotation : MonoBehaviour
    {
        public GameObject ring1;
        //	public GameObject ring2;



        void Update()
        {
            ring1.transform.Rotate(0, 0, -1.2f, Space.Self);
    //		ring1.transform.Rotate (new Vector3(Time.deltaTime* 0,0,-0.01f));
        }

    }
}

