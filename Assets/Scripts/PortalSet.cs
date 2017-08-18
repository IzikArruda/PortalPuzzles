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

    /* The mesh of the portal. If this is null, a default rectangle mesh will be created and assigned. */
    public Mesh portalMesh;

    /* The sizes of the default portal mesh and the triggers. */
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

    /* The offsets of the triggers for the portals */
    public Vector3 triggerOffset;


    /* Depth of the default border */
    public float defaultBorderDepth;

    /* The Widths of the default border's sides. 0 or less means the side will not be created. */
    public float defaultBorderLeft;
    public float defaultBorderRight;
    public float defaultBorderTop;
    public float defaultBorderBottom;

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
        EntrancePortal.meshContainer.localPosition = centeredOffset;
        EntrancePortal.TriggerContainer.localPosition = centeredOffset;
        EntrancePortal.borderContainer.localPosition = new Vector3(-portalMeshWidth/2f, 0, 0) + centeredOffset;
        ExitPortal.meshContainer.localPosition = centeredOffset;
        ExitPortal.TriggerContainer.localPosition = centeredOffset;
        ExitPortal.borderContainer.localPosition = new Vector3(-portalMeshWidth/2f, 0, 0) + centeredOffset;

        /* Ensure the rotation of the portalm meshes are correct */
        EntrancePortal.meshContainer.localEulerAngles = new Vector3(0, 0, 0);
        ExitPortal.meshContainer.localEulerAngles = new Vector3(0, 180, 0);
    }

    void UpdateTriggers() {
        /*
         * Adjust the position, rotation and scale of the triggers along with their offset.
         */

        //NOTE: when the player is very close to the mesh for a portal, the rendering order of the portal mesh will 
        //imrproperly render certain objects.
        EntrancePortal.SetTriggersTransform(portalMeshWidth, portalMeshHeight, portalThickness, triggerOffset);
        ExitPortal.SetTriggersTransform(portalMeshWidth, portalMeshHeight, portalThickness, triggerOffset);
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
         * The border used will be the portalBorder object, which is expected to be a prefab. If this object
         * is null, then use a generated default border defined by this script's defaultBorder values.
         */

        /* Assign the new border to the two portals */
        if(portalBorder) {
            EntrancePortal.SetBorder(portalBorder);
            ExitPortal.SetBorder(portalBorder);
        }

        /* Create a default border and assign it to the portals */
        else {
            GameObject newBorders = CreateDefaultBorder();
            EntrancePortal.SetBorder(newBorders);
            ExitPortal.SetBorder(newBorders);
            //Delete the object once it has been set
            DestroyImmediate(newBorders.gameObject);
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

        /* If it's the reflected mesh, apply another offset */
        Vector3 reflectionOffset = Vector3.zero;
        if(reflectMesh) {
            reflectionOffset = new Vector3(portalMeshWidth, 0, 0);
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

    GameObject CreateDefaultBorder() {
        /*
         * Use the script's variables to create a basic border around the portal's mesh.
         * Any defaultBorder size value that is equal or less than 0 will not be created
         */
        GameObject borderPiece;
        GameObject newBorders = new GameObject();
        Vector3 centerPoint = new Vector3(0, 0, 0);
        newBorders.name = "Default Border Parent";

        
        /* Create the right side of the border piece */
        if(defaultBorderRight > 0) {
            centerPoint = new Vector3(portalMeshWidth/2f + defaultBorderRight, portalMeshHeight/2f, 0);
            borderPiece = CreateBox(centerPoint, defaultBorderRight, portalMeshHeight/2f, defaultBorderDepth);
            borderPiece.name = "Right side";
            borderPiece.transform.parent = newBorders.transform;
        }

        /* Create the left side of the border piece */
        if(defaultBorderLeft > 0) {
            centerPoint = new Vector3(-portalMeshWidth/2f - defaultBorderLeft, portalMeshHeight/2f, 0);
            borderPiece = CreateBox(centerPoint, defaultBorderLeft, portalMeshHeight/2f, defaultBorderDepth);
            borderPiece.name = "Left side";
            borderPiece.transform.parent = newBorders.transform;
        }

        /* Create the top side of the border piece */
        if(defaultBorderTop > 0) {
            centerPoint = new Vector3(0, portalMeshHeight + defaultBorderTop, 0);
            borderPiece = CreateBox(centerPoint, portalMeshWidth/2f, defaultBorderTop, defaultBorderDepth);
            borderPiece.name = "Top side";
            borderPiece.transform.parent = newBorders.transform;
        }

        /* Create the bottom side of the border piece */
        if(defaultBorderBottom > 0) {
            centerPoint = new Vector3(0, -defaultBorderBottom, 0);
            borderPiece = CreateBox(centerPoint, portalMeshWidth/2f, defaultBorderBottom, defaultBorderDepth);
            borderPiece.name = "Bottom side";
            borderPiece.transform.parent = newBorders.transform;
        }

        return newBorders;
    }


    /* -------- Helper Functions ---------------------------------------------------- */
    
    GameObject CreateBox(Vector3 center, float x, float y, float z) {
        /*
         * Create a box using the given parameters
         */
        GameObject cube = new GameObject();
        Mesh mesh = new Mesh();
        Vector3[] vertices;
        int[] triangles;

        vertices = new Vector3[] {
            center + new Vector3(-x, -y, -z),
            center + new Vector3(x, -y, -z),
            center + new Vector3(-x, y, -z),
            center + new Vector3(-x, -y, z),
            center + new Vector3(x, -y, z),
            center + new Vector3(x, y, -z),
            center + new Vector3(-x, y, z),
            center + new Vector3(x, y, z)
        };

        triangles = new int[] {
            0, 2, 1,  1, 2, 5,
            3, 0, 1,  1, 4, 3,
            0, 3, 2,  2, 3, 6,
            1, 5, 4,  5, 7, 4,
            6, 3, 4,  6, 4, 7,
            6, 5, 2,  7, 5, 6
        };

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        
        cube.AddComponent<MeshFilter>().mesh = mesh;
        cube.AddComponent<MeshRenderer>();


        return cube;
    }
}