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

    /* The object used as a border for the portal mesh. Leave it empty for a default border */
    public GameObject portalBorder;

    /* The mesh of the portal. If this is null, then the defaultPortalSize will be used to create the mesh */
    public Mesh portalMesh;
    public float defaultWidth;
    public float defaultHeight;

    /* positional offset of the portal */
    public Vector3 portalOffset;

    /* Depth of the portal's trigger. The higher value, the more noticable of a jump will occur on teleport */
    public float portalThickness;


    /* -------- Built-In Unity Functions ---------------------------------------------------- */

    void Update() {

        /* Create and link the meshes of the portals */
        CreateMesh();
        
        /* Fix the transform of the portal's triggers */
        UpdateTriggers();
    }

    void Start() {
        /*
         * Leave the border creation in the start function until we start to work on borders.
         */

        /* Create and link the borders around each linked portal */
        CreateBorder();
    }


    /* -------- Update Functions ---------------------------------------------------- */

    void UpdateTriggers() {
        /*
         * Adjust the position, rotation and scale of the triggers.
         */

        EntrancePortal.SetTriggersSizes(defaultWidth, defaultHeight, portalThickness);
        ExitPortal.SetTriggersSizes(defaultWidth, defaultHeight, portalThickness);
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
         * The border used will be the portalBorder object. If it is null, a default border will be used.
         */

        if(portalBorder) {
            EntrancePortal.SetBorder(portalBorder);
            ExitPortal.SetBorder(portalBorder);
        }
        else {
            Debug.Log("CREATING A DEFAULT BORDER");
        }
    }


    /* -------- Event Functions ---------------------------------------------------- */

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
