using UnityEngine;
using System.Collections;

/*
 * Attach this to a trigger that is placed in-front of the portal's mesh. The trigger must
 * have it's local positive X axis face towards the portal's mesh. When teleporting the player,
 * it can send a "teleported" signal to the linked TeleportHandler if needed.
 */
public class TeleporterTrigger : MonoBehaviour {

    public Transform partner;
    //public TeleportHandler teleportSignal;

    void OnTriggerStay(Collider collider) {
        /*
         * Check if the player will need to be teleported
         */

        if(collider.tag == "Player") {
            /* Get the plane formed by the trigger */
            Vector3 planeNormal = transform.rotation*new Vector3(1, 0, 0);
            Plane triggerPlane = new Plane(planeNormal, transform.position);

            /* Check if the center of the colliding object is between the trigger's center and the portal's mesh */
            if(triggerPlane.GetSide(collider.transform.position)) {
                TeleportCollider(collider.transform);
            }
        }
    }

    void TeleportCollider(Transform collidingObject) {
        /* 
         * Teleport the given transform to the teleporter's patner's location and signal the linked teleport handler
         */
         
        /* Get the position difference between the player and the trigger's center */
        Vector3 teleportOffset = collidingObject.position - transform.position;

        /* Get the rotation difference betweem the teleporters and apply it to the player */
        Quaternion newQuat = partner.transform.rotation* Quaternion.Inverse(transform.rotation);
        
        /* Move and rotate the player relative to the position and rotation differences */
        collidingObject.position = partner.transform.position;
        collidingObject.position += newQuat*teleportOffset;
        collidingObject.rotation *= newQuat;



        //alert the linked handler. Laster this should the the only thing left and move the rest of this into a seperate script
        //if(teleportSignal != null) {
        //    teleportSignal.playerTeleported();
        //}
    }
}
