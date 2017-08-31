using UnityEngine;
using System.Collections;

/*
 * A set of 2 portalObjects that will be linked together. This script calls their Setter functions to 
 * properly set the portal's parameters (mesh, triggers, borders).
 */
[ExecuteInEditMode]
public class PortalSet : MonoBehaviour {

    /* The name of the portalSet */
    public string objectName;

    /* The two portals that are linked together */
    public PortalObjects EntrancePortal;
    public PortalObjects ExitPortal;

    /* positional offset of the portal's mesh without changing the trigger position */
    public Vector3 portalOffset;

    /* The mesh of the portal. If this is null, a default rectangle mesh will be created and assigned. */
    public Mesh portalMesh;

    /* The sizes of the default portal mesh, the triggers, and the position of the backwards portalMesh */
    public float portalWidth;
    public float portalHeight;
    private float previousWidth = 0;
    private float previousHeight = 0;

    /* If the portal is centered on it's origin point. Else it protrudes from the origin. */
    public bool portalsCentered;

    /* Positional offset of the triggers for the portals */
    public Vector3 triggerOffset;

    /* Depth of the portal's trigger. The higher value, the more noticable of a jump will occur on teleport */
    public float triggerThickness;
    
    /* The object to be used as a border for the portal mesh. If this is null, a default border will be created */
    public GameObject portalBorder;

    /* Depth of the default border */
    public float defaultBorderDepth;

    /* The Widths of the default border's sides. 0 or less means the side will not be created. */
    public float defaultBorderLeft;
    public float defaultBorderRight;
    public float defaultBorderTop;
    public float defaultBorderBottom;
    
    /* When true, will update the given object portal's meshes, triggers and borders */
    public bool updatePortal;
    

    /* ---------- LAYERS DO NOT SEEM TO BE NEEDED ANYMORE, SO COMMENT THEM OUT UNTIL FURTHER TESTING --------- */

    /* A static array that tracks all currently used protalMesh layers. True indicates the layer is active already */
    //private static bool[] availableLayers;

    /* The range of layesr that the portal can occupy. do not change these at run-time. */
    //public static int minLayer = 9;
    //public static int maxLayer = 30;

    /* The current layer this portalSet occupies */
    //private int currentLayer = -1;

    /* -------- Built-In Unity Functions ---------------------------------------------------- */

    void Start() {
        /*
         * Assign a unique rendering layers to each new portalSet 
         * and properly name the children of this protalSet.
         */

        /* Create the availableLayers array if it is not yet initilized or it's sizes dont match */
        /*if(availableLayers == null || availableLayers.Length != maxLayer - minLayer) {
            availableLayers = new bool[maxLayer-minLayer];
            for(int i = 0; i < availableLayers.Length; i++) {
                availableLayers[i] = false;
            }
        }*/

        /* Get the first available layer not currently used by another portalSet*/
        /*if(currentLayer == -1) {
            for(int i = 0; i < availableLayers.Length; i++) {
                if(availableLayers[i] == false) {
                    currentLayer = i;
                    i = availableLayers.Length;
                }
            }
        }else {
            Debug.Log("PortalSet already assigned a layer");
        }*/

        /* Reserve the layer and assign it to the proper children of this portalSet */
        /*if(currentLayer == -1) {
            Debug.Log("WARNIGNG: CANNOT FIND RENDERING LAYER FOR THIS PORTALSET");
        }
        else if(availableLayers[currentLayer]) {
            Debug.Log("WARNIGNG: TRYING TO USE AN OCCUPIED LAYER");
        }
        else {
            availableLayers[currentLayer] = true;
            AssignLayerToChildren(currentLayer+minLayer);
            Debug.Log("NOW CONTROLLING LAYER " + (currentLayer+minLayer));
        }*/

       
        /* Properly name this portalSet */
        string ID = "" + GetInstanceID();
        if(objectName != "") {
            name = objectName + " (PortalID = " + ID + ")";
        }else {
            name = "PortalSet (PortalID = " + ID + ")";
        }

        /* Name the portalSet's portalMeshes using this portal's ID */
        EntrancePortal.portalMesh.name = GetInstanceID() + "|Entrance Mesh";
        EntrancePortal.backwardsPortalMesh.name = GetInstanceID() + "|Entrance Backwards Mesh";
        ExitPortal.portalMesh.name = GetInstanceID() + "|Exit Mesh";
        ExitPortal.backwardsPortalMesh.name = GetInstanceID() + "|Exit Backwards Mesh";

        /* Give this portalSet's ID to each portalMesh */
        EntrancePortal.portalMesh.GetComponent<PortalView>().portalSetID = ID;
        EntrancePortal.backwardsPortalMesh.GetComponent<PortalView>().portalSetID = ID;
        ExitPortal.portalMesh.GetComponent<PortalView>().portalSetID = ID;
        ExitPortal.backwardsPortalMesh.GetComponent<PortalView>().portalSetID = ID;
    }

    void OnDisable() {
        /*
         * Remove the currentLayer from the active layer list
         */

        /* Relenquish control over it's current layer */
        /*if(currentLayer != -1) {
            availableLayers[currentLayer] = false;
            currentLayer = -1;
        }*/
    }

    void Update() {

        /* If there is a change in the portal's size, force it to update */
        if(portalWidth != previousWidth || portalHeight != previousHeight) {
            updatePortal = true;
        }

        /* Reinitilize the portal's pieces */
        if(updatePortal) {
            CreateMesh();
            CreateBorder();
            UpdatePortalTransforms();
            UpdateTriggers();

            updatePortal = false;
        }
    }
    

    /* -------- Update Functions ---------------------------------------------------- */

    void UpdatePortalTransforms() {
        /*
         * Depending on the sizes of the portals, change the local position and rotation of the portals.
         * This includes the portal meshes, triggers, borders, and backwards portals.
         * Since the two portals are "mirrors", the exitPortal will need to be reversed.
         */

        /* Add an offset if the portals need to be centered onto their origin */
        Vector3 centeredOffset = Vector3.zero;
        if(portalsCentered) {
            centeredOffset = new Vector3(portalWidth/2f, 0, 0);
        }

        /* Set the mesh, trigger and border containers to their proper positions */
        EntrancePortal.TriggerContainer.localPosition = centeredOffset;
        EntrancePortal.borderContainer.localPosition = new Vector3(-portalWidth/2f, 0, 0) + centeredOffset;
        ExitPortal.TriggerContainer.localPosition = centeredOffset;
        ExitPortal.borderContainer.localPosition = new Vector3(-portalWidth/2f, 0, 0) + centeredOffset;
        EntrancePortal.meshContainer.localPosition = centeredOffset;
        EntrancePortal.meshContainer.localEulerAngles = new Vector3(0, 0, 0);
        ExitPortal.meshContainer.localPosition = centeredOffset;
        ExitPortal.meshContainer.localEulerAngles = new Vector3(0, 180, 0);

        /* Properly set the portals and their exit points */
        EntrancePortal.SetPortalTransforms();
        ExitPortal.SetPortalTransforms();
    }

    void UpdateTriggers() {
        /*
         * Set the position, rotation and scale of the triggers along with their offset.
         */
         
        EntrancePortal.SetTriggersTransform(portalWidth, portalHeight, triggerThickness, triggerOffset);
        ExitPortal.SetTriggersTransform(portalWidth, portalHeight, triggerThickness, triggerOffset);
    }


    /* -------- Initilization Functions ---------------------------------------------------- */
    
    void CreateMesh() {
        /*
         * Create the mesh for the linked portals using the given portalMesh. If there is no portalMesh,
         * use a generated mesh with the given defaultMeshSize values.
         */
        Mesh normalMesh, offsetMesh;

        /* Get either the linked mesh for the portal or create the default mesh */
        if(portalMesh) {
            //Note: This has not yet been tested and will most likely need revision
            normalMesh = portalMesh;
            offsetMesh = portalMesh;
        }
        else {
            normalMesh = CreateDefaultMesh(false);
            offsetMesh = CreateDefaultMesh(true);
        }

        /* Assign the mesh to each linked portalObject */
        EntrancePortal.SetMesh(normalMesh, offsetMesh);
        ExitPortal.SetMesh(offsetMesh, normalMesh);
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
            DestroyImmediate(newBorders);
        }
    }
    

    /* -------- Event Functions ---------------------------------------------------- */
    
    void AssignLayerToChildren(int renderLayer) {
        /*
         * Assign each portalMesh of this portalSet the given rendering layer and 
         * for each camera in this portalSet, remove the given rendering layer from it's culling mask
         */

        /* Assign the portalMeshes to their proper layer */
        /*EntrancePortal.portalMesh.layer = renderLayer;
        EntrancePortal.backwardsPortalMesh.layer = renderLayer;
        ExitPortal.portalMesh.layer = renderLayer;
        ExitPortal.backwardsPortalMesh.layer = renderLayer;*/

        /* have the children cameras render all but the given rendering layer */
        /*EntrancePortal.portalMesh.GetComponent<PortalView>().scoutCamera.cullingMask = ~(1 << renderLayer);
        EntrancePortal.backwardsPortalMesh.GetComponent<PortalView>().scoutCamera.cullingMask = ~(1 << renderLayer);
        ExitPortal.portalMesh.GetComponent<PortalView>().scoutCamera.cullingMask = ~(1 << renderLayer);
        ExitPortal.backwardsPortalMesh.GetComponent<PortalView>().scoutCamera.cullingMask = ~(1 << renderLayer);*/
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

    Mesh CreateDefaultMesh(bool reflectMesh) {
        /*
         * Create the default mesh for the portal using the default width and height.
         * 
         * When handling the exit portal's mesh, add an offset to the mesh to properly "reflect" the portal.
         */
        Mesh defaultMesh = new Mesh();
        Vector3[] vertices;
        int[] triangles;

        /* If it's the reflected mesh, apply another offset */
        Vector3 reflectionOffset = Vector3.zero;
        if(reflectMesh) {
            reflectionOffset = new Vector3(portalWidth, 0, 0);
        }

        /* Set the vertices for the mesh */
        vertices = new Vector3[] {
                new Vector3(-portalWidth, portalHeight, 0) + portalOffset + reflectionOffset,
                new Vector3(0, portalHeight, 0) + portalOffset + reflectionOffset,
                new Vector3(0, 0, 0) + portalOffset + reflectionOffset,
                new Vector3(-portalWidth, 0, 0) + portalOffset + reflectionOffset
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
            centerPoint = new Vector3(portalWidth/2f + defaultBorderRight, portalHeight/2f, 0);
            borderPiece = CreateBox(centerPoint, defaultBorderRight, portalHeight/2f, defaultBorderDepth);
            borderPiece.name = "Right side";
            borderPiece.transform.parent = newBorders.transform;
        }

        /* Create the left side of the border piece */
        if(defaultBorderLeft > 0) {
            centerPoint = new Vector3(-portalWidth/2f - defaultBorderLeft, portalHeight/2f, 0);
            borderPiece = CreateBox(centerPoint, defaultBorderLeft, portalHeight/2f, defaultBorderDepth);
            borderPiece.name = "Left side";
            borderPiece.transform.parent = newBorders.transform;
        }

        /* Create the top side of the border piece */
        if(defaultBorderTop > 0) {
            centerPoint = new Vector3(0, portalHeight + defaultBorderTop, 0);
            borderPiece = CreateBox(centerPoint, portalWidth/2f, defaultBorderTop, defaultBorderDepth);
            borderPiece.name = "Top side";
            borderPiece.transform.parent = newBorders.transform;
        }

        /* Create the bottom side of the border piece */
        if(defaultBorderBottom > 0) {
            centerPoint = new Vector3(0, -defaultBorderBottom, 0);
            borderPiece = CreateBox(centerPoint, portalWidth/2f, defaultBorderBottom, defaultBorderDepth);
            borderPiece.name = "Bottom side";
            borderPiece.transform.parent = newBorders.transform;
        }


        /* Create the corners of the border pieces of the two sides that connect them are used */
        if(defaultBorderRight > 0) {
            if(defaultBorderTop > 0) {
                centerPoint = new Vector3(portalWidth/2f + defaultBorderRight, portalHeight + defaultBorderTop, 0);
                borderPiece = CreateBox(centerPoint, defaultBorderRight, defaultBorderTop, defaultBorderDepth);
                borderPiece.name = "RT Corner";
                borderPiece.transform.parent = newBorders.transform;
            }

            if(defaultBorderBottom > 0) {
                centerPoint = new Vector3(portalWidth/2f + defaultBorderRight, -defaultBorderBottom, 0);
                borderPiece = CreateBox(centerPoint, defaultBorderRight, defaultBorderBottom, defaultBorderDepth);
                borderPiece.name = "RB Corner";
                borderPiece.transform.parent = newBorders.transform;
            }
        }

        if(defaultBorderLeft > 0) {
            if(defaultBorderTop > 0) {
                centerPoint = new Vector3(-portalWidth/2f - defaultBorderLeft, portalHeight + defaultBorderTop, 0);
                borderPiece = CreateBox(centerPoint, defaultBorderLeft, defaultBorderTop, defaultBorderDepth);
                borderPiece.name = "LT Corner";
                borderPiece.transform.parent = newBorders.transform;
            }

            if(defaultBorderBottom > 0) {
                centerPoint = new Vector3(-portalWidth/2f - defaultBorderLeft, -defaultBorderBottom, 0);
                borderPiece = CreateBox(centerPoint, defaultBorderLeft, defaultBorderBottom, defaultBorderDepth);
                borderPiece.name = "LB Corner";
                borderPiece.transform.parent = newBorders.transform;
            }
        }


        return newBorders;
    }
}