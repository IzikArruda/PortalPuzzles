using UnityEngine;
using System.Collections;

/*
 * Used for moving the given camera around the demo scene
 */
[ExecuteInEditMode]
public class DemoCameraController : MonoBehaviour {


    public GameObject demoCamera;
    public GameObject demoCameraContainer;
    public GameObject focusPoint;
    public float zDistance;
    public float yDistance;
    public float rotationSpeed;
    private float currentRot;

    public GameObject[] portalExits;
    public GameObject[] portalDoorways;
    public GameObject[] portalPositions;

    /* Move the camera every frame */
	void Update () {
	

        /* Make sure the demo camera exists before moving it */
        if(focusPoint != null && demoCamera != null && demoCameraContainer != null) {

            /* Place the base of the camera container */
            demoCameraContainer.transform.position = focusPoint.transform.position;
            demoCameraContainer.transform.localEulerAngles = new Vector3(0, currentRot, 0);

            /* Place the camera itself */
            demoCamera.transform.localPosition = new Vector3(0, yDistance, zDistance);

            /* Increment the rotation */
            currentRot += rotationSpeed;
            if(currentRot > 360) {
                currentRot =- 360;
            }
        }

        /* Place the portals at the given portal positions */
        for(int i = 0; i < portalExits.Length; i++) {
            portalExits[i].transform.position = portalPositions[i].transform.position;
            portalExits[i].transform.rotation = portalPositions[i].transform.rotation;
            portalExits[i].transform.position += new Vector3(0, 0, -yDistance);
            portalDoorways[i].transform.position = portalPositions[i].transform.position;
            portalDoorways[i].transform.rotation = portalPositions[i].transform.rotation;
            portalDoorways[i].transform.position += new Vector3(0, 0, -yDistance);
        }
	}
}
