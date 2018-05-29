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

    /* positional offset of the portal's mesh without changing any other positions */
    public Vector3 portalOffset;
    
    /* The sizes of the portalMesh and the triggers */
    public float portalWidth;
    public float portalHeight;
    private float previousWidth = 0;
    private float previousHeight = 0;

    /* If the portal is centered on it's origin point. Else it protrudes from the origin. */
    public bool portalsCentered;

    /* Positional offset of the triggers for the portals */
    public Vector3 triggerOffset;

    /* Depth of the portal's trigger. The higher value, the more noticable of a jump will occur on teleport. Recommend 0.01. */
    public float triggerThickness;
    
    /* The object to be used as a border for the portal mesh. If this is null, a default border will be created */
    public GameObject portalBorder;
    
    /* The sizes of the default border. 0 or less means the side will not be created. */
    public float defaultBorderDepth;
    public float defaultBorderLeft;
    public float defaultBorderRight;
    public float defaultBorderTop;
    public float defaultBorderBottom;
    
    /* Whether or not both sides of this portal will be active. Determines if rendering layers will be used */
    public bool doubleSided;

    /* If the portal is not doubleSided, then which side will be the active portals */
    public bool exteriorSide;
    
    /* A static array that tracks all currently used protalMesh layers. True indicates the layer is active */
    private static bool[] availableLayers;

    /* The range of layers that the portal can occupy. do not change these at run-time. */
    public static int minLayer = 9;
    public static int maxLayer = 18;
    
    /* The two current layers this portalSet occupies */
    private int occupiedLayer1 = -1;
    private int occupiedLayer2 = -1;

    /* When true, will update the given object portal's meshes, triggers and borders */
    public bool updatePortal;

    /* Whether the portal will render the terrain layer or not. By default, do not render it */
    private bool renderTerrain = false;


    /* -------- Built-In Unity Functions ---------------------------------------------------- */

    public void Start() {
        /*
         * Enture the proper portals are enabled and name the children of this portalSet.
         */

        /* Properly enable the correct portals depending on this portal's selected sides */
        if(doubleSided) {
            EntrancePortal.portalMesh.gameObject.SetActive(true);
            EntrancePortal.backwardsPortalMesh.gameObject.SetActive(true);
            ExitPortal.portalMesh.gameObject.SetActive(true);
            ExitPortal.backwardsPortalMesh.gameObject.SetActive(true);
        }
        else {
            EntrancePortal.portalMesh.gameObject.SetActive(exteriorSide);
            EntrancePortal.backwardsPortalMesh.gameObject.SetActive(!exteriorSide);
            ExitPortal.portalMesh.gameObject.SetActive(exteriorSide);
            ExitPortal.backwardsPortalMesh.gameObject.SetActive(!exteriorSide);
        }

        /* Name this portalSet */
        string ID = "" + GetInstanceID();
        if(objectName != "") {
            name = objectName + " (PortalID = " + ID + ")";
        }else {
            name = "PortalSet (PortalID = " + ID + ")";
        }

        /* For each portalMesh, properly name them and give their portalView script this portalSetID */
        EntrancePortal.portalMesh.name = GetInstanceID() + "|Entrance Mesh";
        EntrancePortal.backwardsPortalMesh.name = GetInstanceID() + "|Entrance Backwards Mesh";
        ExitPortal.portalMesh.name = GetInstanceID() + "|Exit Mesh";
        ExitPortal.backwardsPortalMesh.name = GetInstanceID() + "|Exit Backwards Mesh";

        EntrancePortal.backwardsPortalMesh.GetComponent<PortalView>().portalSetID = ID;
        EntrancePortal.portalMesh.GetComponent<PortalView>().portalSetID = ID;
        ExitPortal.portalMesh.GetComponent<PortalView>().portalSetID = ID;
        ExitPortal.backwardsPortalMesh.GetComponent<PortalView>().portalSetID = ID;
    }

    void OnEnable() {
        /*
         * Assign a unique rendering layer to each new portalSet once they are active.
         * This only occurs if this portalSet is double sided.
         */
         
        if(doubleSided) {

            /* Ensure the layersArray is properly created */
            UpdateLayersArray();

            /* Find any free rendering layers not yet used by double sided portals */
            GetAvailableLayers();

            /* Assign the renderingLayer to the portals and their cameras */
            AssignRenderingLayers();
        }
    }

    void OnDisable() {
        /*
         * Remove the currentLayer from the active layer list and reset the assigned layers.
         * This only occurs if this portalSet is double sided.
         */

        if(doubleSided) {
                
            /* Relenquish control over it's current layers */
            if(occupiedLayer1 != -1) {
                availableLayers[occupiedLayer1] = false;
                occupiedLayer1 = -1;
            }
            if(occupiedLayer2 != -1) {
                availableLayers[occupiedLayer2] = false;
                occupiedLayer2 = -1;
            }

            /* Remove the layering of this portalSet's portals and cameras */
            RemoveRenderingLayers();
        }
    }

    public void Update() {
        /*
         * If any changes to the portal's size occurs, force the portal to update
         */

        /* If there is a change in the portal's size, force it to update */
        if(portalWidth != previousWidth || portalHeight != previousHeight) {
            updatePortal = true;
        }

        /* Reinitilize the portal's pieces */
        if(updatePortal) {
            UpdatePortal();
        }
    }
    

    /* -------- Update Functions ---------------------------------------------------- */

    public void UpdatePortal() {
        /*
         * Update the portal to reflect any size changes
         */
         
        CreateMesh();
        CreateBorder();
        UpdatePortalObjectsTransforms();
        
        /* Update the saved previous sizes */
        previousWidth = portalWidth;
        previousHeight = portalHeight;
        updatePortal = false;
    }

    void UpdatePortalObjectsTransforms() {
        /*
         * Update the transforms of the two portal's meshes, triggers and borders.
         */

        /* Add an offset if the portals need to be centered onto their origin */
        Vector3 centeredOffset = Vector3.zero;
        if(portalsCentered) {
            centeredOffset = new Vector3(portalWidth/2f, 0, 0);
        }

        /* Set the mesh, trigger and border containers to their proper positions */
        EntrancePortal.SetContainersTransforms(centeredOffset, portalWidth);
        ExitPortal.SetContainersTransforms(centeredOffset, portalWidth);

        /* Set the portalMeshes of both the portalObjects, indicating their entrance/exit status */
        EntrancePortal.SetPortalTransforms(portalWidth, true);
        ExitPortal.SetPortalTransforms(portalWidth, false);

        /* Set the sizes of the triggers for each portal */
        EntrancePortal.SetTriggers(portalWidth, portalHeight, triggerThickness, triggerOffset);
        ExitPortal.SetTriggers(portalWidth, portalHeight, triggerThickness, triggerOffset);
    }
    

    /* -------- Initilization Functions ---------------------------------------------------- */
    
    void CreateMesh() {
        /*
         * Create a generated portal mesh and assign it to the linked portals of this portalSet.
         */
        Mesh normalMesh = CreateDefaultMesh();

        /* Assign the mesh to each linked portalObject */
        EntrancePortal.SetMesh(normalMesh);
        ExitPortal.SetMesh(normalMesh);
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

    public void UpdateLayersArray() {
        /*
         * Create the availibleLayers array if it is not yet initilized
         * or recreate it if it's sizes have been updated.
         */

        /* Create the availableLayers array if it is not yet initilized or it's sizes dont match */
        if(availableLayers == null || availableLayers.Length != maxLayer - minLayer) {
            availableLayers = new bool[maxLayer-minLayer];
            for(int i = 0; i < availableLayers.Length; i++) {
                availableLayers[i] = false;
            }
        }
    }


    /* -------- Event Functions ---------------------------------------------------- */
    
    void AssignRenderingLayers() {
        /*
         * Assign the occupiedLayers to this portalSets portals and cameras. 
         * Each PortalObjects that are a part of a double sided portalSet
         * will use a sepperate rendering layer.
         */

        /* Assign the portalMeshes to their proper layer */
        EntrancePortal.portalMesh.layer = minLayer + occupiedLayer1;
        EntrancePortal.backwardsPortalMesh.layer = minLayer + occupiedLayer2;
        ExitPortal.portalMesh.layer = minLayer + occupiedLayer2;
        ExitPortal.backwardsPortalMesh.layer = minLayer + occupiedLayer1;

        /* Assign the cameras their proper layer to ignore */
        EntrancePortal.portalMesh.GetComponent<PortalView>().AssignCameraLayer(minLayer + occupiedLayer1);
        EntrancePortal.backwardsPortalMesh.GetComponent<PortalView>().AssignCameraLayer(minLayer + occupiedLayer2);
        ExitPortal.portalMesh.GetComponent<PortalView>().AssignCameraLayer(minLayer + occupiedLayer2);
        ExitPortal.backwardsPortalMesh.GetComponent<PortalView>().AssignCameraLayer(minLayer + occupiedLayer1);
    }

    void RemoveRenderingLayers() {
        /*
         * Remove the rendering layer on this portalSet's portals and cameras
         */

        EntrancePortal.portalMesh.layer = 0;
        EntrancePortal.backwardsPortalMesh.layer = 0;
        ExitPortal.portalMesh.layer = 0;
        ExitPortal.backwardsPortalMesh.layer = 0;
        EntrancePortal.portalMesh.GetComponent<PortalView>().AssignCameraLayer(-1);
        EntrancePortal.backwardsPortalMesh.GetComponent<PortalView>().AssignCameraLayer(-1);
        ExitPortal.portalMesh.GetComponent<PortalView>().AssignCameraLayer(-1);
        ExitPortal.backwardsPortalMesh.GetComponent<PortalView>().AssignCameraLayer(-1);
    }

    public void GetAvailableLayers() {
        /*
         * Search the availableLayers array to find free rendering layers to be used
         * on this portalSet's portals.
         */

        for(int i = 0; i < availableLayers.Length && (occupiedLayer1 == -1 || occupiedLayer2 == -1); i++) {
            if(availableLayers[i] == false) {
                if(occupiedLayer1 == -1) {
                    occupiedLayer1 = i;
                    availableLayers[occupiedLayer1] = true;
                    //Debug.Log("NOW USING LAYER " + (occupiedLayer1+minLayer));
                }
                else if(occupiedLayer2 == -1) {
                    occupiedLayer2 = i;
                    availableLayers[occupiedLayer2] = true;
                    //Debug.Log("NOW USING LAYER " + (occupiedLayer2+minLayer));
                }
                else {
                    Debug.Log("WARNING: THIS SHOULD NOT BE RUN");
                }
            }
        }
    }

    public void AddFlareLayer() {
        /*
         * Add the flare layer to each camera used by the portals
         */

        EntrancePortal.portalMesh.GetComponent<PortalView>().AddFlareLayer();
        EntrancePortal.backwardsPortalMesh.GetComponent<PortalView>().AddFlareLayer();
        ExitPortal.portalMesh.GetComponent<PortalView>().AddFlareLayer();
        ExitPortal.backwardsPortalMesh.GetComponent<PortalView>().AddFlareLayer();

        /* Disable the exit portal's teleport trigger as it will block the lens flare of the camera */
        ExitPortal.TriggerContainer.GetChild(0).GetComponent<BoxCollider>().enabled = false;
    }

    public void UpdatePortalState(bool state) {
        /*
         * Update the MeshRenderer state of the portal
         */

        EntrancePortal.backwardsPortalMesh.GetComponent<MeshRenderer>().enabled = state;
        EntrancePortal.portalMesh.GetComponent<MeshRenderer>().enabled = state;
        ExitPortal.backwardsPortalMesh.GetComponent<MeshRenderer>().enabled = state;
        ExitPortal.portalMesh.GetComponent<MeshRenderer>().enabled = state;
    }


    /* -------- Helper Functions ---------------------------------------------------- */

    public void SetRenderTerrain(bool renderTer) {
        /*
         * Set whether this portal set should render the terrain layer or not
         */

        renderTerrain = renderTer;
    }

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

    Mesh CreateDefaultMesh() {
        /*
         * Create the default mesh for the portal using the default width and height.
         */
        Mesh defaultMesh = new Mesh();
        Vector3[] vertices;
        int[] triangles;

        /* Set the vertices for the mesh */
        vertices = new Vector3[] {
                new Vector3(-portalWidth, portalHeight, 0) + portalOffset,
                new Vector3(0, portalHeight, 0) + portalOffset,
                new Vector3(0, 0, 0) + portalOffset,
                new Vector3(-portalWidth, 0, 0) + portalOffset
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