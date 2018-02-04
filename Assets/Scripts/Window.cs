using UnityEngine;
using System.Collections;

/*
 * A window is an object that uses a set of portals to connect two areas. This will be used
 * in certain rooms to let the player view a new area. A window will also have meshes around
 * it to simulate an actual window, along with a pane of glass to stop the player from passing through.
 * 
 * There are two windows that get created, both using a portal of the set: an inside and an outside.
 * The inside window expects the player to be on it's side and will be positionned in a room.
 * The outside window can be anywhere and does not expect the player to be on it's side
 */
[ExecuteInEditMode]
public class Window : MonoBehaviour {
    
    /* The portalSet that the window will be using */
    public PortalSet portalSet;

    /* The windows' position and angle */
    public Vector3 insidePos;
    public Vector3 insideRot;
    public Vector3 outsidePos;
    public Vector3 outsideRot;

    /* The main container that will contain the window's meshes */
    public GameObject insideWindowContainer;
    public GameObject outsideWindowContainer;

    /* The gameObjects that will be used to make up the windows */
    public GameObject[] windowPieces;

    /* The sizes of the window frame and portal */
    public float frameThickness;
    public float frameDepth;
    public float windowHeight;
    public float windowWidth;

    /* The GameObject object used as the skysphere for the outside window */
    public GameObject skySphere;

    /* The materials and textures used by the window frame */
    public Material frameMaterial;
    public Material glassMaterial;
    public Texture skySphereTexture;
    private Material skySphereMaterial;


    /* -------- Update Functions ---------------------------------------------------- */

    public void UpdateWindow() {
        /*
         * Create the window mesh and place the portal relative to the window. This is called by an 
         * outside function after it sets the desired values for this window.
         */

        /* Create the required materials */
        UpdateMaterials();

        /* Place the portal */
        UpdatePortalStats();

        /* Create the window */
        UpdateWindowMesh();

        /* Create the skysphere for the outside window */
        UpdateSkySphere();
    }

    void UpdateMaterials() {
        /*
         * Apply any required changes to the materials before creating the window
         */
         
        /* Adjust the glass material's scale to reflect the window's size */
        glassMaterial.SetTextureOffset("_MainTex", new Vector2(0.5f, 0.5f));
        glassMaterial.SetTextureScale("_MainTex", new Vector2(1f/windowWidth, 1f/windowHeight));

        /* Create the sky sphere material */
        skySphereMaterial = new Material(Shader.Find("Unlit/Texture"));
        skySphereMaterial.SetTexture("_MainTex", skySphereTexture);
    }

    public void UpdatePortalStats() {
        /*
         * Update the stats of the portalSet given this window's stats
         */

        /* Set the portal's sizes given this window's sizes */
        portalSet.portalWidth = windowWidth;
        portalSet.portalHeight = windowHeight;

        /* Force the portal to be centered */
        portalSet.portalsCentered = true;

        /* To prevent the portals from being placed behind the room's walls, control it's offset */
        portalSet.portalOffset = new Vector3(0, 0, -0.01f);
        Vector3 portalOffset = new Vector3(0, 0, -0.025f);

        /* Place the portals at their proper locations */
        portalSet.EntrancePortal.transform.position = insidePos;
        portalSet.EntrancePortal.transform.eulerAngles = insideRot;
        portalSet.EntrancePortal.transform.position += portalSet.EntrancePortal.transform.rotation*portalOffset;
        portalSet.ExitPortal.transform.position = outsidePos;
        portalSet.ExitPortal.transform.eulerAngles = outsideRot;
        portalSet.ExitPortal.transform.position -= portalSet.ExitPortal.transform.rotation*portalOffset;

        /* Update the portal's meshCollider with these new values */
        portalSet.updatePortal = true;
        portalSet.Update();
    }

    public void UpdateWindowMesh() {
        /*
         * Update the meshes that form the window along with the containers they are within
         */

        /* Create the window containers if they have not yet been created */
        if(insideWindowContainer == null) { CreateEmptyObject(ref insideWindowContainer, "Inside Window", transform); }
        if(outsideWindowContainer == null) { CreateEmptyObject(ref outsideWindowContainer, "Outside Window", transform); }

        /* Place and rotate the inside and outside windows */
        insideWindowContainer.transform.position = insidePos;
        insideWindowContainer.transform.eulerAngles = insideRot;
        outsideWindowContainer.transform.position = outsidePos;
        outsideWindowContainer.transform.eulerAngles = outsideRot;

        /* Initialize the array of GameObjects that make up the windows */
        int index = 0;
        CreateObjectsArray(ref windowPieces, 10, Vector3.zero);

        /* Create the main 4 frame pieces for each window frame */
        CreateFrame(insideWindowContainer.transform, ref index, windowHeight, windowWidth);
        CreateFrame(outsideWindowContainer.transform, ref index, windowHeight, windowWidth);
    }

    public void UpdateSkySphere() {
        /*
         * Create a sky sphere to place around the outside window to simulate a new environment
         */

        /* Create a sphere primitive */
        if(skySphere != null) { DestroyImmediate(skySphere); }
        skySphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        skySphere.transform.parent = outsideWindowContainer.transform;
        skySphere.transform.localPosition = new Vector3(0, 0, 0);
        skySphere.transform.localEulerAngles = new Vector3(0, 0, 0);
        skySphere.transform.localScale = new Vector3(100, 100, 100);
        skySphere.name = "Sky sphere";

        /* Adjust the components */
        DestroyImmediate(skySphere.GetComponent<SphereCollider>());

        /* Flip all the triangles of the sphere to have it inside-out if needed */
        int[] triangles = skySphere.GetComponent<MeshFilter>().sharedMesh.triangles;
        if(triangles[0] == 0) {
            int tempInt;
            for(int i = 0; i < triangles.Length; i += 3) {
                tempInt = triangles[i + 0];
                triangles[i + 0] = triangles[i + 2];
                triangles[i + 2] = tempInt;
            }
            skySphere.GetComponent<MeshFilter>().sharedMesh.triangles = triangles;
        }


        /* Apply the sky sphere material */
        skySphere.GetComponent<MeshRenderer>().sharedMaterial = skySphereMaterial;
    }


    /* -------- Event Functions ---------------------------------------------------- */

    public void OffsetSkySphere(Vector3 offset) {
        /*
         * Apply an offset to the skySphere of the outside window. This is called by the WaitingRoom
         * to ensure the sky sphere does not seem like a small sphere but a proper large environment.
         */

        /* Get the difference in the angles of both portals */
        Quaternion portalRotDiff =Quaternion.Inverse(portalSet.EntrancePortal.transform.rotation)*portalSet.ExitPortal.transform.rotation;
        
        /* Reposition the sky sphere with the offset */
        skySphere.transform.localPosition = new Vector3(0, 0, 0);

        /* Rotate the material with the same rotation of the outside window */
        skySphere.transform.rotation = portalSet.ExitPortal.transform.rotation;
        
        /* Make sure the window's exit is facing the center of the material/texture */
        skySphere.transform.rotation *= Quaternion.Euler(new Vector3(0, -90, 0));

        /* Apply the offset relative to the sphere's rotation */
        skySphere.transform.localPosition -= skySphere.transform.localRotation*offset;
    }


    /* -------- Helper Functions ---------------------------------------------------- */

    void CreateFrame(Transform windowParent, ref int index, float windowHeight, float windowWidth) {
        /*
         * Create the 4 main boxes that form the frame of a window and the pane of glass in the center.
         */
        CubeCreator cubeScript = null;

        CreateEmptyObject(ref windowPieces[index], "Top frame", windowParent);
        windowPieces[index].transform.localPosition = new Vector3(0, windowHeight + frameThickness/2f, 0);
        cubeScript = windowPieces[index].AddComponent<CubeCreator>();
        cubeScript.x = windowWidth + frameThickness*2;
        cubeScript.y = frameThickness;
        cubeScript.z = frameDepth;
        cubeScript.mainMaterial = frameMaterial;
        cubeScript.updateCube = true;
        index++;

        CreateEmptyObject(ref windowPieces[index], "Bottom frame", windowParent);
        windowPieces[index].transform.localPosition = new Vector3(0, -frameThickness/2f, 0);
        cubeScript = windowPieces[index].AddComponent<CubeCreator>();
        cubeScript.x = windowWidth + frameThickness*2;
        cubeScript.y = frameThickness;
        cubeScript.z = frameDepth;
        cubeScript.mainMaterial = frameMaterial;
        cubeScript.updateCube = true;
        index++;

        CreateEmptyObject(ref windowPieces[index], "Left frame", windowParent);
        windowPieces[index].transform.localPosition = new Vector3(-windowWidth/2f - frameThickness/2f, windowHeight/2f, 0);
        cubeScript = windowPieces[index].AddComponent<CubeCreator>();
        cubeScript.x = frameThickness;
        cubeScript.y = windowHeight;
        cubeScript.z = frameDepth;
        cubeScript.mainMaterial = frameMaterial;
        cubeScript.updateCube = true;
        index++;

        CreateEmptyObject(ref windowPieces[index], "Right frame", windowParent);
        windowPieces[index].transform.localPosition = new Vector3(windowWidth/2f + frameThickness/2f, windowHeight/2f, 0);
        cubeScript = windowPieces[index].AddComponent<CubeCreator>();
        cubeScript.x = frameThickness;
        cubeScript.y = windowHeight;
        cubeScript.z = frameDepth;
        cubeScript.mainMaterial = frameMaterial;
        cubeScript.updateCube = true;
        index++;

        CreateEmptyObject(ref windowPieces[index], "Glass", windowParent);
        windowPieces[index].transform.localPosition = new Vector3(0, windowHeight/2f, 0);
        cubeScript = windowPieces[index].AddComponent<CubeCreator>();
        cubeScript.x = windowWidth;
        cubeScript.y = windowHeight;
        cubeScript.z = frameDepth/1.5f;
        cubeScript.mainMaterial = glassMaterial;
        cubeScript.updateCube = true;
        index++;
    }

    public void CreateObjectsArray(ref GameObject[] objects, int size, Vector3 position) {
        /*
         * Re-create the given array of gameObjects
         */

        /* Ensure each object is empty before creating now ones */
        for(int i = 0; i < objects.Length; i++) {
            if(objects[i] != null) {
                DestroyImmediate(objects[i]);
            }
        }

        /* Create a new array with the new given size if needed */
        if(objects.Length != size) { objects = new GameObject[size]; }
    }

    public void CreateEmptyObject(ref GameObject gameObject, string name, Transform parent) {
        /*
         * Create an empty object, resetting their local position.
         */

        gameObject = new GameObject();
        gameObject.name = name;
        gameObject.transform.parent = parent;
        gameObject.transform.localPosition = new Vector3(0, 0, 0);
        gameObject.transform.localEulerAngles = new Vector3(0, 0, 0);
        gameObject.transform.localScale = new Vector3(1, 1, 1);
    }
}
