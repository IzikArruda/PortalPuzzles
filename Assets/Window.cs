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
public class Window : MonoBehaviour {
    
    /* The portalSet that the window will be using */
    public PortalSet portalSet;

    /* The window's position */
    public Vector3 insidePosition;
    public Vector3 outsidePosition;

    /* The main container that will contain the window's meshes */
    public Transform WindowContainer;

    /* The gameObjects that will be used to make up the windows */
    public GameObject[] windowPieces;


    /* -------- Built-In Functions ---------------------------------------------------- */
    
    void Start () {
        /*
         * For now, create the walls on startup
         */

        UpdateWindow();
	}


    /* -------- Update Functions ---------------------------------------------------- */

    void UpdateWindow() {
        /*
         * Create the window mesh and place the portal relative to the window
         */

        /* Place the portals at their proper locations */
        portalSet.EntrancePortal.transform.position = insidePosition;
        portalSet.ExitPortal.transform.position = outsidePosition;

        /* Place a box where the two windows will be */
        CreateObjects(ref windowPieces, 2, Vector3.zero);

        windowPieces[0].name = "inside window";
        windowPieces[0].AddComponent<BoxCollider>();
        windowPieces[0].transform.position = insidePosition;

        windowPieces[1].name = "outside window";
        windowPieces[1].AddComponent<BoxCollider>();
        windowPieces[1].transform.position = outsidePosition;
    }


    /* -------- Event Functions ---------------------------------------------------- */



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
