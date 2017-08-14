using UnityEngine;
using System.Collections;

/*
 * A set of 2 portalObjects that will be linked together. This script calls their Setter functions to 
 * properly set the portal's parameters (mesh, triggers, borders) to be identical.
 */
[ExecuteInEditMode]
public class PortalSet : MonoBehaviour {
    
    /* The two portals that are linked together */
    public PortalObjects EntrancePortal;
    public PortalObjects ExitPortal;

    /* The object used as a border for the portal mesh */
    public GameObject portalBorder;

    /* The mesh of the portal. If this is null, then a rectangle mesh will be created */
    public Mesh portalMesh;

    /* The sizes of the portal */
    public float portalMeshWidth;
    public float portalMeshHeight;

    /* positional offset of the portal */
    public Vector3 portalOffset;

    /* Depth of the portal's trigger. The higher value, the more noticable of a jump will occur on teleport */
    public float portalThickness;

    /* A temporary value for ease of access to update the borders of the portals */
    public bool updateBorders;


    /* If the portal is centered on it's origin point. Else it protrudes from the origin. */
    public bool portalsCentered;


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

        /* Add an offset if the portals need to be centered onto their origin */
        Vector3 centeredOffset = Vector3.zero;
        if(portalsCentered) {
            centeredOffset = new Vector3(portalMeshWidth/2f, 0, 0);
        }

        /* Reposition the portal's and their containers */
        EntrancePortal.meshContainer.localPosition = new Vector3(0, 0, 0) + centeredOffset;
        EntrancePortal.TriggerContainer.localPosition = new Vector3(0, 0, 0) + centeredOffset;
        EntrancePortal.borderContainer.localPosition = new Vector3(-portalMeshWidth/2f, 0, 0) + centeredOffset;
        ExitPortal.meshContainer.localPosition = new Vector3(-3, 0, 0) + centeredOffset;
        ExitPortal.TriggerContainer.localPosition = new Vector3(0, 0, 0) + centeredOffset;
        ExitPortal.borderContainer.localPosition = new Vector3(-portalMeshWidth/2f, 0, 0) + centeredOffset;

        /* Ensure the rotation of the portalm meshes are correct */
        EntrancePortal.meshContainer.localEulerAngles = new Vector3(0, 0, 0);
        ExitPortal.meshContainer.localEulerAngles = new Vector3(0, 180, 0);
    }

    void UpdateTriggers() {
        /*
         * Adjust the position, rotation and scale of the triggers.
         */

        EntrancePortal.SetTriggersTransform(portalMeshWidth, portalMeshHeight, portalThickness);
        ExitPortal.SetTriggersTransform(portalMeshWidth, portalMeshHeight, portalThickness);
    }
    
    void CreateMesh() {
        /*
         * Create the mesh for the linked portals using the given portalMesh. If there is no portalMesh,
         * use a generated mesh with the given defaultMeshSize values.
         */
        Mesh mesh1, mesh2;

        /* Get either the linked mesh for the portal or create the the default */
        if(portalMesh) {
            mesh1 = portalMesh;
            mesh2 = portalMesh;
        }
        else {
            mesh1 = CreateDefaultMesh(false);
            mesh2 = CreateDefaultMesh(true);
        }

        /* Assign the mesh to each linked portalObject */
        EntrancePortal.SetMesh(mesh1);
        ExitPortal.SetMesh(mesh2);
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

    Mesh CreateDefaultMesh(bool reflectMesh) {
        /*
         * Create the default mesh for the portal using the default width and height. It does not need UVs.
         * 
         * When handling the exit portal's mesh, add an offset to the mesh to properly "reflect" the portal.
         */
        Mesh defaultMesh = new Mesh();
        Vector3[] vertices;
        int[] triangles;

        /* If it's the reflected mesh, apply another offset. The "-3" is because there is a 
         * magic 3 hidden somewhere related to */
        Vector3 reflectionOffset = Vector3.zero;
        if(reflectMesh) {
            reflectionOffset = new Vector3(portalMeshWidth-3, 0, 0);
        }


        /* Set the vertices for the mesh */
        vertices = new Vector3[] {
                new Vector3(-portalMeshWidth, portalMeshHeight, 0) + portalOffset + reflectionOffset,
                new Vector3(0, portalMeshHeight, 0) + portalOffset + reflectionOffset,
                new Vector3(0, 0, 0) + portalOffset + reflectionOffset,
                new Vector3(-portalMeshWidth, 0, 0) + portalOffset + reflectionOffset
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
