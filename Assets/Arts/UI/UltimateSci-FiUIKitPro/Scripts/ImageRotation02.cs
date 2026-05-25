using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Ultimatescifiuikit.ImageRotation02
{
    public class ImageRotation02 : MonoBehaviour
    {
        //	public GameObject ring1;
        public GameObject target;
        public float rotationSpeed;
        //	public GameObject ring2;
        //	public float xAngle, yAngle, zAngle;


        void Update()
        {
            //		ring1.transform.Rotate(0.0f, 0, 90f, Space.World);
            //		Spin the object around the target at 20 degrees/second.
            //		transform.RotateAround(target.transform.position, Vector3.up, 20 * Time.deltaTime);
            transform.RotateAround(target.transform.position, new Vector3(0, 0, 90), rotationSpeed * Time.deltaTime);



        }
    }
}

