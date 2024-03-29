﻿using UnityEngine;
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
    
    /* The materials and textures used by the window frame */
    public Material frameMaterial;
    public Material glassMaterial;
    public Material crackedGlassMaterial;

    /* The UV scale if applicable */
    public Vector2 UVScale;

    /* The base object used for the glass. It contains a particleSystem that will be copied. */
    public GameObject glassObjectReference;


    /* -------- Update Functions ---------------------------------------------------- */

    public void UpdateWindow(GameObject glassObjectRef) {
        /*
         * Create the window mesh and place the portal relative to the window. This is called by an 
         * outside function after it sets the desired values for this window.
         */
        glassObjectReference = glassObjectRef;

        /* Place the portal */
        UpdatePortalStats();

        /* Create the window */
        UpdateWindowMesh();
    }
    public void UpdateWindow() {
        UpdateWindow(null);
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
        
        /* Ensure the window's portal's thickness is above 0 */
        portalSet.triggerThickness = 0.01f;

        /* Properly position the windows in the world */
        UpdateWindowPosition();

        /* Update the portal's meshCollider with these new values */
        portalSet.updatePortal = true;
    }

    public void UpdateWindowMesh() {
        /*
         * Update the meshes that form the window along with the containers they are within
         */

        /* Create the window containers if they have not yet been created */
        if(insideWindowContainer == null) { CreateEmptyObject(ref insideWindowContainer, "Inside Window", transform); }
        if(outsideWindowContainer == null) { CreateEmptyObject(ref outsideWindowContainer, "Outside Window", transform); }
        
        /* Initialize the array of GameObjects that make up the windows */
        int index = 0;
        CreateObjectsArray(ref windowPieces, 10, Vector3.zero);

        /* Create the main 4 frame pieces for each window frame */
        CreateFrame(insideWindowContainer.transform, ref index, windowHeight, windowWidth);
        CreateFrame(outsideWindowContainer.transform, ref index, windowHeight, windowWidth);
    }

    public void UpdateWindowPosition() {
        /*
         * Update the position of the windows in the world
         */

        /* Place and rotate the inside and outside windows */
        insideWindowContainer.transform.position = insidePos;
        insideWindowContainer.transform.eulerAngles = insideRot;
        outsideWindowContainer.transform.position = outsidePos;
        outsideWindowContainer.transform.eulerAngles = outsideRot;

        /* To prevent the portals from being placed behind the room's walls, control it's offset */
        portalSet.portalOffset = new Vector3(0, 0, -0.01f);
        Vector3 portalOffset = new Vector3(0, 0, -frameDepth/3f);
        /* Place the portals at their proper locations */
        portalSet.EntrancePortal.transform.position = insidePos;
        portalSet.EntrancePortal.transform.eulerAngles = insideRot;
        portalSet.EntrancePortal.transform.position += portalSet.EntrancePortal.transform.rotation*portalOffset;
        portalSet.ExitPortal.transform.position = outsidePos;
        portalSet.ExitPortal.transform.eulerAngles = outsideRot;
        portalSet.ExitPortal.transform.position -= portalSet.ExitPortal.transform.rotation*portalOffset;
    }


    /* -------- Event Functions ---------------------------------------------------- */

    public void AddFlareLayer() {
        /*
         * Add the flare layer to the portal's cameras
         */

        portalSet.AddFlareLayer();
    }

    public void SetWindowState(bool state) {
        /*
         * Set the state of the window's portal to either be visible or invisible.
         * This is to prevent the player from rendering the portals out of view.
         */

        portalSet.gameObject.SetActive(true);
        portalSet.UpdatePortalState(state);
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

        CreateEmptyObject(ref windowPieces[index], "Glass", windowParent, glassObjectReference);
        windowPieces[index].transform.localPosition = new Vector3(0, windowHeight/2f, 0);
        cubeScript = windowPieces[index].AddComponent<CubeCreator>();
        cubeScript.x = windowWidth;
        cubeScript.y = windowHeight;
        cubeScript.z = frameDepth*0.95f;
        cubeScript.mainMaterial = glassMaterial;
        /* Set the scale and offset of the window */
        cubeScript.UVScale = new Vector2(1f/windowWidth, 1f/windowHeight);
        cubeScript.ZNegativeOffset = new Vector2(0.5f, 0.5f);
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

    public void CreateEmptyObject(ref GameObject gameObject, string name, Transform parent, GameObject baseObject) {
        /*
         * Create an empty object, resetting their local position.
         * If the baseObject reference is not null, use it as a base object.
         */

        if(baseObject != null) {
            gameObject = Instantiate(baseObject);
        }
        else {
            gameObject = new GameObject();
        }

        gameObject.name = name;
        gameObject.transform.parent = parent;
        gameObject.transform.localPosition = new Vector3(0, 0, 0);
        gameObject.transform.localEulerAngles = new Vector3(0, 0, 0);
        gameObject.transform.localScale = new Vector3(1, 1, 1);
    }

    public void CreateEmptyObject(ref GameObject gameObject, string name, Transform parent) {
        CreateEmptyObject(ref gameObject, name, parent, null);
    }
}
