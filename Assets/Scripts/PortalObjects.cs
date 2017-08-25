using UnityEngine;
using System.Collections;

/*
 * Holds links to all relevent gameObjects for a portal, such as the triggers, the mesh and the containers.
 * It is often called by it's parent, an empty transform with a PortalSet script.
 */
public class PortalObjects : MonoBehaviour {

    /* The gameObject that contains the portal's mesh and PortalView script */
    public GameObject portalMesh;

    /* The exit point of the portalMesh */
    public GameObject portalMeshExitPoint;

    /* The partner portalMesh for the other side of the portal to make it double sided */
    public GameObject backwardsPortalMesh;

    /* The exit point of the backwardsPortalMesh */
    public GameObject backwardsPortalMeshExitPoint;
    
    /* The trigger that the player will teleport between for this portal */
    public GameObject teleporterEnterTrigger;
    
    /* A link to each child container of the portal */
    public Transform meshContainer;
    public Transform TriggerContainer;
    public Transform borderContainer;

    /* A link to the border that surrounds the contour of the portal mesh. */
    public GameObject border;
    

    /* -------- Setters ---------------------------------------------------- */

    public void SetPortalTransforms() {
        /*
         * Set the transforms of the portal's meshes and exit points.
         */
         
        portalMesh.transform.localPosition = new Vector3(0, 0, 0);
        portalMesh.transform.localEulerAngles = new Vector3(0, 0, 0);
        backwardsPortalMesh.transform.localPosition = new Vector3(0, 0, 0);
        backwardsPortalMesh.transform.localEulerAngles = new Vector3(0, 180, 0);
        portalMeshExitPoint.transform.localPosition = new Vector3(0, 0, 0);
        portalMeshExitPoint.transform.localEulerAngles = new Vector3(0, 180, 0);
        backwardsPortalMeshExitPoint.transform.localPosition = new Vector3(0, 0, 0);
        backwardsPortalMeshExitPoint.transform.localEulerAngles = new Vector3(0, 0, 0);
    }

    public void SetMesh(Mesh mesh, Mesh backwardsMesh) {
        /*
         * Link the given mesh to the meshRenderer that is used to display the portal.
         */
         
        portalMesh.GetComponent<MeshFilter>().mesh = mesh;
        backwardsPortalMesh.GetComponent<MeshFilter>().mesh = backwardsMesh;
    }
    
    public void SetTriggersTransform(float width, float height, float depth, Vector3 offSet) {
        /*
         * Set the position, rotation and scale of the portal's triggers using the given parameters.
         */

        /* Set the properties of this script's portal's trigger */
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
