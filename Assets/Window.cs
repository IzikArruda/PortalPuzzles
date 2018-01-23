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

    /* The materials used by the window frame */
    public Material frameMaterial;
    public Material glassMaterial;
    public Material skySphereMaterial;

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

    /* The sphere object used as the skysphere for the outside window */
    public GameObject skySphere;

    
    /* -------- Update Functions ---------------------------------------------------- */

    public void UpdateWindow() {
        /*
         * Create the window mesh and place the portal relative to the window. This is called by an 
         * outside function after it sets the desired values for this window.
         */

        /* Place the portal */
        UpdatePortalStats();

        /* Create the window */
        UpdateWindowMesh();

        /* Create the skysphere for the outside window */
        UpdateSkySphere();
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
        Vector3 portalOffset = portalSet.portalOffset*2;

        /* Place the portals at their proper locations */
        portalSet.EntrancePortal.transform.position = insidePos;
        portalSet.EntrancePortal.transform.eulerAngles = insideRot;
        portalSet.EntrancePortal.transform.position += portalSet.EntrancePortal.transform.rotation*portalOffset;
        portalSet.ExitPortal.transform.position = outsidePos;
        portalSet.ExitPortal.transform.eulerAngles = outsideRot;
        portalSet.ExitPortal.transform.localPosition -= portalSet.ExitPortal.transform.rotation*portalOffset;

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
        Destroy(skySphere.GetComponent<SphereCollider>());

        /* Flip all the triangles of the sphere to have it inside-out */
        int[] triangles = skySphere.GetComponent<MeshFilter>().mesh.triangles;
        int tempInt;
        for(int i = 0; i < triangles.Length; i += 3) {
            tempInt = triangles[i + 0];
            triangles[i + 0] = triangles[i + 2];
            triangles[i + 2] = tempInt;
        }
        skySphere.GetComponent<MeshFilter>().mesh.triangles = triangles;

        /* Apply the skySphere texture to the sphere */
        skySphere.GetComponent<MeshRenderer>().sharedMaterial = skySphereMaterial;
    }


    /* -------- Event Functions ---------------------------------------------------- */



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
        cubeScript.UpdateBox();
        windowPieces[index].GetComponent<MeshRenderer>().material = frameMaterial;
        index++;

        CreateEmptyObject(ref windowPieces[index], "Bottom frame", windowParent);
        windowPieces[index].transform.localPosition = new Vector3(0, -frameThickness/2f, 0);
        cubeScript = windowPieces[index].AddComponent<CubeCreator>();
        cubeScript.x = windowWidth + frameThickness*2;
        cubeScript.y = frameThickness;
        cubeScript.z = frameDepth;
        cubeScript.UpdateBox();
        cubeScript.UpdateBox();
        windowPieces[index].GetComponent<MeshRenderer>().material = frameMaterial;
        index++;

        CreateEmptyObject(ref windowPieces[index], "Left frame", windowParent);
        windowPieces[index].transform.localPosition = new Vector3(-windowWidth/2f - frameThickness/2f, windowHeight/2f, 0);
        cubeScript = windowPieces[index].AddComponent<CubeCreator>();
        cubeScript.x = frameThickness;
        cubeScript.y = windowHeight;
        cubeScript.z = frameDepth;
        cubeScript.UpdateBox();
        windowPieces[index].GetComponent<MeshRenderer>().material = frameMaterial;
        index++;

        CreateEmptyObject(ref windowPieces[index], "Right frame", windowParent);
        windowPieces[index].transform.localPosition = new Vector3(windowWidth/2f + frameThickness/2f, windowHeight/2f, 0);
        cubeScript = windowPieces[index].AddComponent<CubeCreator>();
        cubeScript.x = frameThickness;
        cubeScript.y = windowHeight;
        cubeScript.z = frameDepth;
        cubeScript.UpdateBox();
        windowPieces[index].GetComponent<MeshRenderer>().material = frameMaterial;
        index++;

        CreateEmptyObject(ref windowPieces[index], "Glass", windowParent);
        windowPieces[index].transform.localPosition = new Vector3(0, windowHeight/2f, 0);
        cubeScript = windowPieces[index].AddComponent<CubeCreator>();
        cubeScript.x = windowWidth;
        cubeScript.y = windowHeight;
        cubeScript.z = frameDepth/1.5f;
        cubeScript.UpdateBox();
        windowPieces[index].GetComponent<MeshRenderer>().material = glassMaterial;
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
        gameObject.transform.localScale = new Vector3(1f, 1, 1);
    }
}
