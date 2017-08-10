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
    
    public Quaternion TeleportParameters(ref Vector3 position, ref Quaternion rotation) {
        /*
         * Change the given parameters as if they were teleported from the teleporter 
         * connected to this script to the teleporter assigned to the "partner" variable.
         * When moving the point from one teleporter to another, the point must go from
         * one side of the collision box to the other, as shown by this graphic bellow:
         * 
         * --> = direction, 0 = point of collision, || = both walls of the mesh.
         *  pre-teleport:  -->0| |
         *  post-teleport:     | |0-->
         *  
         * The goal is to have the distance between the collision mesh large enough that the camera's 
         * clipping plane does not clip the portal mesh (trigger must be widder than clipping plane minimum * 2)
         * but also have the trigger small enough that the extra distance teleported equal to the trigger's width
         * is not large enough to have a noticable effect when the player or anything uses the teleporter.
         * 
         * Position determines the point of collision with the ray and the trigger.
         * Rotation determines what direction the position is pointed towards.
         * 
         * Returns the difference in the rotation before and after the teleportation,
         * so it can be applied to other rotations to simulate a teleport.
         */

        /* Get the distance from the collision point to the trigger's origin and rotate it by the partner trigger's rotation */
        Vector3 positionOffset = Quaternion.Inverse(transform.rotation)*(transform.position - position);
        positionOffset = partner.transform.rotation*positionOffset;
        
        /* Get a plane defined by the partner portal's mesh */
        Plane meshPlane = new Plane(partner.transform.position, 
                partner.transform.position + partner.transform.up, 
                partner.transform.position + partner.transform.right);

        /* Get the distance the given position is from the mesh */
        float distanceFromMesh = meshPlane.GetDistanceToPoint(partner.transform.position - positionOffset);

        /* Push the offset to be on the other side and not resting on the trigger */
        positionOffset -= partner.transform.forward*distanceFromMesh*2.0001f;

        
        /* Get the rotation difference between the two portals */
        Quaternion teleporterRotationDifference = partner.transform.rotation*Quaternion.Inverse(transform.rotation);


        /* Update the parameters to have the mproperly teleported */
        position = partner.transform.position - positionOffset;
        rotation = teleporterRotationDifference*rotation;

        /* Draw a line that represents the new directions */
        Vector3 currForward = rotation * Vector3.forward;
        Vector3 currUp = rotation * Vector3.up;
        Debug.DrawLine(position, position + currForward*2f, Color.blue);
        Debug.DrawLine(position, position + currUp*1f, Color.blue);

        return teleporterRotationDifference;
    }
}
