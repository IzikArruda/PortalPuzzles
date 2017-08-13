using UnityEngine;
using System.Collections;

/*
 * Attach this to the game object that parents the objects of a singular portal. It will link to
 * the portal's mesh and door objects.
 * 
 * portalMesh being the gameObject that contains the MeshRenderer
 */
[ExecuteInEditMode]
public class PortalObjects : MonoBehaviour {

    public GameObject portalMesh;
    public GameObject teleporterEnterTrigger;
    public GameObject teleporterLeaveTrigger;
    //public GameObject portalDoor;

    /* The sizes of the portal mesh */
    public float portalWidth;
    public float portalHeight;
    /* positional offset of the portal */
    public Vector3 portalOffset;

    public Vector3 wierdMeshOffset;

    /* A link to the border that surrounds the contour of the portal mesh. */
    public GameObject border;


    /* A link to each child container of the portal */
    public Transform meshContainer;
    public Transform TriggerContainer;
    public Transform borderContainer;



    /* Update the sizes of the mesh of the given portals using the saved sizes */
    public void CreatePortalMesh() {
        /*
         * Create the portal mesh that the player will be viewing using the portal's size parameters.
         * 
         * A portal mesh's (0, 0, 0) will be on the mesh's bottom center.
         */

        ////NOTE: THE PARTNER WILL ALOS NEED TO HAVE IT'S MESH CHANGED

        Mesh mesh = new Mesh();
        Vector3[] vertices;
        Vector2[] UV;
        int[] triangles;

        /* Set the vertices for the portal mesh */
        //NOTE: WHY DO I NEED TO REMOVE 0.25F to allign the meshes textures?
        vertices = new Vector3[] {
                new Vector3(-portalWidth, portalHeight, 0) + portalOffset,
                new Vector3(0, portalHeight, 0) + portalOffset,
                new Vector3(0, 0, 0) + portalOffset,
                new Vector3(-portalWidth, 0, 0) + portalOffset
            };

        /* Set the two polygons that form the portal mesh */
        triangles = new int[] {
                3, 2, 1, 3, 1, 0
            };

        /* Set the UVs for the portal mesh */
        UV = new Vector2[] {
                new Vector2(0, 0),
                new Vector2(0, 1),
                new Vector2(1, 0),
                new Vector2(1, 1)
            };

        /* Assign the mesh to renderer */
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        //mesh.uv = UV;
        portalMesh.GetComponent<MeshFilter>().mesh = mesh;



        //The thickness of the portal should be a small amout larger than twice the camera's near clipping plane
        //The Offset of the portal should be slightly larger than the camera's near clipping plane instead
        float portalThickness = 0.01f;
        /*  Set the teleport triggers linked to the portal to be the same sizes of the portal mesh */
        teleporterEnterTrigger.transform.localEulerAngles = new Vector3(0, 0, 0);
        teleporterEnterTrigger.transform.localScale = transform.localScale;
        teleporterEnterTrigger.transform.localPosition = new Vector3(-portalWidth/2f, portalHeight/2f, 0);
        teleporterEnterTrigger.GetComponent<BoxCollider>().center = new Vector3(0, 0, 0);
        teleporterEnterTrigger.GetComponent<BoxCollider>().size = new Vector3(portalWidth, portalHeight, portalThickness);

        teleporterLeaveTrigger.transform.localEulerAngles = new Vector3(0, 0, 0);
        teleporterLeaveTrigger.transform.localScale = transform.localScale;
        teleporterLeaveTrigger.transform.localPosition = new Vector3(-portalWidth/2f, portalHeight/2f, 0); ;
        teleporterLeaveTrigger.GetComponent<BoxCollider>().center = new Vector3(0, 0, 0);
        teleporterLeaveTrigger.GetComponent<BoxCollider>().size = new Vector3(portalWidth, portalHeight, portalThickness);

        
    }


    public void SetBorder(GameObject borderObject) {
        /*
         * Set the border of the portal to be the given gameObject. Duplicate the given object and 
         * set it's parent and transform to be this portal's proper border.
         */

        /* Do not reset the border if one already exists */
        if(border == null) {
            border = Instantiate(borderObject);
            border.transform.parent = borderContainer;
            border.transform.localPosition = new Vector3(0, 0, 0);
            border.transform.localEulerAngles = new Vector3(0, 0, 0);
        }
    }
}
