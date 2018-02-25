using UnityEngine;
using System.Collections;

/* 
 * A WaitingRoom is a ConnectedRoom that connects two AttachedRooms. It is not a puzzle room 
 * and serves to put more distance between each puzzle room. A waiting room
 * has the shape of a Z tetromino to ensure the player will not see more than 2 puzzle rooms at once.
 */
[ExecuteInEditMode]
public class WaitingRoom : ConnectedRoom {

    /* The two AttachedRooms that will be connected  */
    public AttachedRoom entranceRoom;
    public AttachedRoom exitRoom;

    /* The previous and upcomming WaitingRooms, if applicable */
    public WaitingRoom previousRoom;
    public WaitingRoom nextRoom;

    /* The window used in this WaitingRoom along with it's stats */
    public Transform windowContainer;
    public Window[] windows;
    public float frameThickness;
    public float frameDepth;
    [Range(0, 1)]
    public float windowHeightRatio;
    [Range(0, 1)]
    public float windowWidthRatio;
    private Vector3 playerEnterOffset;
    //Where the outside window will be placed. Use the "Window Exit" object in it's Points of Interest container
    public Transform windowExit;

    /* Values set by this room upon it's creation. Used as a reference. */
    private float xDist;
    private float yDist;
    private float zDist;
    private Vector3 roomCenter;
    
    /* The materials and textures used by this room */
    public Material windowFrameMaterial;
    public Material windowGlassMaterial;
    public Texture skySphereTexture;
    private Material skySphereMaterial;

    /* The GameObject object used as the skysphere for the outside window */
    public GameObject skySphere;

    /* -------- Built-In Functions ---------------------------------------------------- */

    void Awake() {
        /*
         * Ensure every room is disabled before the Start() functions start running to allow 
         * only the bare minimum required rooms being active once the player finishes loading.
         */

        /* do not disable the rooms if it is in the editor */
        if(!Application.isEditor) {
            entranceRoom.DisablePuzzleRoom();
            exitRoom.DisablePuzzleRoom();
            DisableRoom();
        }
    }

    void Start() {
        /*
         * On start-up, recreate the room's skeleton any puzzle rooms from the AttachedRooms.
         */

        /* Update the walls of the room */
        UpdateRoom();

        /* Place the window in a good position in the room */
        UpdateWindows();

        /* Update the sky sphere */
        UpdateSkySphere();

        /* Update the layers of each object */
        UpdateLayers();

    }

    void OnTriggerEnter(Collider player) {
        /*
         * When the player enters the room's trigger, enable both connected puzzle rooms
         * and their connected rooms
         */

        /* Ensure the collider entering the trigger is a player */
        if(player.GetComponent<CustomPlayerController>() != null) {
            entranceRoom.EnablePuzzleRoom();
            exitRoom.EnablePuzzleRoom();
            if(previousRoom != null) { previousRoom.SoftEnable(); }
            if(nextRoom != null) { nextRoom.SoftEnable(); }

            /* Set the playerEnterOffset */
            playerEnterOffset = player.GetComponent<CustomPlayerController>().playerCamera.transform.position;
        }
    }

    void OnTriggerExit(Collider player) {
        /*
         * When the player leaves the room's trigger, Check which side of the room
         * the player left to determine which puzzle room to disable
         */

        /* Ensure the collider entering the trigger is a player */
        if(player.GetComponent<CustomPlayerController>() != null) {
            Vector3 center = (entranceRoom.exitPointFront.position + exitRoom.exitPointBack.position)/2f;

            /* Player progressed forward */
            if(player.transform.position.z > center.z) {
                entranceRoom.DisablePuzzleRoom();
                exitRoom.EnablePuzzleRoom();
                if(nextRoom != null) { nextRoom.SoftEnable(); }
                if(previousRoom != null) { previousRoom.DisableRoom(); }
            }

            /* Player moved backwards through the puzzles */
            else {
                exitRoom.DisablePuzzleRoom();
                entranceRoom.EnablePuzzleRoom();
                if(previousRoom != null) { previousRoom.SoftEnable(); }
                if(nextRoom != null) { nextRoom.DisableRoom(); }
            }
        }
    }

    void OnTriggerStay(Collider player) {
        /*
         * Whenever the player is inside the waitingRoom, move the window's sky sphere
         * relative to the player's camera to this room's center.
         */
        Vector3 playerCameraPosition;
        Vector3 centerDifference;
         
        /* Ensure the collider entering the trigger is a player */
        if(player.GetComponent<CustomPlayerController>() != null) {
            playerCameraPosition = player.GetComponent<CustomPlayerController>().playerCamera.transform.position;
            centerDifference = playerCameraPosition - playerEnterOffset;
            OffsetSkySphere(centerDifference);
        }
    }

    /* -------- Event Functions ---------------------------------------------------- */

    void UpdateRoom() {
        /*
         * Given the position of the attached rooms, re-create this room's bounderies
         */
         
        /* Extract the needed values from the two AttachedRooms */
        float xEntranceDist = entranceRoom.exitWidth;
        float yEntranceDist = entranceRoom.exitHeight;
        float xExitDist = exitRoom.exitWidth;
        float yExitDist = exitRoom.exitHeight;

        /* Re-position the room to the center position between the two attachedRooms */
        roomCenter = (entranceRoom.exitPointFront.position + exitRoom.exitPointBack.position)/2f;
        roomCenter -= new Vector3((xEntranceDist/2f - xExitDist/2f)/2f, 0, 0);
        
        /* Set the sky spheres in a place to that will not be near other spheres or puzzle rooms */
        windowExit.eulerAngles = new Vector3(0, 0, 0);
        windowExit.position = roomCenter + new Vector3(0, 3000, roomCenter.z*10);
        
        /* Calculate the sizes of this waitingRoom */
        xDist = Mathf.Abs(entranceRoom.exitPointFront.position.x - exitRoom.exitPointBack.position.x) + xEntranceDist/2f + xExitDist/2f;
        yDist = Mathf.Max(yEntranceDist, yExitDist);
        zDist = Mathf.Abs(entranceRoom.exitPointFront.position.z - exitRoom.exitPointBack.position.z);

        /* Re-create the trigger that is used to determine if the player has entered either AttachedRooms */
        CreateTrigger();
        
        /* Re-create each wall for the room as a default, centered, empty object */
        CreateObjects(ref roomWalls, 8, roomCenter);

        /* Set unique values for each individual wall */
        roomWalls[0].name = "Floor";
        CreatePlane(roomWalls[0], xDist, zDist, 8, floorMaterial, 0, false);
        //Attach a DetectPlayerLegRay script to the floor
        roomWalls[0].AddComponent<DetectPlayerLegRay>();
        roomWalls[0].GetComponent<DetectPlayerLegRay>().objectType = 0;

        roomWalls[1].name = "Left wall";
        roomWalls[1].transform.position += new Vector3(-xDist/2f, yDist/2f, 0);
        CreatePlane(roomWalls[1], yDist, zDist, 8, wallMaterial, 1, true);

        roomWalls[2].name = "Right wall";
        roomWalls[2].transform.position += new Vector3(xDist/2f, yDist/2f, 0);
        CreatePlane(roomWalls[2], yDist, zDist, 8, wallMaterial, 1, false);

        roomWalls[3].name = "Ceiling";
        roomWalls[3].transform.position += new Vector3(0, yDist, 0);
        CreatePlane(roomWalls[3], xDist, zDist, 8, ceilingMaterial, 0, true);

        roomWalls[4].name = "Entrance side wall";
        roomWalls[4].transform.position += new Vector3(xEntranceDist/2f, yDist/2f, -zDist/2f);
        CreatePlane(roomWalls[4], xDist - xEntranceDist, yDist, 8, wallMaterial, 2, true);

        roomWalls[5].name = "Exit side wall";
        roomWalls[5].transform.position += new Vector3(-xExitDist/2f, yDist/2f, zDist/2f);
        CreatePlane(roomWalls[5], xDist - xExitDist, yDist, 8, wallMaterial, 2, false);

        roomWalls[6].name = "Above Entrance wall";
        roomWalls[6].transform.position += new Vector3(-xDist/2f + xEntranceDist/2f, yDist - (yDist - yEntranceDist)/2f, -zDist/2f);
        CreatePlane(roomWalls[6], xEntranceDist, yDist - yEntranceDist, 8, wallMaterial, 2, true);

        roomWalls[7].name = "Above Exit wall";
        roomWalls[7].transform.position += new Vector3(xDist/2f - xExitDist/2f, yDist - (yDist - yExitDist)/2f, zDist/2f);
        CreatePlane(roomWalls[7], xExitDist, yDist - yExitDist, 8, wallMaterial, 2, false);
    }

    void UpdateWindows() {
        /*
         * Update the values of the windows and position them in their position relative to this room.
         * Any adjustements to the window's inside transform will also be done to it's outside transform.
         */
        Vector3 ontoWallOffset;
        Vector3 ontoWallEuler;
        float windowHeight = windowHeightRatio*yDist;
        float wallWidth;

        /* Set the unified values of each window used */
        for(int i = 0; i < windows.Length; i++) {
            UpdateWindow(windows[i], frameThickness, frameDepth, windowHeight, windowFrameMaterial, windowGlassMaterial);
        }


        /* Place the inside window/entrance portal on the left wall, halfway up the wall */
        wallWidth = zDist;
        ontoWallOffset = new Vector3(-xDist/2f, yDist/2f - windowHeight/2f, 0);
        ontoWallEuler = new Vector3(0, -90, 0);
        UpdateWindowTransform(windows[0], ontoWallOffset, ontoWallEuler, windowWidthRatio*wallWidth);

        /* Place the next window on the right wall, halfway up again */
        wallWidth = zDist;
        ontoWallOffset = new Vector3(xDist/2f, yDist/2f - windowHeight/2f, 0);
        ontoWallEuler = new Vector3(0, 90, 0);
        UpdateWindowTransform(windows[1], ontoWallOffset, ontoWallEuler, windowWidthRatio*wallWidth);

        /* Place the next window on the front wall */
        wallWidth = xDist - exitRoom.exitWidth;
        ontoWallOffset = new Vector3(-xDist/2f + (xDist - exitRoom.exitWidth)/2f, yDist/2f - windowHeight/2f, zDist/2f);
        ontoWallEuler = new Vector3(0, 0, 0);
        UpdateWindowTransform(windows[2], ontoWallOffset, ontoWallEuler, windowWidthRatio*wallWidth);

        /* Place the next window on the back wall */
        wallWidth = xDist - entranceRoom.exitWidth;
        ontoWallOffset = new Vector3(xDist/2f - (xDist - entranceRoom.exitWidth)/2f, yDist/2f - windowHeight/2f, -zDist/2f);
        ontoWallEuler = new Vector3(0, 180, 0);
        UpdateWindowTransform(windows[3], ontoWallOffset, ontoWallEuler, windowWidthRatio*wallWidth);



        /* Send a command to update the windows with the new given parameters */
        for(int i = 0; i < windows.Length; i++) {
            windows[i].UpdateWindow();
        }
    }
    
    void UpdateSkySphere() {
        /*
         * Create a sky sphere to place around the outside window to simulate a new environment
         */

        /* Create a sphere primitive */
        if(skySphere != null) { DestroyImmediate(skySphere); }
        skySphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        skySphere.transform.parent = transform;
        OffsetSkySphere(new Vector3(0, 0, 0));
        skySphere.transform.localScale = new Vector3(250, 250, 250);
        skySphere.name = "Sky sphere";

        /* Rotate the material with the same rotation of the outside window */
        skySphere.transform.rotation = windowExit.rotation;
        skySphere.transform.rotation = Quaternion.Euler(new Vector3(0, 0, 0));
        
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
        skySphereMaterial = new Material(Shader.Find("Unlit/Texture"));
        skySphereMaterial.SetTexture("_MainTex", skySphereTexture);
        skySphere.GetComponent<MeshRenderer>().sharedMaterial = skySphereMaterial;
    }

    void UpdateWindow(Window window, float thickness, float depth, float height, 
            Material frameMaterial, Material glassMaterial) {
        /*
         * Update the values of the single window script given
         */

        window.frameThickness = thickness;
        window.frameDepth = depth;
        window.windowHeight = height;
        window.frameMaterial = frameMaterial;
        window.glassMaterial = glassMaterial;
    }

    void UpdateWindowTransform(Window window, Vector3 pos, Vector3 eul, float width) {
        /*
         * Update the given window's start and inside and outside transforms 
         */

        window.windowWidth = width;
        window.insidePos = roomCenter + pos;
        window.insideRot = eul;
        window.outsidePos = windowExit.position + pos;
        window.outsideRot = windowExit.eulerAngles + eul;
    }

    void UpdateLayers() {
        /*
         * Set the layers of the outside window frame and the window's cameras to only render the skySphere layer.
         */
         int layer = PortalSet.maxLayer+1;
        
        /* Set the layer of the windows so that the cameras only render the skySphere layer */
        for(int i  = 0; i < windows.Length; i++) {
            windows[i].SetWindowLayer(layer);
        }

        /* Set the layer of the skySphere */
        skySphere.gameObject.layer = layer;
    }
    

    /* -------- Event Functions ---------------------------------------------------- */
    
    public void OffsetSkySphere(Vector3 offset) {
        /*
         * Apply an offset to the skySphere of this room to ensure the sky sphere 
         * does not seem like a small sphere but a proper large environment.
         */
         
        /* Reposition the sky sphere at the given window exit point */
        skySphere.transform.position = windowExit.position;
        
        /* Apply the offset relative to the sphere's rotation */
        skySphere.transform.localPosition += skySphere.transform.localRotation*offset;
    }
    
    void CreateTrigger() {
        /*
         * Create the trigger that encompasses both this WaitingRoom and both the connected AttachedRooms
         */
        Vector3 backPoint = entranceRoom.exitPointBack.transform.position;
        Vector3 frontPoint = exitRoom.exitPointFront.transform.position;

        /* Get the proper width of the collider to encompass both AttachedRooms */
        float xFull = Mathf.Abs(frontPoint.x - backPoint.x) + entranceRoom.exitWidth/2f + exitRoom.exitWidth/2f;
        float zFull = Mathf.Abs(frontPoint.z - backPoint.z);
        
        /* Get the Z axis offset of the room center due to inequal exit/entrance z sizes */
        float zDiff = entranceRoom.roomLength - exitRoom.roomLength;

        /* Update the collider with it's new stats */
        RecreateMainTrigger();
        roomTrigger.center = roomCenter + new Vector3(0, yDist/2f, -zDiff);
        //Dont use the full Z distance to prevent the player from hitting it from the puzzleRoom
        roomTrigger.size = new Vector3(xFull, yDist, zFull*0.95f);
    }

    public void DisableRoom() {
        /*
         * Disable the trigger, the objects that make this room and the connected rooms.
         */

        roomTrigger.enabled = false;
        windowContainer.gameObject.SetActive(false);
        roomObjectsContainer.gameObject.SetActive(false);
        skySphere.gameObject.SetActive(false);
        entranceRoom.DisablePuzzleRoom();
        exitRoom.DisablePuzzleRoom();
        entranceRoom.DisableRoom();
        exitRoom.DisableRoom();
    }

    public void EnableRoom() {
        /*
         * Enable this WaitingRoom, it's AttachedRooms and their corresponding puzzleRooms 
         * and the two potential WaitingRooms that are behind and ahead of this room.
         */
         
        roomTrigger.enabled = true;
        windowContainer.gameObject.SetActive(true);
        roomObjectsContainer.gameObject.SetActive(true);
        skySphere.gameObject.SetActive(true);
        entranceRoom.EnablePuzzleRoom();
        exitRoom.EnablePuzzleRoom();
        entranceRoom.EnableRoom();
        exitRoom.EnableRoom();

        /* Soft enable the other nearby WaitingRooms (if applicable) */
        if(previousRoom != null) { previousRoom.SoftEnable(); }
        if(nextRoom != null) { nextRoom.SoftEnable(); }
    }

    public void SoftEnable() {
        /*
         * Only enable this waitingRoom and it's AttachedRooms. Do not change any other
         * puzzle rooms and other waiting rooms.
         */

        roomTrigger.enabled = true;
        windowContainer.gameObject.SetActive(true);
        roomObjectsContainer.gameObject.SetActive(true);
        entranceRoom.EnableRoom();
        exitRoom.EnableRoom();
    }
}
