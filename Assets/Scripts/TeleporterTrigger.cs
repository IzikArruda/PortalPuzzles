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
         * 
         * this has been changed. teleportation will occcur when a ray collides with the trigger at any point
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


    public void TeleportParameters(ref Vector3 position, ref Quaternion rotation) {
        /*
         * Change the given parameters as if they were teleported from the teleporter 
         * connected to this script to the teleporter assigned to the "partner" variable.
         * 
         * Position determines the point of collision with the ray and the trigger.
         * Rotation determines what direction the position is pointed towards
         */
        //Note: there might need to be a rotation parameters that will be passed through 
        //to represent the difference in the rotation at the start and the rotation at the end

        //Note: a problem might be when the direction is not a straight vector.
        //test it by having a changable direction to see if it works
        //set both portla to be cloe together to ensure aby rotatio works wjen both portlas are identical

        /* Position the testthiscube at the starting position */
        if(testCubeThis != null) {
            testCubeThis.transform.position = position;
            testCubeThis.transform.rotation = rotation;
        }
        

        /* Get a vector of the distance from the collision point to the trigger's origin and rotate it by the partner trigger's rotation */
        Vector3 positionOffset = Quaternion.Inverse(transform.rotation)*(transform.position - position);
        positionOffset = partner.transform.rotation*positionOffset;


        /* Apply the rotationDifference to the partner portal's forward to get the ray's direction as if it was teleported */
        /* Apply the rotation difference between the ray and the hit trigger's rotation
		 * to the partner trigger's rotation to find the teleported rotation */
        //Maybe inverse the given rotation
        //The problem is the "rotation" value right here
        //maybe take the rotation and add them sepperatly after te portal rotation is done
        //The ideal way should have t find the difference from the ray to the hit portals normal. Then the difference berween the two portals
        Quaternion teleportedDirection = partner.transform.rotation*Quaternion.Inverse(transform.rotation);
        //Quaternion teleportedForward = Quaternion.FromToRotation(transform.forward, partner.transform.forward);

        //testCubePartner.transform.rotation = Quaternion.LookRotation(partner.transform.forward, partner.transform.up);
        //testCubePartner.transform.rotation *= Quaternion.Inverse(transform.rotation)*Quaternion.LookRotation(direction, upVector);
















        //CURRENTLY WORTKING????
        /* Update the parameters now that proper rotations and offsets have been found */
        position = partner.transform.position - positionOffset;
        Quaternion newRotation = teleportedDirection;
        newRotation *= rotation;

        rotation = newRotation;

















        Vector3 currForward = rotation * Vector3.forward;
        Vector3 currUp = rotation * Vector3.up;

        /* Draw a line that represents the new directions */
        Debug.DrawLine(position, position + currForward*2f, Color.blue);
        Debug.DrawLine(position, position + currUp*1f, Color.blue);
        
        /* Position the test cube at the new position and direction */
        if(testCubePartner != null) {
            testCubePartner.transform.position = position;
            testCubePartner.transform.rotation = rotation;
        }
    }
}
