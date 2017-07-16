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


    public void TeleportParameters(ref Vector3 position, ref Vector3 direction, ref Quaternion rotation) {
        /*
         * Change the given parameters as if they were teleported
         */
        float portalThickness = 0.01f;
        ///////GOAL: GET THE PORTALS TO PROPERLY TELEPORT THE RAY. CURRENTLY ITS NOT CORRECTLY BEING TELEPORTED





        /* Rotation difference between portals */
        Quaternion roitationDiff = transform.rotation * partner.transform.rotation;


        /* Get the position difference between the given position and the trigger's center */
        Vector3 offsetFromCenter = position - transform.position;

        /* Get the rotation difference betweem the teleporters */
        Quaternion portalRotationQuat = partner.transform.rotation*transform.rotation;

        /* Update the given position and rotation values */
        //position = partner.transform.position + offsetFromCenter;
        //direction = portalRotationQuat*direction;
        //rotation *= portalRotationQuat;








        //FIX TRIGGERS AND HOW THEY HAVE A ROTATION
        ////////////PROPERLY HANDLES ANY ROTATION PUT INTO THIS TRIGGER. IT WILL STILL NEED TO HANDLE ROTATION PUT
        ////////////INTO THE PARTNER TRIGGER. CURRENTLY IT PROPERLY ROTATES AND MOVES THE PARAMETERS UPON TELEPORT
        ////////////ASSUMING THE PARTNER PORTAL'S ROTATIONS ARE SET TO 0 WHEN POSSIBLE (EXCLUDING THE TRIGGER'S 90 IN Y)
        //proper this handle:
        /*
         * Quaternion finalRot = Quaternion.Euler(currentTriggerRot.eulerAngles.z,
                -currentTriggerRot.eulerAngles.y+180,
                currentTriggerRot.eulerAngles.x);
         */

        /* Get the proper rotation angle of each portal */
        Quaternion currentTriggerRot = transform.rotation;
        Quaternion partnerTriggerRot = partner.transform.rotation;
        //Quaternion finalRot = Quaternion.Inverse(partnerTriggerRot)*currentTriggerRot;
        //ONE OF THESE IS STILL WRONG
        Quaternion finalRot = Quaternion.Euler(currentTriggerRot.eulerAngles.x + 0,
                -currentTriggerRot.eulerAngles.y+180 + 0,
                currentTriggerRot.eulerAngles.z + 0);
        finalRot = Quaternion.Inverse(currentTriggerRot)*partnerTriggerRot;
        finalRot.eulerAngles = new Vector3(finalRot.eulerAngles.x, finalRot.eulerAngles.y+180, finalRot.eulerAngles.z);
        //Thinking amybe we need to rotate it arounfd the y axis by 180 degrees


        Debug.Log(finalRot.eulerAngles.x + " _ " +  finalRot.eulerAngles.y + " _ " +  finalRot.eulerAngles.z);

        /* Find the amount of distance the given position is from this trigger, ignoring the orientation of the trigger */
        Vector3 positionOffset = Quaternion.Inverse(currentTriggerRot)*(transform.position - position);
        /* Invert the Z position so the ray leaves the partner portal on the same side */
        positionOffset = new Vector3(-positionOffset.x, positionOffset.y, positionOffset.z);



        /* Apply the partner trigger's rotation to the offset to properly place the position once teleported */
        positionOffset = partnerTriggerRot*positionOffset;



        ///Every rotation applied will need to be added to the rotation parameter
        //rotation *= Quaternion. currentTriggerRot*partnerTriggerRot;
        


        /* Update the parameters to have them teleport to this portal's partner */
        position = partner.transform.position - positionOffset;
        direction = finalRot*direction;
        rotation = rotation;
    }
}
