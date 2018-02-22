using UnityEngine;
using System.Collections;

/*
 * Attach this sctipr to an object that contains a camera that will use the skysphere
 */
public class SkySphereCamera : MonoBehaviour {

    /* The camera that will render only the skySphere */
    private GameObject skySphereCameraObject;
    private Camera skysphereCamera;


	void Start () {
        /*
         * On startup, create the camera and set it's properties to make it only render the skySphere
         */

        /* Create the object to hold the camera */
        skySphereCameraObject = new GameObject();
        skySphereCameraObject.name = "Sky Sphere Camera";
        skySphereCameraObject.layer = PortalSet.maxLayer + 1;
        skySphereCameraObject.transform.parent = transform;
        skySphereCameraObject.transform.localPosition = new Vector3(0, 0, 0);
        skySphereCameraObject.transform.localEulerAngles = new Vector3(0, 0, 0);
        skySphereCameraObject.transform.localScale = new Vector3(1, 1, 1);

        /* Attach the camera and change it's settings */
        skysphereCamera = skySphereCameraObject.AddComponent<Camera>();
        skysphereCamera.clearFlags = CameraClearFlags.Depth;
        skysphereCamera.cullingMask = 1 << PortalSet.maxLayer + 1;

        /* Add a script to the camera that will prevent the rendering of fog */
        skySphereCameraObject.AddComponent<RemoveFog>();

        /* Get the camera that wants the skySphere to be rendered and use some of it's stats */
        if(GetComponent<Camera>() != null) {
            skysphereCamera.depth = GetComponent<Camera>().depth - 1;
            skysphereCamera.nearClipPlane = GetComponent<Camera>().nearClipPlane;
            skysphereCamera.farClipPlane = GetComponent<Camera>().farClipPlane;

            /* Do not let the camera render the skySphere */
            GetComponent<Camera>().clearFlags = CameraClearFlags.Nothing;
            GetComponent<Camera>().cullingMask = ~(1 << PortalSet.maxLayer + 1);
        }
    }
}
