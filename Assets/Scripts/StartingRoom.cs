using UnityEngine;
using System.Collections;

/*
 * The room the player is expected to start in. This script simply builds the walls to the given variables.
 * It also requires a link to it's AttachedRoom that connects it to the next WaitingRoom to know it's sizes.
 */
public class StartingRoom : ConnectedRoom {

    /* The AttachedRoom that leads into this empty room */
    public AttachedRoom exit;

    /* Stairs that connects the room's exit to the floor */
    public StairsCreator stairs;

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
    public Material windowGlassMaterial;
    public Texture skySphereTexture;
    private bool glassBroken;

    /* The particleSystem that will produce a bunch of shattered glass */
    private new ParticleSystem particleSystem;
    private ParticleSystem.Particle glassParticles;
    public Material particleMaterial;

    /* The audio source that will play the glass shattering sound effect. Have it linked to the sound and mixer group in the editor. */
    public AudioSource glassShatterSource;

    /* The terrainGenerator that generates the world for the outside window */
    public TerrainController outsideTerrain;

    /* How high the outside window aims to be above the ground */
    [HideInInspector]
    public float windowExitExtraHeight;

    /* The textures used for the materials of the objects that form the room */
    public Material unlitMaterial;
    private Material windowFrameMaterial;
    private Material stairsStepMaterial;
    private Material stairsOtherMaterial;
    public Texture floorTexture;
    public Texture wallTexture;
    public Texture ceilingTexture;
    public Texture windowFrameTexture;
    public Texture stairsStepTexture;
    public Texture stairsOtherTexture;

    /* The textures the room will use once it changes textures */
    public Texture floorTextureAlt;
    public Texture wallTextureAlt;
    public Texture ceilingTextureAlt;
    public Texture windowFrameTextureAlt;
    public Texture stairsStepTextureAlt;
    public Texture stairsOtherTextureAlt;
    
    /* Tracks the speed gained from gravity on the particles */
    private float gravityMod = 0;


    /* -------- Built-In Functions ---------------------------------------------------- */

    public void Start () {
        /*
         * On startup, build the walls of the room
         */

        /* Run the terrainController's start function to create the noise provider, used with placing the outside window. */
        outsideTerrain.StartAlt();
        UpdateRoom();

        /* Once the room is setup, link the window's first camera to the TerrainController */
        window.portalSet.EntrancePortal.backwardsPortalMesh.GetComponent<PortalView>().Start();
        outsideTerrain.windowCam = window.portalSet.EntrancePortal.backwardsPortalMesh.transform.GetChild(0);
        outsideTerrain.windowExitPoint = windowExit;
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

                /* Set the new focus point to be the player's camera */
                //outsideTerrain.focusPoint = player.GetComponent<CustomPlayerController>().playerCamera.transform;
            }
        }
    }
    
    
    /* -------- Update Functions ---------------------------------------------------- */

    public void UpdateRoom() {
        /*
         * Update the room's walls, colliders and portal
         */

        UpdateMaterials();
        UpdateWalls();
        UpdateWindow();
        UpdateCollider();
    }

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
        stairs.Update();

        /* Reposition the attachedRoom */
        exit.update = true;
        exit.Update();
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

        /* Set the window's positions and call it's function to reposition them in the world */
        Vector3 backWallCenter = exit.exitPointBack.position + new Vector3(0, -roomBellowHeight + frameThickness + windowFromWall/2f, -roomDepth);
        window.insidePos = backWallCenter;
        window.insideRot = new Vector3(0, 180, 0);
        /* Place the window's exit at a distance just outside the player's view distance, ensuring they cannot see the rooms.
         * Also, place the window in the center of a chunk using the terrain's chunkLength. */
        float chunkDist = outsideTerrain.chunkLength;
        windowExit.position = -new Vector3(chunkDist/2f, 0, (outsideTerrain.chunkViewRange+0.5f)*chunkDist);
        windowExitExtraHeight = 25;
        UpdateOutsideWindowPositon(true);

        /* Set the materials that the window will use */
        window.frameMaterial = windowFrameMaterial;
        window.glassMaterial = windowGlassMaterial;

        /* Send a command to update the windows with the new given parameters */
        window.UpdateWindow();

        /* Make the window's portal's camera render the terrain layer */
        window.portalSet.EntrancePortal.portalMesh.GetComponent<PortalView>().SetRenderTerrain(true);
        window.portalSet.EntrancePortal.backwardsPortalMesh.GetComponent<PortalView>().SetRenderTerrain(true);
        window.portalSet.ExitPortal.portalMesh.GetComponent<PortalView>().SetRenderTerrain(false);
        window.portalSet.ExitPortal.backwardsPortalMesh.GetComponent<PortalView>().SetRenderTerrain(false);

        /* Add a DetectPlayerLegRay script onto the glass of the window, making the window's glass break upon player leg contact */
        if(window.windowPieces[4].GetComponent<DetectPlayerLegRay>() == null) {
            window.windowPieces[4].AddComponent<DetectPlayerLegRay>();
            window.windowPieces[4].GetComponent<DetectPlayerLegRay>().objectType = 1;

        }
        if(window.windowPieces[9].GetComponent<DetectPlayerLegRay>() == null) {
            window.windowPieces[9].AddComponent<DetectPlayerLegRay>();
            window.windowPieces[9].GetComponent<DetectPlayerLegRay>().objectType = 1;
        }
        
        /* Add the flare layer to the window's camera as it will be viewing the sun */
        window.AddFlareLayer();

        /* To ensure the window's cameras will render a sun flare, remove the colliders on the outside window */
        window.windowPieces[9].AddComponent<BoxCollider>().enabled = false;
        
        /* Create the particle system used by the otuside window's glass */
        UpdateParticleSystem();
    }
    
    void UpdateParticleSystem() {
        /*
         * Create and set the stats of the particle emitter placed on the outside window 
         * if it is not already created
         */

        /* Create a new particle system if there is not already one attached */
        particleSystem = gameObject.GetComponent<ParticleSystem>();
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

        /* Set the layer to ignoreRaycast for the room's collider */
        gameObject.layer = 2;

        /* Position the collider into the center of the room */
        roomCollider.center = exit.exitPointBack.position + new Vector3(0, -(roomBellowHeight - (extraHeight + exit.exitHeight))/2f, -roomDepth/2f);

        /* Set the size of the collider to encompass the whole room */
        roomCollider.size = new Vector3((extraRoomWidth + exit.exitWidth), (extraHeight + exit.exitHeight + roomBellowHeight), (roomDepth));
    }

    void UpdateMaterials() {
        /*
         * Update the materials used by the startingRoom
         */

        /* Floor */
        floorMaterial = Instantiate(unlitMaterial);
        floorMaterial.SetTexture("_MainTex", floorTexture);
        floorMaterial.SetTextureScale("_MainTex", new Vector2(5, 5));
        floorMaterial.name = "Floor (StartingRoom)";

        /* Wall */
        wallMaterial = Instantiate(unlitMaterial);
        wallMaterial.SetTexture("_MainTex", wallTexture);
        wallMaterial.name = "Wall (StartingRoom)";

        /* Ceiling */
        ceilingMaterial = Instantiate(unlitMaterial);
        ceilingMaterial.SetTexture("_MainTex", ceilingTexture);
        ceilingMaterial.name = "Ceiling (StartingRoom)";

        /* Window Frame */
        windowFrameMaterial = Instantiate(unlitMaterial);
        windowFrameMaterial.SetTexture("_MainTex", windowFrameTexture);
        windowFrameMaterial.name = "Window Border (StartingRoom)";

        /* Stairs */
        stairsStepMaterial = Instantiate(unlitMaterial);
        stairsStepMaterial.SetTexture("_MainTex", stairsStepTexture);
        stairsStepMaterial.name = "Stairs Step (StartingRoom)";
        stairsOtherMaterial = Instantiate(unlitMaterial);
        stairsOtherMaterial.SetTexture("_MainTex", stairsOtherTexture);
        stairsOtherMaterial.name = "Stairs Other (StartingRoom)";
    }


    /* -------- Event Functions ---------------------------------------------------- */

    public void UpdateOutsideWindowPositon(bool instantUpdate) {
        /*
         * Re-position the outside window to match the position of windowExit. This can be run in real-time.
         * 
         * instantUpdate controls how the position is updated. If it's true, we simply set the window's height 
         * to it's proper value. If it's false, then take into account the current window placement and slowly
         * adjust the window's height to reach the proper height after multiple calls to this function.
         * This will make the window smoothly adjust to it's expected height.
         */
         
        /* Get the expected height of the window relative to the terrain bellow it */
        float terrainHeight = outsideTerrain.GetTerrainHeightAt(windowExit.position.x, windowExit.position.z)*outsideTerrain.height + windowExitExtraHeight;
        float heightDifference = terrainHeight - windowExit.transform.position.y;

        /* Add a portion of the heightDifference to the current window to have it smoothly adjust to the height changes */
        /* Only add a portion of heightDifference if the portion is large enough */
        if(!instantUpdate && Mathf.Abs(heightDifference) > 0.025f) {
            terrainHeight = windowExit.transform.position.y + heightDifference*0.05f*Time.deltaTime*60;
        }

        /* Update the window's world positions */
        windowExit.transform.position = new Vector3(windowExit.position.x, terrainHeight, windowExit.position.z);
        window.outsidePos = windowExit.position;
        window.outsideRot = windowExit.eulerAngles;
        window.UpdateWindowPosition();
    }

    public void BreakGlass(GameObject playerObject) {
        /*
         * This is called from an outside function when the window of the room needs to be broken.
         */
        glassBroken = true;
        
        /* Set the hit windows to be inactive */
        window.windowPieces[4].SetActive(false);
        window.windowPieces[4].GetComponent<DetectPlayerLegRay>().objectType = -1;
        window.windowPieces[9].SetActive(false);
        window.windowPieces[9].GetComponent<DetectPlayerLegRay>().objectType = -1;
        
        /* Disable the wall with that holds the window to let the player fall through */
        roomWalls[4].GetComponent<Collider>().enabled = false;

        /* Create a burst of glass particles */
        if(particleSystem != null) {
            ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams();
            emitParams.position = windowExit.position + new Vector3(0, window.windowHeight/2f, 0);
            emitParams.applyShapeToPosition = true;
            particleSystem.Emit(emitParams, 5000);
        }

        /* Put the room into the terrainLayer to ensure the glass particles will render outside */
        gameObject.layer = PortalSet.maxLayer + 2;

        /* Once the player breaks the glass, put them into the "outside" state */
        playerObject.GetComponent<CustomPlayerController>().StartFallingOutside();

        /* Play the audioSource of the glass shattering */
        glassShatterSource.Play();
    }
    
    public void ChangeTextures() {
        /*
         * Update the textures used in this room. This to called when the player starts falling into previous rooms
         */

        //For now, just remove the textures used on all the room's materials
        floorMaterial.SetTexture("_MainTex", floorTextureAlt);
        wallMaterial.SetTexture("_MainTex", wallTextureAlt);
        ceilingMaterial.SetTexture("_MainTex", ceilingTextureAlt);
        windowFrameMaterial.SetTexture("_MainTex", windowFrameTextureAlt);
        stairsStepMaterial.SetTexture("_MainTex", stairsStepTextureAlt);
        stairsOtherMaterial.SetTexture("_MainTex", stairsOtherTextureAlt);
    }
    
	public void UpdateTimeRate(float roomTimeRate, float soundtimeRate){
        /*
         * Given a roomTimeRate and a soundTimeRate, adjust how the glass shattering and
         * the glass particles react by simmulating time slowing down.
		 */

        /* Set the breaking glass audio source to use the audio time rate */
        glassShatterSource.pitch = soundtimeRate;

        /* Depending on how long/distance travelled, decrease the Y velocity to simulate gravity */
        gravityMod += Time.deltaTime * roomTimeRate * 7.5f;

        /* Edit the velocity over lifetime */
        ParticleSystem.VelocityOverLifetimeModule velocityOverLifetime = particleSystem.velocityOverLifetime;
        velocityOverLifetime.x = new ParticleSystem.MinMaxCurve(5*roomTimeRate, -5*roomTimeRate);
        velocityOverLifetime.y = new ParticleSystem.MinMaxCurve((2 - gravityMod)*roomTimeRate, (-2 - gravityMod)*roomTimeRate);
        velocityOverLifetime.z = new ParticleSystem.MinMaxCurve(-1*roomTimeRate, -10*roomTimeRate);

        /* Edit the rotation over lifetime */
        ParticleSystem.RotationOverLifetimeModule rotationOverLifetime = particleSystem.rotationOverLifetime;
        rotationOverLifetime.x = new ParticleSystem.MinMaxCurve(3.14f*roomTimeRate, 9.42f*roomTimeRate);
        rotationOverLifetime.y = new ParticleSystem.MinMaxCurve(3.14f*roomTimeRate, 9.42f*roomTimeRate);
        rotationOverLifetime.z = new ParticleSystem.MinMaxCurve(3.14f*roomTimeRate, 9.42f*roomTimeRate);
    }
}
