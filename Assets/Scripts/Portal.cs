using UnityEngine;
using System.Collections;

/*
 * Holds references to important elements of the portal.
 * 
 * Portals will have a "hiddenLights" list, which is a list of lights between both portals that are
 * hidden and are used to emulate the light from light fixtures passing through the portals.
 * These lights are only active when the portal's door is open.
 */
public class Portal : MonoBehaviour {
    
    /* The "entrance" portalObjects of the portal the user will consider an "entrance" */
    public PortalObjects EntrancePortal;

    /* The exit portalObjects of the entrance portal that leads into a new area */
    public PortalObjects ExitPortal;

    /* An array of extra "entrance" portalObjects. These will most likely not be teleporting the player */
    public PortalObjects[] ExtraEntrancePortals;
    

    public void SetPortalAngle(float x, float y, float z) {
        /*
         * Set the angle of each door linked to this portal
         */

        //EntrancePortal.portalDoor.transform.localEulerAngles = new Vector3(x, y, z);
        //ExitPortal.portalDoor.transform.localEulerAngles = new Vector3(x, y, z);
        
        //for(int i = 0; i < ExtraEntrancePortals.Length; i++) {
            //ExtraEntrancePortals[i].portalDoor.transform.localEulerAngles = new Vector3(x, y, z);
        //}
    }

    public void SetPortalsActiveState(bool closed) {
        /*
         * Set the portal's meshes active state
         */

        EntrancePortal.portalMesh.SetActive(closed);
        ExitPortal.portalMesh.SetActive(closed);

        for(int i = 0; i < ExtraEntrancePortals.Length; i++) {
            ExtraEntrancePortals[i].portalMesh.SetActive(closed);
        }
    }
}
