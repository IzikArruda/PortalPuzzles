using UnityEngine;
using System.Collections;

/*
 * Holds links to all relevent gameObjects for a portal, such as the triggers, the mesh and the containers.
 * It is often called by it's parent, an empty transform with a PortalSet script.
 */
public class PortalObjects : MonoBehaviour {

    /* The gameObject that contains the portal's mesh and PortalView script */
    public GameObject portalMesh;

    /* The two triggers that the player will teleport between for this portal */
    public GameObject teleporterEnterTrigger;
    public GameObject teleporterLeaveTrigger;
    
    /* A link to each child container of the portal */
    public Transform meshContainer;
    public Transform TriggerContainer;
    public Transform borderContainer;

    /* A link to the border that surrounds the contour of the portal mesh. */
    public GameObject border;


    /* -------- Setters ---------------------------------------------------- */

    public void SetMesh(Mesh mesh) {
        /*
         * Link the given mesh to the meshRenderer that is used to dispaly the portal.
         */
         
        portalMesh.GetComponent<MeshFilter>().mesh = mesh;
    }
    
    public void SetTriggersTransform(float width, float height, float depth) {
        /*
         * Set the position, rotation and scale of the portal's triggers using the given parameters.
         */

        /* Set the properties of this script's portal's trigger */
        teleporterEnterTrigger.transform.localEulerAngles = new Vector3(0, 0, 0);
        teleporterEnterTrigger.transform.localScale = transform.localScale;
        teleporterEnterTrigger.transform.localPosition = new Vector3(-width/2f, height/2f, 0);
        teleporterEnterTrigger.GetComponent<BoxCollider>().center = new Vector3(0, 0, 0);
        teleporterEnterTrigger.GetComponent<BoxCollider>().size = new Vector3(width, height, depth);

        /* Set the properties of the trigger at the partner portal */
        teleporterLeaveTrigger.transform.localEulerAngles = new Vector3(0, 0, 0);
        teleporterLeaveTrigger.transform.localScale = transform.localScale;
        teleporterLeaveTrigger.transform.localPosition = new Vector3(-width/2f, height/2f, 0); ;
        teleporterLeaveTrigger.GetComponent<BoxCollider>().center = new Vector3(0, 0, 0);
        teleporterLeaveTrigger.GetComponent<BoxCollider>().size = new Vector3(width, height, depth);
    }
    
    public void SetBorder(GameObject borderObject) {
        /*
         * Set the border of the portal to be the given gameObject. Duplicate the given object and 
         * set it's parent and transform to be this portal's proper border. 
         */

        /* Delete any border objects that already exist */
        foreach(Transform child in borderContainer) {
            DestroyImmediate(child.gameObject);
        }

        /* Create the new border */
        border = Instantiate(borderObject);
        border.transform.parent = borderContainer;
        border.transform.localPosition = new Vector3(0, 0, 0);
        border.transform.localEulerAngles = new Vector3(0, 0, 0);
    }
}
