using UnityEngine;
using System.Collections;

/*
 * Attach this to a trigger that is placed in front of the portal's mesh. The trigger must
 * have it's local positive X axis face towards the portal's mesh. When teleporting the player,
 * it can send a "teleported" signal to the linked TeleportHandler if needed.
 * 
 * 
 */
public class TeleporterTrigger : MonoBehaviour {

    public GameObject testCubePartner = null;
    public GameObject testCubeThis = null;
    public Transform partner;
    //public TeleportHandler teleportSignal;

    void OnTriggerStay(Collider collider) {
        /*
         * Check if the player will need to be teleported
         */
        /*
         * Check if the player will need to be teleported. The player will be teleported if they pass
         * the plane defined by the portal mesh.
         */

        if(collider.tag == "Player") {
            /* Get the plane of the portal's mesh using the teleport trigger */
            Vector3 planeNormal = transform.rotation*new Vector3(1, 0, 0);

            /* Create a plane of the form of the portal mesh */
            Plane triggerPlane = new Plane(planeNormal, transform.position);


            /* Check if the center of the colliding object is between the trigger's center and the portal's mesh */
            if(triggerPlane.GetSide(collider.transform.position)) {
                //TeleportCollider(collider.transform);
                Debug.Log("TELEPORTED PLAYER");
            }
            /* The portal mesh will always be facing in the local X negative direction and will 
             * positioned be half the collider box's X width in it's local X positive direction. */
        }
    }


    public void TeleportParameters(ref Vector3 position, ref Vector3 direction, ref Quaternion rotation) {
        /*
         * Change the given parameters as if they were teleported from the teleporter 
         * connected to this script to the teleporter assigned to the "partner" variable.
         * 
         * Position determines the point of collision with the ray and the trigger.
         * Direction and up vectors are used to define the ray's orientation. 
         * Update the rotation parameter with any rotation that is applied to the direction.
         */
        //The testthiscubes will need to be removed
        //this value must be implemented into the teleport parameter, or have direction and up combined into a quaternion
        Vector3 upVector = new Vector3(0, 1, 0);

        /* Position the testthiscube at the starting position */
        if(testCubeThis != null) {
            testCubeThis.transform.position = position + direction*0;
            testCubeThis.transform.rotation = Quaternion.LookRotation(direction, upVector);
        }
        

        /* Get a vector of the distance from the ray to the trigger's origin and rotate it's partner's rotation */
        Vector3 positionOffset = Quaternion.Inverse(transform.rotation)*(transform.position - position);
        positionOffset = partner.transform.rotation*positionOffset;

        
        /* Apply the rotationDifference to the partner portal's forward to get the ray's direction as if it was teleported */
        testCubePartner.transform.rotation = Quaternion.LookRotation(partner.transform.forward, partner.transform.up);
        testCubePartner.transform.rotation *= Quaternion.Inverse(transform.rotation)*Quaternion.LookRotation(direction, upVector);


        /* Update the parameters now that proper rotations and offsets have been found */
        position = partner.transform.position - positionOffset;
        direction = testCubePartner.transform.forward;
        upVector = testCubePartner.transform.up;


        /* Draw a line that represents the new directions */
        Debug.DrawLine(position, position + direction*2f, Color.blue);
        Debug.DrawLine(position, position + upVector*1f, Color.blue);
        /* Position the test cube at the new position */
        if(testCubePartner != null) {
            testCubePartner.transform.position = position;
            
        }
    }
}
