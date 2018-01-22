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

    /* The window's position and angle */
    public Vector3 insidePosition;
    public Vector3 insideEulerAngles;
    public Vector3 outsidePosition;
    public Vector3 outsideEulerAngles;

    /* The main container that will contain the window's meshes */
    public Transform WindowContainer;
    public Transform insideWindow;
    public Transform outsideWindow;

    /* The gameObjects that will be used to make up the windows */
    public GameObject[] windowPieces;

    /* The sizes of the window frame */
    public float frameThickness;
    public float frameDepth;

    /* The sizes of the portal */
    public float windowHeight;
    public float windowWidth;


    /* -------- Built-In Functions ---------------------------------------------------- */
    
    void Start () {

	}


    /* -------- Update Functions ---------------------------------------------------- */

    public void UpdateWindow() {
        /*
         * Create the window mesh and place the portal relative to the window
         */
        float windowHeight = portalSet.portalHeight;
        float windowWidth = portalSet.portalWidth;

        /* Place a box where the two windows will be */
        CreateObjects(ref windowPieces, 8, Vector3.zero);

        /* Place the portals at their proper locations */
        portalSet.ExitPortal.transform.position = outsidePosition;
        
        /* Place and rotate the inside and outside windows */
        insideWindow.position = insidePosition;
        insideWindow.eulerAngles = insideEulerAngles;
        outsideWindow.position = outsidePosition;
        outsideWindow.eulerAngles = outsideEulerAngles;

        /* To prevent the portals from being placed behind the room's walls, use an offset */
        portalSet.portalOffset = new Vector3(0, 0, -0.01f);
        Vector3 portalOffset = portalSet.portalOffset*2;


        portalSet.EntrancePortal.transform.position = insidePosition;
        portalSet.EntrancePortal.transform.eulerAngles = insideEulerAngles;
        portalSet.EntrancePortal.transform.position += portalSet.EntrancePortal.transform.rotation*portalOffset;
        portalSet.ExitPortal.transform.position = outsidePosition;
        portalSet.ExitPortal.transform.eulerAngles = outsideEulerAngles;
        portalSet.ExitPortal.transform.localPosition -= portalSet.ExitPortal.transform.rotation*portalOffset;




        /* Create the main 4 frame pieces for each window frame */
        CreateFrame(insideWindow, 0, windowHeight, windowWidth);
        CreateFrame(outsideWindow, 4, windowHeight, windowWidth);
    }


    /* -------- Event Functions ---------------------------------------------------- */

    void CreateFrame(Transform windowParent, int index, float windowHeight, float windowWidth) {
        /*
         * Create the 4 main boxes that form the frame of a window.
         */
        CubeCreator cubeScript = null;

        windowPieces[index].name = "Top frame";
        windowPieces[index].transform.parent = windowParent;
        windowPieces[index].transform.localPosition = new Vector3(0, windowHeight + frameThickness/2f, 0);
        windowPieces[index].transform.localEulerAngles = new Vector3(0, 0, 0);
        windowPieces[index].transform.localScale = new Vector3(1, 1, 1);
        cubeScript = windowPieces[index].AddComponent<CubeCreator>();
        cubeScript.x = windowWidth + frameThickness*2;
        cubeScript.y = frameThickness;
        cubeScript.z = frameDepth;
        index++;

        windowPieces[index].name = "Bottom frame";
        windowPieces[index].transform.parent = windowParent;
        windowPieces[index].transform.localPosition = new Vector3(0, -frameThickness/2f, 0);
        windowPieces[index].transform.localEulerAngles = new Vector3(0, 0, 0);
        windowPieces[index].transform.localScale = new Vector3(1, 1, 1);
        cubeScript = windowPieces[index].AddComponent<CubeCreator>();
        cubeScript.x = windowWidth + frameThickness*2;
        cubeScript.y = frameThickness;
        cubeScript.z = frameDepth;
        index++;

        windowPieces[index].name = "Left frame";
        windowPieces[index].transform.parent = windowParent;
        windowPieces[index].transform.localPosition = new Vector3(-windowWidth/2f - frameThickness/2f, windowHeight/2f, 0);
        windowPieces[index].transform.localEulerAngles = new Vector3(0, 0, 0);
        windowPieces[index].transform.localScale = new Vector3(1, 1, 1);
        cubeScript = windowPieces[index].AddComponent<CubeCreator>();
        cubeScript.x = frameThickness;
        cubeScript.y = windowHeight;
        cubeScript.z = frameDepth;
        index++;

        windowPieces[index].name = "Right frame";
        windowPieces[index].transform.parent = windowParent;
        windowPieces[index].transform.localPosition = new Vector3(windowWidth/2f + frameThickness/2f, windowHeight/2f, 0);
        windowPieces[index].transform.localEulerAngles = new Vector3(0, 0, 0);
        windowPieces[index].transform.localScale = new Vector3(1, 1, 1);
        cubeScript = windowPieces[index].AddComponent<CubeCreator>();
        cubeScript.x = frameThickness;
        cubeScript.y = windowHeight;
        cubeScript.z = frameDepth;
    }

    /* -------- Helper Functions ---------------------------------------------------- */

    public void CreateObjects(ref GameObject[] objects, int size, Vector3 position) {
        /*
         * Re-create the given array of gameObjects. Set only values that are idential for each objects.
         */

        /* Ensure each object is empty before creating now ones */
        for(int i = 0; i < objects.Length; i++) {
            if(objects[i] != null) {
                DestroyImmediate(objects[i]);
            }
        }

        /* Create a new array with the new given size if needed */
        if(objects.Length != size) { objects = new GameObject[size]; }

        /* Create each new objects */
        for(int i = 0; i < objects.Length; i++) {
            objects[i] = new GameObject();
            objects[i].transform.parent = WindowContainer;
            objects[i].transform.position = position;
            objects[i].transform.localEulerAngles = new Vector3(0, 0, 0);
            objects[i].transform.localScale = new Vector3(1, 1, 1);
        }
    }
}
