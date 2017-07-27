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
         * Change the given parameters as if they were teleported
         */
        Vector3 upVector = new Vector3(0, 1, 0);

        /* Position the this cube at the starting position */
        if(testCubeThis != null) {
            testCubeThis.transform.position = position + direction*0;
            testCubeThis.transform.rotation = Quaternion.LookRotation(direction, upVector);
        }

        /* Get the proper rotation angle of each portal */
        Quaternion currentTriggerRot = transform.rotation;
        Quaternion partnerTriggerRot = partner.transform.rotation;

        /* Find the amount of distance the given position is from this trigger, ignoring the orientation of the trigger */
        Vector3 positionOffset = Quaternion.Inverse(currentTriggerRot)*(transform.position - position);
        /* Invert the Z position so the ray leaves the partner portal on the same side */
        positionOffset = new Vector3(positionOffset.x, positionOffset.y, positionOffset.z);
        
        /* Apply the partner trigger's rotation to the offset to properly place the position once teleported */
        positionOffset = partnerTriggerRot*positionOffset;

        /* Update the parameters to have them teleport to this portal's partner */
        position = partner.transform.position - positionOffset;
        direction = direction;

        /* Draw a line that represents the new directions */
        Debug.DrawLine(position, position + direction*2f, Color.blue);
        Debug.DrawLine(position, position + upVector*1f, Color.blue);

        
        /* Position the test cube at the new position */
        if(testCubePartner != null) {

            /* Set the position of the partner cube */
            testCubePartner.transform.position = position;

            /* Set the rotation of the partner cube */
            /*
             * so far, any rotation on this portal that is not in the X rotation will fail to rotate properly.
             * 
             * Idea: take the up and forward vector of the portals and compare them?
             * 
             * 
             * current idea to implement:
             * draw the forward vector of each mesh and mess with predefined lines relative to them
             */
            /* Get the starting euler for the teleported object */
            Quaternion newAngle = testCubeThis.transform.rotation;
            float x = 0;
            float y = 0;
            float z = 0;

            //step 0: i think find the roitation difference between the portals

            //1
            z = 0;

            //2
            x += -transform.transform.eulerAngles.x + partner.transform.eulerAngles.x;

            //3
            y += -transform.transform.eulerAngles.y + partner.transform.eulerAngles.y;


            /* Set the rotation of the partner */
            newAngle = Quaternion.Euler(x, y, z);
            testCubePartner.transform.rotation = newAngle;




            ////////////////////////


            /* Draw the forward and up vector of each portal */
            Debug.DrawLine(transform.position, transform.position + transform.forward, Color.red);
            Debug.DrawLine(partner.transform.position, partner.transform.position + partner.transform.forward, Color.red);

            /* Rotate the forward vectors slightly */
            Debug.DrawLine(transform.position, transform.position + Quaternion.AngleAxis(25, transform.up)*transform.forward, Color.red);
            Debug.DrawLine(partner.transform.position, partner.transform.position + Quaternion.AngleAxis(25, partner.transform.up)*partner.transform.forward, Color.red);



            /* Get the difference between the ray's direction and the hit trigger's forward */
            Quaternion rotationDifference = Quaternion.Inverse(transform.rotation)*Quaternion.LookRotation(direction, upVector);
            Debug.Log(rotationDifference.eulerAngles);

            /* Move the partner cube to be at the hit portal and inherit the portal's forward vector */
            //testCubePartner.transform.position = testCubeThis.transform.position;
            testCubePartner.transform.rotation = Quaternion.LookRotation(partner.transform.forward, partner.transform.up);
            //apply the rotation difference gotten from the ray and the hit trigger
            testCubePartner.transform.rotation *= rotationDifference;

            ////////////////////////


            //var rot : Vector3 = Quaternion.LookRotation(otherPortal.forward).eulerAngles;
            //Vector3 eul = Quaternion.LookRotation(partner.forward).eulerAngles;

            //rot += transform.localEulerAngles;
            //eul -= transform.eulerAngles;

            //myDuplicate.localEulerAngles = rot;
            //testCubePartner.transform.rotation = Quaternion.Euler(eul);




            //Get the rotation difference between the two portal;
            //Quaternion partnerRott = partner.transform.rotation;
            //Quaternion rotDiff = Quaternion.Inverse(transform.rotation)*partnerRott;
            //testCubePartner.transform.rotation = rotDiff;
        }


    }
}
