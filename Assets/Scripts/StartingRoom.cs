using UnityEngine;
using System.Collections;

/*
 * The room the player is expected to start in. This script simply builds the walls to the given variables.
 * It also requires a link to it's AttachedRoom that connects it to the next WaitingRoom to know it's sizes.
 */
[ExecuteInEditMode]
public class StartingRoom : ConnectedRoom {

    /* The AttachedRoom that leads into this empty room */
    public AttachedRoom exit;

    /* Stairs that connects the room's exit to the floor */
    public StairsCreator stairs;
    public Material stairsStepMaterial;
    public Material stairsOtherMaterial;

    /* The breakable window used in this room */
    public Window window;

    /* How much distance is between the exit and the man wall. Z axis. */
    public float roomDepth;

    /* How much extra width the room has. This does not include the exit's width. X axis. */
    public float extraRoomWidth;

    /* How much extra height the room has. This does not include the exit's height. Y axis. */
    public float extraHeight;

    /* How much distance the floor will be from the base of the room's exit */
    public float roomBellowHeight;

    /* Stats used for the window of the room */
    public float frameThickness;
    public float frameDepth;
    public float windowFromWall;
    public Transform windowExit;
    public Material windowFrameMaterial;
    public Material windowGlassMaterial;
    public Texture skySphereTexture;
    private bool glassBroken;

    /* The particleSystem that will produce a bunch of shattered glass */
    public Material particleMaterial;

    /* The audio source that will play the glass shattering sound effect. Have it linked to the sound and mixer group in the editor. */
    public AudioSource glassShatterSource;

    /* The terrainGenerator that generates the world for the outside window */
    public TerrainController outsideTerrain;


    /* -------- Built-In Functions ---------------------------------------------------- */

    void Start () {
        /*
         * On startup, build the walls of the room
         */

        UpdateWalls();
        UpdateWindow();
        UpdateCollider();
    }

    void OnTriggerExit(Collider player) {
        /*
         * When the player leaves the room and the glass is broken, 
         * have the outsideTerrain change it's focus target to the player.
         */

        /* Ensure the collider entering the trigger is a player */
        if(player.GetComponent<CustomPlayerController>() != null) {

            /* Ensure the glass is broken as the player leaves the room */
            if(glassBroken) {
                
                /* Update the terrain generator's focus object to be the player instead of the window */
                outsideTerrain.focusPoint = player.transform;
            }
        }
    }


    /* -------- Update Functions ---------------------------------------------------- */

    void UpdateWalls() {
        /*
         * Build the walls of the room using the linked AttachedRoom as reference
         */

        /* Get the desired sizes of the room */
        float roomWidth = extraRoomWidth + exit.exitWidth;
        float upperRoomHeight = extraHeight + exit.exitHeight;
        Vector3 upperCenter = exit.exitPointBack.position + new Vector3(0, 0, -roomDepth/2f);
        float fullRoomHeight = upperRoomHeight + roomBellowHeight;
        

        /* Re-create each wall for the room */
        CreateObjects(ref roomWalls, 9, upperCenter);

        /* Set unique values for each individual wall */
        roomWalls[0].name = "Floor";
        roomWalls[0].transform.position += new Vector3(0, -roomBellowHeight, 0);
        CreatePlane(roomWalls[0], roomWidth, roomDepth, 8, floorMaterial, 0, false);
        //Attach a DetectPlayerLegRay script to the floor
        roomWalls[0].AddComponent<DetectPlayerLegRay>();
        roomWalls[0].GetComponent<DetectPlayerLegRay>().objectType = 0;

        roomWalls[1].name = "Left wall";
        roomWalls[1].transform.position += new Vector3(-roomWidth/2f, upperRoomHeight/2f - (fullRoomHeight - upperRoomHeight)/2f, 0);
        CreatePlane(roomWalls[1], fullRoomHeight, roomDepth, 8, wallMaterial, 1, true);

        roomWalls[2].name = "Right wall";
        roomWalls[2].transform.position += new Vector3(roomWidth/2f, upperRoomHeight/2f - (fullRoomHeight - upperRoomHeight)/2f, 0);
        CreatePlane(roomWalls[2], fullRoomHeight, roomDepth, 8, wallMaterial, 1, false);

        roomWalls[3].name = "Ceiling";
        roomWalls[3].transform.position += new Vector3(0, upperRoomHeight, 0);
        CreatePlane(roomWalls[3], roomWidth, roomDepth, 8, ceilingMaterial, 0, true);

        roomWalls[4].name = "Back wall";
        roomWalls[4].transform.position += new Vector3(0, upperRoomHeight/2f - (fullRoomHeight - upperRoomHeight)/2f, -roomDepth/2f);
        CreatePlane(roomWalls[4], roomWidth, fullRoomHeight, 8, wallMaterial, 2, true);

        roomWalls[5].name = "Exit top wall";
        roomWalls[5].transform.position += new Vector3(0, upperRoomHeight - extraHeight/2f, roomDepth/2f);
        CreatePlane(roomWalls[5], roomWidth, extraHeight, 8, wallMaterial, 2, false);

        roomWalls[6].name = "Exit left wall";
        roomWalls[6].transform.position += new Vector3(-roomWidth/2f + extraRoomWidth/4f, (upperRoomHeight - extraHeight)/2f, roomDepth/2f);
        CreatePlane(roomWalls[6], extraRoomWidth/2f, (upperRoomHeight - extraHeight), 8, wallMaterial, 2, false);

        roomWalls[7].name = "Exit right wall";
        roomWalls[7].transform.position += new Vector3(roomWidth/2f - extraRoomWidth/4f, (upperRoomHeight - extraHeight)/2f, roomDepth/2f);
        CreatePlane(roomWalls[7], extraRoomWidth/2f, (upperRoomHeight - extraHeight), 8, wallMaterial, 2, false);
        
        roomWalls[8].name = "Exit Bottom wall";
        roomWalls[8].transform.position += new Vector3(0, -roomBellowHeight/2f, roomDepth/2f);
        CreatePlane(roomWalls[8], roomWidth, roomBellowHeight, 8, wallMaterial, 2, false);


        /* Position the key points of the stairs for this room */
        stairs.endPoint.position = upperCenter + new Vector3(0, 0, roomDepth/2f);
        stairs.startPoint.position = upperCenter + new Vector3(0, 0, roomDepth/2f) + new Vector3(0, -roomBellowHeight, -roomBellowHeight);
        stairs.stairsMaterial = stairsStepMaterial;
        stairs.otherMaterial = stairsOtherMaterial;
        stairs.stairsWidth = exit.exitWidth;
        stairs.baseDepth = roomBellowHeight;
        stairs.upVector = new Vector3(0, 1, 0);
        stairs.updateStairs = true;
        stairs.resetAngle = true;
    }
    
    void UpdateWindow() {
        /*
         * Update the values of the window and position it in an appropriate spot in the room
         */
        glassBroken = false;

        /* Set the size of the window's frame */
        window.frameThickness = frameThickness;
        window.frameDepth = frameDepth;

        /* Make the window occupy most of the back wall */
        float roomWidth = extraRoomWidth + exit.exitWidth;
        float upperRoomHeight = extraHeight + exit.exitHeight;
        float fullRoomHeight = upperRoomHeight + roomBellowHeight;
        window.windowHeight = fullRoomHeight - frameThickness*2 - windowFromWall;
        window.windowWidth = roomWidth - frameThickness*2 - windowFromWall;

        /* Place the window's entrance on the room's back wall */
        Vector3 backWallCenter = exit.exitPointBack.position + new Vector3(0, -roomBellowHeight + frameThickness + windowFromWall/2f, -roomDepth);
        window.insidePos = backWallCenter;
        window.insideRot = new Vector3(0, 180, 0);
        
        /* Place the outside window/exit portal using the windowExit transform given to this script  */
        window.outsidePos = windowExit.position;
        window.outsideRot = windowExit.eulerAngles;

        /* Set the materials that the window will use */
        window.frameMaterial = windowFrameMaterial;
        window.glassMaterial = windowGlassMaterial;

        /* Send a command to update the windows with the new given parameters */
        window.UpdateWindow();
        
        /* Add a DetectPlayerLegRay script onto the glass of the window, making the window's glass break upon player leg contact */
        if(window.windowPieces[4].GetComponent<DetectPlayerLegRay>() == null) {
            window.windowPieces[4].AddComponent<DetectPlayerLegRay>();
            window.windowPieces[4].GetComponent<DetectPlayerLegRay>().objectType = 1;

        }
        if(window.windowPieces[9].GetComponent<DetectPlayerLegRay>() == null) {
            window.windowPieces[9].AddComponent<DetectPlayerLegRay>();
            window.windowPieces[9].GetComponent<DetectPlayerLegRay>().objectType = 1;
        }

        /* Create the particle system used by the otuside window's glass */
        UpdateParticleSystem();
    }
    
    void UpdateParticleSystem() {
        /*
         * Create and set the stats of the particle emitter placed on the outside window 
         * if it is not already created
         */
        ParticleSystem particleSystem = gameObject.GetComponent<ParticleSystem>();

        /* Create a new particle system if there is not already one attached */
        if(particleSystem == null) {
            particleSystem = gameObject.AddComponent<ParticleSystem>();
        }
        
        /* Set the particle that will be emitted */
        ParticleSystemRenderer rend = particleSystem.GetComponent<ParticleSystemRenderer>();
        rend.material = particleMaterial;

        /* Adjust the emission rate to not produce any particles passively */
        ParticleSystem.EmissionModule emission = particleSystem.emission;
        emission.rate = 0;

        /* Adjust the shape where the particles will be emitted from */
        ParticleSystem.ShapeModule shape = particleSystem.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.box = new Vector3(window.windowWidth, window.windowHeight, 0.01f);
    }

    void UpdateCollider() {
        /*
         * Create a collider that surrounds the room
         */
        BoxCollider roomCollider = GetComponent<BoxCollider>();

        /* Create the room collider if it does not exist */
        if(roomCollider == null) { roomCollider = gameObject.AddComponent<BoxCollider>(); }
        roomCollider.isTrigger = true;

        /* Position the collider into the center of the room */
        roomCollider.center = exit.exitPointBack.position + new Vector3(0, -(roomBellowHeight - (extraHeight + exit.exitHeight))/2f, -roomDepth/2f);

        /* Set the size of the collider to encompass the whole room */
        roomCollider.size = new Vector3((extraRoomWidth + exit.exitWidth), (extraHeight + exit.exitHeight + roomBellowHeight), (roomDepth));
    }


    /* -------- Event Functions ---------------------------------------------------- */

    public void BreakGlass(GameObject playerObject) {
        /*
         * This is called from an outside function when the window of the room needs to be broken.
         */
        glassBroken = true;

        Debug.Log("Destroy the glass objects");

        /* Set the hit windows to be inactive */
        window.windowPieces[4].SetActive(false);
        window.windowPieces[4].GetComponent<DetectPlayerLegRay>().objectType = -1;
        window.windowPieces[9].SetActive(false);
        window.windowPieces[9].GetComponent<DetectPlayerLegRay>().objectType = -1;
        
        /* Disable the wall with that holds the window to let the player fall through */
        roomWalls[4].GetComponent<Collider>().enabled = false;

        /* Create a burst of glass particles */
        ParticleSystem particleSystem = gameObject.GetComponent<ParticleSystem>();
        if(particleSystem != null) {
            ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams();
            emitParams.position = windowExit.position + new Vector3(0, window.windowHeight/2f, 0);
            emitParams.applyShapeToPosition = true;
            particleSystem.Emit(emitParams, 10000);
        }

        /* Once the player breaks the glass, put them into the "outside" state */
        playerObject.GetComponent<CustomPlayerController>().ActiveOutsideState();

        /* Play the audioSource of the glass shattering */
        glassShatterSource.Play();
    }
}
