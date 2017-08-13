using UnityEngine;
using System.Collections;

/*
 * A set of portalObjects that will be linked together. This script calls their Setter functions to 
 * properly set the portal's parameters (mesh, triggers, borders) to be identical.
 */
[ExecuteInEditMode]
public class PortalSet : MonoBehaviour {
    
    /* The two portals that are linked together */
    public PortalObjects EntrancePortal;
    public PortalObjects ExitPortal;

    /* The object used as a border for the portal mesh */
    public GameObject portalBorder;

    /* The mesh of the portal. If this is null, then the defaultPortalSize will be used to create the mesh */
    public Mesh portalMesh;
    public float defaultWidth;
    public float defaultHeight;

    /* positional offset of the portal */
    public Vector3 portalOffset;

    /* Depth of the portal's trigger. The higher value, the more noticable of a jump will occur on teleport */
    public float portalThickness;

    /* A temporary value for ease of access to update the borders of the portals */
    public bool updateBorders;


    /* -------- Built-In Unity Functions ---------------------------------------------------- */

    void Update() {

        /* Adjust the positionning of the portals */
        UpdatePortalPosition();

        /* Create and link the meshes of the portals */
        CreateMesh();
        
        /* Fix the transform of the portal's triggers */
        UpdateTriggers();
        
        /* Update the borders for the portals if needed */
        UpdateTheBorders();
    }
    


    /* -------- Update Functions ---------------------------------------------------- */

    void UpdatePortalPosition() {
        /*
         * Depending on the sizes of the portals, change the local position of the portals.
         * Since the two portals are "mirrors", one of them will need to be reversed.
         */

        /* Keep the entrance portal in it's neutral position */
        EntrancePortal.meshContainer.localPosition = new Vector3(0, 0, 0);
        EntrancePortal.meshContainer.localEulerAngles = new Vector3(0, 0, 0);

        /* Reverse the exit portal by simply changing it's mesh's transform. (Yeuler = 180, X pos = -width) */
        ExitPortal.meshContainer.localPosition = new Vector3(-defaultWidth, 0, 0);
        ExitPortal.meshContainer.localEulerAngles = new Vector3(0, 180, 0);
    }

    void UpdateTriggers() {
        /*
         * Adjust the position, rotation and scale of the triggers.
         */

        EntrancePortal.SetTriggersTransform(defaultWidth, defaultHeight, portalThickness);
        ExitPortal.SetTriggersTransform(defaultWidth, defaultHeight, portalThickness);
    }
    
    void CreateMesh() {
        /*
         * Create the mesh for the linked portals using the given portalMesh. If there is no portalMesh,
         * use a generated mesh with the given defaultMeshSize values.
         */
        Mesh mesh;

        /* Get either the linked mesh for the portal or create the the default */
        if(portalMesh) {
            mesh = portalMesh;
        }else {
            mesh = CreateDefaultMesh();
        }

        /* Assign the mesh to each linked portalObject */
        EntrancePortal.SetMesh(mesh);
        ExitPortal.SetMesh(mesh);
    }
    
    void CreateBorder() {
        /*
         * Create a border for each portal. They will have the same border to ensure consistensy between portals.
         * The border used will be the portalBorder object, which is expected to be a prefab.
         */

        if(portalBorder) {
            /* Assign the new border to the two portals */
            EntrancePortal.SetBorder(portalBorder);
            ExitPortal.SetBorder(portalBorder);
        }
        else {
            Debug.Log("NO BORDER GIVEN");
        }
    }


    /* -------- Event Functions ---------------------------------------------------- */

    void UpdateTheBorders() {
        /*
         * Check if the borders need to be updated. This occurs when the "updateBorders" bolean is set to true
         */


        if(updateBorders == true) {
            CreateBorder();
            updateBorders = false;
        }
    }

    Mesh CreateDefaultMesh() {
        /*
         * Create the default mesh for the portal using the default width and height. It does not need UVs
         */
        Mesh defaultMesh = new Mesh();
        Vector3[] vertices;
        int[] triangles;

        /* Set the vertices for the mesh */
        vertices = new Vector3[] {
                new Vector3(-defaultWidth, defaultHeight, 0) + portalOffset,
                new Vector3(0, defaultHeight, 0) + portalOffset,
                new Vector3(0, 0, 0) + portalOffset,
                new Vector3(-defaultWidth, 0, 0) + portalOffset
            };

        /* Set the two polygons that form the mesh */
        triangles = new int[] {
                3, 2, 1, 3, 1, 0
        };

        /* Assign the properties of the mesh */
        defaultMesh.vertices = vertices;
        defaultMesh.triangles = triangles;

        return defaultMesh;
    }

    
}
