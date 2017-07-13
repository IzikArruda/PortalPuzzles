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

    public void TeleportCollider(Transform collidingObject) {
        /* 
         * Teleport the given transform to the teleporter's patner's location and signal the linked teleport handler
         */
         
        /* Get the position difference between the player and the trigger's center */
        Vector3 offsetFromCenter = collidingObject.position - transform.position;

        /* Get the rotation difference betweem the teleporters and apply it to the player */
        Quaternion newQuat = partner.transform.rotation* Quaternion.Inverse(transform.rotation);
        
        /* Move and rotate the player relative to the position and rotation differences */
        collidingObject.position = partner.transform.position;
        collidingObject.position += newQuat*offsetFromCenter;
        collidingObject.rotation *= newQuat;



        //alert the linked handler. Laster this should the the only thing left and move the rest of this into a seperate script
        //if(teleportSignal != null) {
        //    teleportSignal.playerTeleported();
        //}
    }


    public void TeleportParameters(ref Vector3 position, ref Vector3 direction, ref Quaternion rotation) {
        /*
         * Change the given parameters as if they were teleported
         */

        /* Get the position difference between the given position and the trigger's center */
        Vector3 offsetFromCenter = position - transform.position;

        /* Get the rotation difference betweem the teleporters */
        Quaternion portalRotationQuat = partner.transform.rotation* Quaternion.Inverse(transform.rotation);

        /* Update the given position and rotation values */
        position = partner.transform.position + portalRotationQuat*offsetFromCenter;
        direction = portalRotationQuat*direction;
        rotation *= portalRotationQuat;
    }
}
