﻿using UnityEngine;
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
    public BreakableWindow window;

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
    public float windowHeight;
    public float windowWidth;
    public Transform windowExit;
    public Material windowFrameMaterial;
    public Material windowGlassMaterial;
    public Texture skySphereTexture;



    /* -------- Built-In Functions ---------------------------------------------------- */

    void Start () {
        /*
         * On startup, build the walls of the room
         */

        UpdateWalls();
        UpdateWindow();
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
         
        /* Set the size of the window's frame */
        window.frameThickness = frameThickness;
        window.frameDepth = frameDepth;

        /* Make the window occupy most of the back wall */
        float windowFromWall = 0.5f;
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
        window.skySphereTexture = skySphereTexture;

        /* Send a command to update the windows with the new given parameters */
        window.UpdateWindow();
    }
}
