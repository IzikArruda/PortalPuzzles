using UnityEngine;
using System.Collections;

/*
 * Holds links to all relevent gameObjects for a portal, such as the triggers, the mesh and the containers.
 * It is often called by it's parent, an empty transform with a PortalSet script.
 */
public class PortalObjects : MonoBehaviour {
    
    /* The gameObject that contains the portal's mesh and PortalView script */
    public GameObject portalMesh;
    
    /* The partner portalMesh for the other side of the portal to make it double sided */
    public GameObject backwardsPortalMesh;
    
    /* The trigger that the player will teleport between for this portal */
    public GameObject teleporterEnterTrigger;
    
    /* A link to each child container of the portal */
    public Transform meshContainer;
    public Transform TriggerContainer;
    public Transform borderContainer;

    /* A link to the border that surrounds the contour of the portal mesh. */
    public GameObject border;


    /* -------- Setters ---------------------------------------------------- */

    public void SetContainersTransforms(Vector3 centeredOffset, float width) {
        /*
         * Set the transforms of the containers and this portalObject's portalMeshes.
         */

        /* Set the positions and rotations that are not entrance/exit dependent */
        meshContainer.localPosition = centeredOffset;
        TriggerContainer.localPosition = centeredOffset;
        TriggerContainer.localEulerAngles = new Vector3(0, 0, 0);
        borderContainer.localPosition = new Vector3(-width/2f, 0, 0) + centeredOffset;
        borderContainer.localEulerAngles = new Vector3(0, 0, 0);
        portalMesh.transform.localEulerAngles = new Vector3(0, 0, 0);
        backwardsPortalMesh.transform.localEulerAngles = new Vector3(0, 180, 0);
    }

    public void SetPortalTransforms(float width, bool isEntrance) {
        /*
         * place the portalMeshes. Depending on the value of isEntrance, their positions will change.
         */
         
        if(isEntrance) {
            portalMesh.transform.localPosition = new Vector3(0, 0, 0);
            backwardsPortalMesh.transform.localPosition = new Vector3(-width, 0, 0);
            meshContainer.localEulerAngles = new Vector3(0, 0, 0);
        }
        else {
            portalMesh.transform.localPosition = new Vector3(width, 0, 0);
            backwardsPortalMesh.transform.localPosition = new Vector3(0, 0, 0);
            meshContainer.localEulerAngles = new Vector3(0, 180, 0);
        }
    }

    public void SetMesh(Mesh mesh) {
        /*
         * Link the given mesh to the MeshFilter that is used to display the portal.
         */

        portalMesh.GetComponent<MeshFilter>().mesh = mesh;
        backwardsPortalMesh.GetComponent<MeshFilter>().mesh = mesh;
    }

    public void SetTriggers(float width, float height, float depth, Vector3 offSet) {
        /*
         * Set the position, rotation and scale of the portal's triggers using the given parameters.
         */

        teleporterEnterTrigger.transform.localEulerAngles = new Vector3(0, 0, 0);
        teleporterEnterTrigger.transform.localScale = transform.localScale;
        teleporterEnterTrigger.transform.localPosition = new Vector3(-width/2f, height/2f, 0) + offSet;
        teleporterEnterTrigger.GetComponent<BoxCollider>().center = new Vector3(0, 0, 0);
        teleporterEnterTrigger.GetComponent<BoxCollider>().size = new Vector3(width, height, depth);
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
        border.name = borderObject.name;
        border.transform.parent = borderContainer;
        border.transform.localPosition = new Vector3(0, 0, 0);
        border.transform.localEulerAngles = new Vector3(0, 0, 0);
    }
    
}
