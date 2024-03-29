﻿using UnityEngine;
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
    public Material windowGlassMaterial;
    public Material windowCrackedGlassMaterial;
    public Texture skySphereTexture;
    private Material skySphereMaterial;

    /* The GameObject object used as the skysphere for the outside window */
    public GameObject skySphere;

    /* The textures used for the materials of the objects that form the room */
    public Material waitingRoomUnlitMaterial;
    public Material unlitMaterial;
    private Material windowFrameMaterial;
    public Texture floorTexture;
    public Texture wallTexture;
    public Texture ceilingTexture;
    public Texture windowFrameTexture;

    /* The alternative textures the room will use once it changes textures */
    public Texture2D floorTextureAlt;
    public Texture2D wallTextureAlt;
    public Texture2D ceilingTextureAlt;
    public Texture2D windowFrameTextureAlt;
    public float textureAltFavor;

    /* The color tints of it's connected rooms */
    public Vector3 entranceTint;
    public Vector3 exitTint;

    /* The glass shard emitted from the window as it cracks */
    public Material glassShardMaterial;
    public GameObject particleSystemObjectReference;

    /* The range of the texture rgb value clamping for this room */
    public float textureClampRangeFloor;
    public float textureClampRangeWall;
    public float textureClampRangeCeiling;
    public float textureClampRangeFrame;
    public float textureClampOffsetFloor;
    public float textureClampOffsetWall;
    public float textureClampOffsetCeiling;
    public float textureClampOffsetFrame;
    public float textureClampTiming;


    /* -------- Built-In Functions ---------------------------------------------------- */

    void Awake() {
        /*
         * Ensure every room is disabled before the Start() functions start running to allow 
         * only the bare minimum required rooms being active once the player finishes loading.
         */

        /* do not disable the rooms if it is in the editor */
        /*if(!Application.isEditor) {
            entranceRoom.DisablePuzzleRoom();
            exitRoom.DisablePuzzleRoom();
            DisableRoom();
        }*/
    }

    public void Start() {
        /*
         * On start-up, recreate the room's skeleton any puzzle rooms from the AttachedRooms.
         */
         
        /* Update the walls of the room */
        UpdateRoom();

        /* Place the window in a good position in the room */
        UpdateWindows();

        /* Update the sky sphere */
        UpdateSkySphere();

        /* Every waitingRoom will start disabled */
        DisableRoom();
    }
    
    void Update() {
        UpdateMaterialTextureRounding();
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
            SoftEnable();
            if(previousRoom != null) { previousRoom.SoftEnable(); }
            if(nextRoom != null) { nextRoom.SoftEnable(); }
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
                exitRoom.EnablePuzzleRoom();
                if(nextRoom != null) { nextRoom.SoftEnable(); }
                if(previousRoom != null) { previousRoom.DisableRoom(); }
            }

            /* Player moved backwards through the puzzles */
            else {
                entranceRoom.EnablePuzzleRoom();
                if(previousRoom != null) { previousRoom.SoftEnable(); }
                if(nextRoom != null) { nextRoom.DisableRoom(); }
            }
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
        windowExit.position = roomCenter + new Vector3(0, 50000, roomCenter.z*1000);

        /* Calculate the sizes of this waitingRoom */
        xDist = Mathf.Abs(entranceRoom.exitPointFront.position.x - exitRoom.exitPointBack.position.x) + xEntranceDist/2f + xExitDist/2f;
        yDist = Mathf.Max(yEntranceDist, yExitDist);
        zDist = Mathf.Abs(entranceRoom.exitPointFront.position.z - exitRoom.exitPointBack.position.z);
        
        /* Update the materials used by this room and the attachedRooms once we have the positions calculated */
        UpdateMaterials();

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

        /* Extend the box colliders of the ceiling, floor, left and rigght walls to cover the room's corners.
         * Note that due to this extension, all attachedRooms should be atleast 1 unit in depth */
        roomWalls[0].GetComponent<BoxCollider>().size += new Vector3(1, 0, 1);
        roomWalls[3].GetComponent<BoxCollider>().size += new Vector3(1, 0, 1);
        roomWalls[1].GetComponent<BoxCollider>().size += new Vector3(0, 0, 1);
        roomWalls[2].GetComponent<BoxCollider>().size += new Vector3(0, 0, 1);
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
        

        /* Make each portal's camera only render the skySphere layer and up to a camera depth of 2 */
        for(int i = 0; i < windows.Length; i++) {
            /* Set the render layer */
            windows[i].portalSet.EntrancePortal.portalMesh.GetComponent<PortalView>().SetSkySphereLayer(true);
            windows[i].portalSet.EntrancePortal.backwardsPortalMesh.GetComponent<PortalView>().SetSkySphereLayer(true);
            windows[i].portalSet.ExitPortal.portalMesh.GetComponent<PortalView>().SetSkySphereLayer(true);
            windows[i].portalSet.ExitPortal.backwardsPortalMesh.GetComponent<PortalView>().SetSkySphereLayer(true);

            /* Set the camera depth limit */
            windows[i].portalSet.EntrancePortal.portalMesh.GetComponent<PortalView>().maxCameraDepth = 2;
            windows[i].portalSet.EntrancePortal.backwardsPortalMesh.GetComponent<PortalView>().maxCameraDepth = 2;
            windows[i].portalSet.ExitPortal.portalMesh.GetComponent<PortalView>().maxCameraDepth = 2;
            windows[i].portalSet.ExitPortal.backwardsPortalMesh.GetComponent<PortalView>().maxCameraDepth = 2;

            /* Set the portal's incompatible to true */
            windows[i].portalSet.incompatible = true;
        }

        /* Send a command to update the windows with the new given parameters */
        for(int i = 0; i < windows.Length; i++) {
            windows[i].UpdateWindow(particleSystemObjectReference);

            /* Add a legDetect function to each window */
            windows[i].windowPieces[4].gameObject.AddComponent<DetectPlayerLegRay>();
            windows[i].windowPieces[4].gameObject.GetComponent<DetectPlayerLegRay>().objectType = 2;


            /*
             * Update the window's particleSystem to reflect it's size and waitingRoom.
             */
            ParticleSystem glassEmitter = windows[i].windowPieces[4].GetComponent<ParticleSystem>();

            /* Set the collision to the waitingRoom's floor */
            var collision = glassEmitter.collision;
            collision.enabled = true;
            collision.SetPlane(0, roomWalls[0].transform);
            collision.dampen = new ParticleSystem.MinMaxCurve(0, 0.2f);
            collision.bounce = new ParticleSystem.MinMaxCurve(0, 0.5f);

            /* Set the sizes of the emission box to that of the window */
            var shape = glassEmitter.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.box = new Vector3(windows[i].windowWidth, windows[i].windowHeight, windows[i].frameDepth);
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
        float viewDistance = CustomPlayerController.cameraFarClippingPlane*0.2f;
        skySphere.transform.localScale = new Vector3(viewDistance, viewDistance, viewDistance);
        skySphere.name = "Sky sphere";
        skySphere.layer = PortalSet.maxLayer + 1;

        /* Place the sky sphere around the window exit */
        skySphere.transform.position = windowExit.position;

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
        window.crackedGlassMaterial = windowCrackedGlassMaterial;
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

    void UpdateMaterials() {
        /*
         * Update the materials used by this WaitingRoom and it's ConnectedRooms
         */

        /* Floor */
        floorMaterial = Instantiate(waitingRoomUnlitMaterial);
        floorMaterial.SetTexture("_MainTex", floorTexture);
        floorMaterial.SetTexture("_SecondTex", floorTextureAlt);
        floorMaterial.SetTextureScale("_MainTex", new Vector2(5, 5));
        floorMaterial.name = "Floor (WaitingRoom " + this.GetInstanceID() + ")";

        /* Wall */
        wallMaterial = Instantiate(waitingRoomUnlitMaterial);
        wallMaterial.SetTexture("_MainTex", wallTexture);
        wallMaterial.SetTexture("_SecondTex", wallTextureAlt);
        wallMaterial.name = "Wall (WaitingRoom " + this.GetInstanceID() + ")";

        /* Ceiling */
        ceilingMaterial = Instantiate(waitingRoomUnlitMaterial);
        ceilingMaterial.SetTexture("_MainTex", ceilingTexture);
        ceilingMaterial.SetTexture("_SecondTex", ceilingTextureAlt);
        ceilingMaterial.name = "Ceiling (WaitingRoom " + this.GetInstanceID() + ")";

        /* Window Frame */
        windowFrameMaterial = Instantiate(waitingRoomUnlitMaterial);
        windowFrameMaterial.SetTexture("_MainTex", windowFrameTexture);
        windowFrameMaterial.SetTexture("_SecondTex", windowFrameTextureAlt);
        windowFrameMaterial.name = "Window Border (WaitingRoom " + this.GetInstanceID() + ")";
        
        /* Set the properties of the shader used by these materials */
        floorMaterial.SetFloat("_RoomCenter", roomCenter.z);
        floorMaterial.SetFloat("_RoomDepthBuffer", zDist/2f);
        floorMaterial.SetFloat("_TextureZLengthEntr", entranceRoom.roomLength*2);
        floorMaterial.SetFloat("_TextureZLengthExit", exitRoom.roomLength*2);
        floorMaterial.SetVector("_EntrTint", entranceTint);
        floorMaterial.SetVector("_ExitTint", exitTint);
        floorMaterial.SetFloat("_TextureFavor", 0);

        wallMaterial.SetFloat("_RoomCenter", roomCenter.z);
        wallMaterial.SetFloat("_RoomDepthBuffer", zDist/2f);
        wallMaterial.SetFloat("_TextureZLengthEntr", entranceRoom.roomLength*2);
        wallMaterial.SetFloat("_TextureZLengthExit", exitRoom.roomLength*2);
        wallMaterial.SetVector("_EntrTint", entranceTint);
        wallMaterial.SetVector("_ExitTint", exitTint);
        wallMaterial.SetFloat("_TextureFavor", 0);

        ceilingMaterial.SetFloat("_RoomCenter", roomCenter.z);
        ceilingMaterial.SetFloat("_RoomDepthBuffer", zDist/2f);
        ceilingMaterial.SetFloat("_TextureZLengthEntr", entranceRoom.roomLength*2);
        ceilingMaterial.SetFloat("_TextureZLengthExit", exitRoom.roomLength*2);
        ceilingMaterial.SetVector("_EntrTint", entranceTint);
        ceilingMaterial.SetVector("_ExitTint", exitTint);
        ceilingMaterial.SetFloat("_TextureFavor", 0);

        windowFrameMaterial.SetFloat("_RoomCenter", roomCenter.z);
        windowFrameMaterial.SetFloat("_RoomDepthBuffer", zDist/2f);
        windowFrameMaterial.SetFloat("_TextureZLengthEntr", entranceRoom.roomLength*2);
        windowFrameMaterial.SetFloat("_TextureZLengthExit", exitRoom.roomLength*2);
        windowFrameMaterial.SetVector("_EntrTint", entranceTint);
        windowFrameMaterial.SetVector("_ExitTint", exitTint);
        windowFrameMaterial.SetFloat("_TextureFavor", 0);

        /* Link the materials to it's AttachedRooms */
        entranceRoom.floorMaterial = floorMaterial;
        entranceRoom.wallMaterial = wallMaterial;
        entranceRoom.ceilingMaterial = ceilingMaterial;
        entranceRoom.wallConnectorMaterial = windowFrameMaterial;
        entranceRoom.roomSide = false;
        exitRoom.floorMaterial = floorMaterial;
        exitRoom.wallMaterial = wallMaterial;
        exitRoom.ceilingMaterial = ceilingMaterial;
        exitRoom.wallConnectorMaterial = windowFrameMaterial;
        exitRoom.roomSide = true;
    }

    public void UpdateMaterialTextureRounding() {
        /*
         * Update the _RoundingRange of the room's materials
         */

        /* Have an offset go back and forth depending on the time to add a "breathin room" animation */
        float offset = Mathf.Sin(Mathf.PI * 2 * (Time.time / textureClampTiming));
        
        floorMaterial.SetFloat("_RoundRange", textureClampRangeFloor + textureClampOffsetFloor*offset);
        wallMaterial.SetFloat("_RoundRange", textureClampRangeWall + textureClampOffsetWall*offset);
        ceilingMaterial.SetFloat("_RoundRange", textureClampRangeCeiling + textureClampOffsetCeiling*offset);
        windowFrameMaterial.SetFloat("_RoundRange", textureClampRangeFrame + textureClampOffsetFrame*offset);
    }


    /* -------- Event Functions ---------------------------------------------------- */

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
         * Disable the room's portals and the rooms connected to it
         */

        /* Disable the windows of the room */
        ChangeWindowState(false);

        /* Disable the adjecent puzzle rooms */
        entranceRoom.DisablePuzzleRoom();
        exitRoom.DisablePuzzleRoom();
    }

    public void EnableRoom() {
        /*
         * Enable the room's portals and the rooms connected to it.
         * This is used on startup to ensure the connected rooms are fully loaded
         */

        /* Enable this room's portals if they havent already (used just for the player startup) */
        SoftEnable();

        /* Enable the portals of the adjacent puzzle rooms */
        entranceRoom.EnablePuzzleRoom();
        exitRoom.EnablePuzzleRoom();

        /* Soft enable the other nearby WaitingRooms (if applicable) */
        if(previousRoom != null) { previousRoom.SoftEnable(); }
        if(nextRoom != null) { nextRoom.SoftEnable(); }
    }

    public void SoftEnable() {
        /*
         * Only enable the portals of the windows in this waiting room
         */
         
        ChangeWindowState(true);
    }

    public void ChangeWindowState(bool state) {
        /*
         * Change the rendering state of this waitingRoom's windows. This is used
         * to disable and enable the windows while the player moves between rooms.
         */

        for(int i = 0; i < windowContainer.childCount; i++) {
            windowContainer.GetChild(i).GetComponent<Window>().SetWindowState(state);
        }
    }

    public float GetRoomWidth() {
        /*
         * Return the width (X axis) of the room
         */

        return xDist;
    }

    public float GetRoomLength() {
        /*
         * Return the length (Z axis) of the room
         */

        return zDist;
    }

    public float GetRoomHeight() {
        /*
         * Return the height (Y axis) of the room
         */

        return yDist;
    }

    public void ChangeTextures() {
        /*
         * Update the textures used in this room. This to called when the player starts falling into previous rooms
         */

        floorMaterial.SetFloat("_TextureFavor", textureAltFavor);
        wallMaterial.SetFloat("_TextureFavor", textureAltFavor);
        ceilingMaterial.SetFloat("_TextureFavor", textureAltFavor);
        windowFrameMaterial.SetFloat("_TextureFavor", textureAltFavor);
    }
}
