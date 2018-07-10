using UnityEngine;
using System.Collections;

/* 
 * A script that builds the walls around the puzzle room. 
 * 
 * Requires a link to the exits and entrances that connect to the room.
 */
[ExecuteInEditMode]
public class PuzzleRoomEditor : MonoBehaviour {

    /* The attached rooms that connect to the exit and entrance holes */
    public AttachedRoom entrance;
    public AttachedRoom exit;

    /* The position that the entrance and the exit attached rooms will be in */
    public Transform puzzleRoomEntrancePoint;
    public Transform puzzleRoomExitPoint;

    /* The parent of all the walls and clouds that form the puzzle room */
    public GameObject puzzleRoomWalls;
    public GameObject puzzleRoomClouds;

    /* The extra priority added into the noise function */
    public float gradPriority;
    /* Scaling of the main, second and noise wall textures on the wall material */
    public Vector2 mainScale;
    public Vector2 secondScale;
    public Vector2 noiseScale;

    /* The walls that make up the puzzle room. Each wall has a specific place in the array and the room */
    [HideInInspector]
    public GameObject[] walls;

    /* The upper and lower "clouds" which make the puzzle room seem infinite */
    [HideInInspector]
    public GameObject upperClouds;
    [HideInInspector]
    public GameObject lowerClouds;
    /* How many "clouds" (i.e. a flat square mesh) are in a set */
    public float cloudHeight;
    /* Distance between a cloud set's first and last cloud mesh */
    public int cloudAmount;
    /* How much distance the clouds sets are from the room's center */
    public float cloudDensity;
    /* Y displacement applied to the clouds container. Used to keep the player between both cloud sets. */
    public float cloudOffset = 0;
    
    /* Stats on the room calculated once initialized */
    private float roomLength;
    private float roomHeight;
    public float roomWidth;
    
    /* The minimum distance the player needs to be from the center before the clouds move in confunction with them */
    //public float minYClouds;
    /* The maximum distance from the center the "puzzle area" occupies */
    public float maxYPlayArea;
    
    /* the minimum distance the player needs to fall from the level's center before getting teleported to the other side. */
    private float minYTeleport;

    /* Set this value to true to update the walls */
    public bool updateWalls;

    /* How much distance the walls will fade. Higher means slower fade over distance */
    public float fadeLength;

    /* How much distance the texture needs to travel before it begins fading. Expected to be positive. */
    public float preFadeLength;

    /* Height offset of the fade */
    public float fadeOffset;

    /* Scales the size of the UVs of the walls. */
    public float wallUVStretch;
    public float wallUV2Stretch;
    public float wallUV3Stretch;

    /* How much the UVs of each texture is animated */
    public Vector2 mainTexAnim;
    public Vector2 secondTexAnim;
    public Vector2 noiseTexAnim;

    /* The wall and cloud materials used in the puzzleRoom */
    public Material cloudMaterial;
    public Material cloudBlockerMaterial;
    public Material wallMaterial;

    /* The textures used by the walls of the puzzleRoom */
    public Texture2D mainWallTexture;
    public Texture2D secondWallTexture;
    public Texture2D mainWallTextureAlt;
    public Texture2D secondWallTextureAlt;
    public Texture2D noiseWallTexture;

    /* The materials used by the room along with their starting and alternative textures */
    public Material[] roomMaterials;
    public Texture2D[] roomMainTexture;
    public Texture2D[] roomAltTexture;
    /* The edge materials. The textures used are given by the index, which points to the position in the previous arrays */
    public Material[] roomEdgeMaterials;
    public int[] roomEdgeMainIndex;
    public int[] roomEdgeSecondIndex;
    
    /* The color tint that gets appleid to the walls of the room. Ranges are from [0, 1] */
    public Vector3 wallColorTint;
    public Vector3 wallColorTintAlt;
    public float centerTintOffset;
    

    /* -------- Built-In Unity Functions ---------------------------------------------------- */

    void Start() {
        /*
		 * force the walls to update on startup. 
		 */
         
        updateWalls = true;
        Update();
    }

    public void Update() {
        /*
         * Update the walls that form the puzzle room
         */
         
        if(updateWalls) {
            
            /* Move the attached rooms into their given positions and link them to this room */
            UpdateAttachedRooms();

            /* Calculate the new positionnal stats of this puzzleRoom */
            UpdateSizes();

            /* Update the material used by this room's walls */
            UpdateMaterial();

            /* Ensure the walls and clouds that form the room are properly created and positioned */
            CreateWalls();
            CreateClouds();

            /* Create and place the playerDetector collider */
            CreatePlayerDetector();
            
            updateWalls = false;
        }

        /* Update the UVs of the textures that need animation */
        AnimateUVS();

        /* Update the position of the clouds using cloudOffset */
        UpdateClouds();
    }

    void OnTriggerStay(Collider collider) {
        /*
         * If the player is within the puzzle room, check their relative height from the center.
         * If the distance is more than minYClouds, have the clouds follow the player.
         * If the distance is more than minYTeleport, teleport the player from top to bottom or vice versa.
         */

        if(collider.tag == "Player") {

            /* HHave the height of the center of each room be identical (y = 0) */
            Vector3 centerPoint = (puzzleRoomEntrancePoint.position + puzzleRoomExitPoint.position)/2f;
            centerPoint = new Vector3(centerPoint.x, 0, centerPoint.z);
            //Get the distance that the player is from the room's center
            float playerFromCenter = collider.transform.position.y - centerPoint.y;

            
            /* Teleport the player to the other top/bottom boundary */
            if(Mathf.Abs(playerFromCenter) > minYTeleport) {
                /* Teleport the player to the other side */
                collider.transform.position -= new Vector3(0, Mathf.Sign(playerFromCenter)*minYTeleport*2, 0);

                /* If the player teleported, update the playerFromCenter value */
                playerFromCenter = collider.transform.position.y - centerPoint.y;
            }

            /* Have the clouds follow the player, with it being more centered the further the player */
            if(Mathf.Abs(playerFromCenter) > maxYPlayArea) {
                cloudOffset = playerFromCenter;
                cloudOffset -= (maxYPlayArea*Mathf.Sign(playerFromCenter))
                        *(1 - ((Mathf.Abs(playerFromCenter) - maxYPlayArea)/(minYTeleport-maxYPlayArea)));

                /* When the player is out of the play area, force them to fast fall */
                collider.GetComponent<CustomPlayerController>().ApplyFastfall(true);
            }

            /* Player is within the room's minimum size, do not offset the clouds */
            else {
                cloudOffset = 0;
            }
            
            /* As the player gets further away from the room's center, increase their falling velocity */
            float fromCenterToEdgeRatio = (Mathf.Abs(playerFromCenter) - minYTeleport/3f) / (minYTeleport - minYTeleport/3f);
            if(Mathf.Abs(playerFromCenter) > minYTeleport/3f) {
                collider.GetComponent<CustomPlayerController>().gravityVectorMod = 1 + 6*fromCenterToEdgeRatio;
            }
            else {
                collider.GetComponent<CustomPlayerController>().gravityVectorMod = 1;
            }
            
            /* When far from the room, make the player camera's near clipping large. This is to
             * prevent the depth buffer issue when having a small near clip plane value.
             * It is safe to change the value because the player is far from any portals at this point. */
            if(Mathf.Abs(playerFromCenter) > (maxYPlayArea + 20)) {
                collider.GetComponent<CustomPlayerController>().playerCamera.nearClipPlane = 0.14f;
            }
            /* Set the player camera's near clip plane to it's saved value */
            else {
                collider.GetComponent<CustomPlayerController>().playerCamera.nearClipPlane = CustomPlayerController.cameraNearClippingPlane;
            }

            
            /* Depending on what index the current puzzleRoom is, run a specific function from the playerController */
            collider.GetComponent<CustomPlayerController>().UpdatePlayerRoom(transform.parent.name);

            /* Check if the player does not have the proper rotation to fall into the clouds */
            if(collider.transform.up != Vector3.up && collider.transform.up != Vector3.down) {

                /* Reset the hint text if the player is within the play area */
                if(Mathf.Abs(playerFromCenter) <= maxYPlayArea) {
                    collider.GetComponent<CustomPlayerController>().playerMenu.ForceHintReset(6);
                    collider.GetComponent<CustomPlayerController>().playerMenu.ForceHintReset(7);
                    collider.GetComponent<CustomPlayerController>().playerMenu.ForceHintReset(8);
                }

                /* Update the hint text relative to the player's distance from the play area */
                else if(Mathf.Abs(playerFromCenter) < maxYPlayArea + 30) {
                    collider.GetComponent<CustomPlayerController>().playerMenu.DelayChangeHint(6);
                }
                else if(Mathf.Abs(playerFromCenter) < maxYPlayArea + 40) {
                    collider.GetComponent<CustomPlayerController>().playerMenu.SetHintText(7);
                }
                else {
                    collider.GetComponent<CustomPlayerController>().playerMenu.SetHintText(8);
                }
            }
        }
    }


    /* -------- Update Functions ---------------------------------------------------- */

	public void UpdateSizes(){
        /*
	     * Calculate the sizes of the room to be used with the creation functions.
		 * Length is given by the exit placement and height by cloud variables.
	     */

        /* Get the distance between the room's exit points's along the Z axis for the room length */
        roomLength = Mathf.Abs((entrance.exitPointFront.position - exit.exitPointBack.position).z);
        
        /* Calculate the distance needed for the player to teleport, which is the distance needed for the clouds to completely cover the level */
        minYTeleport = maxYPlayArea + cloudHeight + cloudDensity;

        /* Get the rooms height using the maximum distance the clouds can reach */
        roomHeight = maxYPlayArea*2 + (cloudHeight*2 + cloudDensity*2)*2;
    }

    private void UpdateAttachedRooms() {
        /*
         * Link and move the attached rooms to the givenpoints defined by the two puzzleRoomPoint transforms.
         * The attached rooms will be moved so their exit point shares the same transform as
         * the corresponding puzzleRoomPoint.
         */
        Vector3 distanceToExit;

        /* Link the two attached rooms to this puzzle room */
        entrance.UpdateAttachedPuzzleRoom(transform.parent.gameObject);
        exit.UpdateAttachedPuzzleRoom(transform.parent.gameObject);

        /* Reposition the entrance room */
        distanceToExit = entrance.exitPointFront.position - puzzleRoomEntrancePoint.position;
        entrance.transform.position -= distanceToExit;

        /* Reposition the exit room */
        distanceToExit = exit.exitPointBack.position - puzzleRoomExitPoint.position;
        exit.transform.position -= distanceToExit;
    }
    
    public void UpdateClouds() {
        /*
		 * Set the position of the cloud's container to move them all relative to the player's position
		 */

        puzzleRoomClouds.transform.localPosition = new Vector3(0, cloudOffset, 0);
      
      
		/* Checkif the clouds have UVs, and increment them if so */
        if(upperClouds != null && upperClouds.GetComponent<MeshFilter>() != null 
                && upperClouds.GetComponent<MeshFilter>().sharedMesh != null 
                && upperClouds.GetComponent<MeshFilter>().sharedMesh.uv != null){
        	UpdateCloudsUVs(upperClouds);
        }
        
        if(lowerClouds != null && lowerClouds.GetComponent<MeshFilter>() != null 
                && lowerClouds.GetComponent<MeshFilter>().sharedMesh != null 
                && lowerClouds.GetComponent<MeshFilter>().sharedMesh.uv != null){
        	UpdateCloudsUVs(lowerClouds);
        }
    }
    
    public void UpdateCloudsUVs(GameObject clouds){
        /*
    	 * Increment the UVs of the clouds to simulate them animating.
    	 */
        Vector2 uvIncrement;

        Vector2[] cloudUVs = clouds.GetComponent<MeshFilter>().sharedMesh.uv;

        /* Each layer in a cloud set is represented by 4 UV values */
        for(int i = 0; i < cloudAmount; i++){

            //Do not always increment the UVs in the same way
            uvIncrement = new Vector2( (1+(i % 4))*0.0002f, (1+((i+2) % 4))*0.0002f);

            cloudUVs[i*4 + 0] += uvIncrement;
    		cloudUVs[i*4 + 1] += uvIncrement;
            cloudUVs[i*4 + 2] += uvIncrement;
            cloudUVs[i*4 + 3] += uvIncrement;

            /* Keep the UVs by looping through the same textures bounds */
            if(cloudUVs[i*4].x > 1) {
                cloudUVs[i*4 + 0].x -= 1;
                cloudUVs[i*4 + 1].x -= 1;
                cloudUVs[i*4 + 2].x -= 1;
                cloudUVs[i*4 + 3].x -= 1;
            }
            if(cloudUVs[i*4].y > 1) {
                cloudUVs[i*4 + 0].y -= 1;
                cloudUVs[i*4 + 1].y -= 1;
                cloudUVs[i*4 + 2].y -= 1;
                cloudUVs[i*4 + 3].y -= 1;
            }
        }

        clouds.GetComponent<MeshFilter>().sharedMesh.uv = cloudUVs;
    }

    public void UpdateMaterial() {
        /*
         * Update the materials used by this puzzleRoom and it's contents
         */
         
        /* Re-create the material with the proper shader */
        wallMaterial = new Material(Shader.Find("Unlit/PuzzleWallShader"));
        wallMaterial.name =  "Wall (" + transform.parent.name + ")";

        /* Assign the textures to the new material */
        wallMaterial.SetTexture("_MainTex", mainWallTexture);
        wallMaterial.SetTexture("_SecondTex", secondWallTexture);
        wallMaterial.SetTexture("_RepeatingNoiseTex", noiseWallTexture);

        /* Apply the scaling to each texture */
        wallMaterial.SetTextureScale("_MainTex", mainScale);
        wallMaterial.SetTextureScale("_SecondTex", secondScale);
        wallMaterial.SetTextureScale("_RepeatingNoiseTex", noiseScale);

        /* Set the parameters of the wall's shader that requires variables set to this room */
        wallMaterial.SetFloat("_TeleportHeight", minYTeleport);
        wallMaterial.SetFloat("_CenterOffset", centerTintOffset);
        wallMaterial.SetVector("_RoomColorTint", wallColorTint);
        wallMaterial.SetVector("_RoomColorTintAlt", wallColorTintAlt);
        
        /* Make sure the blocker material is properly created */
        cloudBlockerMaterial = new Material(Shader.Find("Unlit/Color"));
        cloudBlockerMaterial.color = Color.black;

        
        /* Set the main texture of each given room material */
        for(int i = 0; i < roomMaterials.Length; i++) {
            roomMaterials[i].SetTexture("_MainTex", roomMainTexture[i]);
        }
        /* Set the textures of the edges using the main textures */
        for(int i = 0; i < roomEdgeMaterials.Length; i++) {
            roomEdgeMaterials[i].SetTexture("_MainTex", roomMainTexture[roomEdgeMainIndex[i]]);
            roomEdgeMaterials[i].SetTexture("_SecondTex", roomMainTexture[roomEdgeSecondIndex[i]]);
        }
    }

    public void AnimateUVS() {
        /*
         * Use the given texAnim values to animate the UVs of the textures
         */
        Vector2[] UV, UV2, UV3;
        Mesh mesh;

        for(int i = 0; i < walls.Length; i++) {
            mesh = walls[i].GetComponent<MeshFilter>().sharedMesh;
            UV = mesh.uv;
            UV2 = mesh.uv2;
            UV3 = mesh.uv3;

            /* Update the UVs to change the texture in-game */
            if(mainTexAnim.magnitude != 0) { AnimateUV(ref UV, mainTexAnim); }
            if(secondTexAnim.magnitude != 0) { AnimateUV(ref UV2, secondTexAnim); }
            if(noiseTexAnim.magnitude != 0) { AnimateUV(ref UV3, noiseTexAnim); }

            /* Apply the changes to the mesh */
            mesh.uv = UV;
            mesh.uv2 = UV2;
            mesh.uv3 = UV3;
            walls[i].GetComponent<MeshFilter>().sharedMesh = mesh;
        }
    }

    public void AnimateUV(ref Vector2[] UV, Vector2 animation) {
        /*
         * Given the reference to an array of UV values, update each of them with the given animation amount.
         */

        for(int i = 0; i < UV.Length; i++) {
            UV[i] += animation;
        }
    }
    
    
    /* -------- Initilizing Functions ---------------------------------------------------- */

    private void CreateWalls() {
        /*
         * Re-create the walls that form the puzzle room using the placement of the entrance/exit points and room sizes.
         */
        Vector3 entrancePlacement = puzzleRoomEntrancePoint.position - transform.position;
        Vector3 exitPlacement = puzzleRoomExitPoint.position - transform.position;
        Vector3 pos;
        float UVYOffset;

        /* Reposition the attached rooms to fit the puzzle room's exit and entrance points */
        UpdateAttachedRooms();
        
        /* Ensure the walls array is emptied before creating new ones */
        if(walls != null) {
            for(int i = 0; i < walls.Length; i++) {
                if(walls[i] != null) {
                    DestroyImmediate(walls[i]);
                }
            }
        }

        /* Create the new walls array for the 10 walls that form the puzzle room */
        walls = new GameObject[10];
        puzzleRoomWalls.transform.localPosition = new Vector3(0, 0, 0);

        /* Create and place the room's side walls that reflect the room's width */
        walls[0] = new GameObject();
        walls[0].name = "Left Wall";
        walls[0].transform.parent = puzzleRoomWalls.transform;
        walls[0].transform.localEulerAngles = new Vector3(0, 90, 0);
        walls[0].transform.localPosition = new Vector3(0, 0, roomLength/2f) + 
                new Vector3(roomWidth/2f, 0, 0);
        CreateWallMesh(walls[0], roomLength, roomHeight, true, walls[0].transform.position, 0);

        walls[1] = new GameObject();
        walls[1].name = "Right wall";
        walls[1].transform.parent = puzzleRoomWalls.transform;
        walls[1].transform.localEulerAngles = new Vector3(0, -90, 0);
        walls[1].transform.localPosition = new Vector3(0, 0, roomLength/2f) + 
                new Vector3(-roomWidth/2f, 0, 0);
        CreateWallMesh(walls[1], roomLength, roomHeight, true, walls[1].transform.position, 0);

        /* Create and place the walls that are situated bellow the room's entrance/exit */
        walls[2] = new GameObject();
        walls[2].name = "Entrance wall bellow";
        walls[2].transform.parent = puzzleRoomWalls.transform;
        walls[2].transform.localEulerAngles = new Vector3(0, 180, 0);
        walls[2].transform.position = puzzleRoomEntrancePoint.position + 
                new Vector3(0, -roomHeight/4f - entrancePlacement.y/2f, 0);
        pos = new Vector3(-walls[2].transform.position.x, walls[2].transform.position.y, walls[2].transform.position.z);
        if(entrancePlacement.y < 0) {
            UVYOffset = 0;
        }
        else {
            UVYOffset = -1.5f*entrancePlacement.y;
        }
        CreateWallMesh(walls[2], entrance.exitWidth, roomHeight/2f + entrancePlacement.y, false, pos, UVYOffset);

        walls[3] = new GameObject();
        walls[3].name = "Exit wall bellow";
        walls[3].transform.parent = puzzleRoomWalls.transform;
        walls[3].transform.localEulerAngles = new Vector3(0, 0, 0);
        walls[3].transform.position = puzzleRoomExitPoint.position + 
                new Vector3(0, -roomHeight/4f - exitPlacement.y/2f, 0);
        if(exitPlacement.y < 0) {
            UVYOffset = 0;
        }else {
            UVYOffset = -1.5f*exitPlacement.y;
        }
        CreateWallMesh(walls[3], exit.exitWidth, roomHeight/2f + exitPlacement.y, false, walls[3].transform.position, UVYOffset);

        /* Create and place the walls that are situated above the room's entrance/exit */
        walls[4] = new GameObject();
        walls[4].name = "Entrance wall above";
        walls[4].transform.parent = puzzleRoomWalls.transform;
        walls[4].transform.localEulerAngles = new Vector3(0, 180, 0);
        walls[4].transform.position = puzzleRoomEntrancePoint.position + 
                new Vector3(0, roomHeight/4f + entrance.exitHeight/2f - entrancePlacement.y/2f, 0);
        pos = new Vector3(-walls[4].transform.position.x, walls[4].transform.position.y, walls[4].transform.position.z);
        if(entrancePlacement.y > 0) {
            UVYOffset = 0;
        }
        else {
            UVYOffset = entrance.exitHeight*1.5f + entrancePlacement.y*1.5f;
        }
        CreateWallMesh(walls[4], entrance.exitWidth, roomHeight/2f - entrance.exitHeight - entrancePlacement.y, false, pos, UVYOffset);

        walls[5] = new GameObject();
        walls[5].name = "Exit wall above";
        walls[5].transform.parent = puzzleRoomWalls.transform;
        walls[5].transform.localEulerAngles = new Vector3(0, 0, 0);
        walls[5].transform.position = puzzleRoomExitPoint.position + 
                new Vector3(0, roomHeight/4f + exit.exitHeight/2f - exitPlacement.y/2f, 0);
        if(exitPlacement.y > 0) {
            UVYOffset = 0;
        }
        else {
            UVYOffset = exit.exitHeight*1.5f + exitPlacement.y*1.5f;
        }
        CreateWallMesh(walls[5], exit.exitWidth, roomHeight/2f - exit.exitHeight - exitPlacement.y, false, walls[5].transform.position, UVYOffset);

        /* Create and place the walls that are on the left and right of the entrance */
        walls[6] = new GameObject();
        walls[6].name = "Entrance left wall";
        walls[6].transform.parent = puzzleRoomWalls.transform;
        walls[6].transform.localEulerAngles = new Vector3(0, 180, 0);
        walls[6].transform.localPosition = new Vector3(-roomWidth/4f, 0, 0) + 
                new Vector3(-entrance.exitWidth/4 + entrancePlacement.x/2f, 0, 0);
        pos = new Vector3(-walls[6].transform.position.x, walls[6].transform.position.y, walls[6].transform.position.z);
        CreateWallMesh(walls[6], roomWidth/2f - entrance.exitWidth/2f + entrancePlacement.x, roomHeight, false, pos, 0);

        walls[7] = new GameObject();
        walls[7].name = "Entrance right wall";
        walls[7].transform.parent = puzzleRoomWalls.transform;
        walls[7].transform.localEulerAngles = new Vector3(0, 180, 0);
        walls[7].transform.localPosition = new Vector3(roomWidth/4f, 0, 0) +
                new Vector3(entrance.exitWidth/4f + entrancePlacement.x/2f, 0, 0);
        pos = new Vector3(-walls[7].transform.position.x, walls[7].transform.position.y, walls[7].transform.position.z);
        CreateWallMesh(walls[7], roomWidth/2f - entrance.exitWidth/2f - entrancePlacement.x, roomHeight, false, pos, 0);

        /* Create and place the walls that are on the left and right of the exit */
        walls[8] = new GameObject();
        walls[8].name = "Exit wall left";
        walls[8].transform.parent = puzzleRoomWalls.transform;
        walls[8].transform.localEulerAngles = new Vector3(0, 0, 0);
        walls[8].transform.localPosition = new Vector3(-roomWidth/4f, 0, roomLength) +
                new Vector3(-exit.exitWidth/4f + exitPlacement.x/2f, 0, 0);
        CreateWallMesh(walls[8], roomWidth/2f - exit.exitWidth/2f + exitPlacement.x, roomHeight, false, walls[8].transform.position, 0);

        walls[9] = new GameObject();
        walls[9].name = "Exit wall right";
        walls[9].transform.parent = puzzleRoomWalls.transform;
        walls[9].transform.localEulerAngles = new Vector3(0, 0, 0);
        walls[9].transform.localPosition = new Vector3(roomWidth/4f, 0, roomLength) +
                new Vector3(exit.exitWidth/4f + exitPlacement.x/2f, 0, 0);
        CreateWallMesh(walls[9], roomWidth/2f - exit.exitWidth/2f - exitPlacement.x, roomHeight, false, walls[9].transform.position, 0);
    }

    private void CreateClouds() {
        /*
         * Create two sets of flat meshes at the upper and lower bounds of the puzzle room.
         * They are used to make the puzzle room seem endless to the player and to mask the 
         * teleport the player undergoes if they fall too far from the room's center.
         */

        /* Remove the previously created clouds before recreating them */
        if(upperClouds != null) {
            DestroyImmediate(upperClouds);
        }
        if(lowerClouds != null) {
            DestroyImmediate(lowerClouds);
        }
        
        /* Create and position new upperClouds */
        upperClouds = new GameObject();
        CreateCloudsMesh(upperClouds, roomWidth, roomLength, cloudAmount, cloudDensity);
        upperClouds.name = "Upper Clouds";
        upperClouds.transform.parent = puzzleRoomClouds.transform;
        upperClouds.transform.localPosition = new Vector3(0, cloudHeight, roomLength/2f);
        upperClouds.transform.localEulerAngles = new Vector3(180, 0, 0);

        /* Create and position new lowerclouds */
        lowerClouds = new GameObject();
        CreateCloudsMesh(lowerClouds, roomWidth, roomLength, cloudAmount, cloudDensity);
        lowerClouds.name = "Lower Clouds";
        lowerClouds.transform.parent = puzzleRoomClouds.transform;
        lowerClouds.transform.localPosition = new Vector3(0, -cloudHeight, roomLength/2f);
    }

    private void CreatePlayerDetector() {
        /*
         * Ensure there is a BoxCollider attached to this gameObject that will be used 
         * to detect the player's position in the room. It spans the entire room.
         */

        /* Make sure the BoxCollider exists */
        if(GetComponent<BoxCollider>() == null) {
            gameObject.AddComponent<BoxCollider>();
        }

        /* Layer the trigger to prevent the player's raytraces from colliding with it */
        gameObject.layer = 2;

        /* It must be a trigger to allow the player to move inside the room */
        GetComponent<BoxCollider>().isTrigger = true;

        /* Set the sizes of the collider to be equal to the whole room */
        GetComponent<BoxCollider>().center = new Vector3(0, 0, roomLength/2f);
        GetComponent<BoxCollider>().size = new Vector3(roomWidth, roomHeight, roomLength);
    }


    /* -------- Event Functions ---------------------------------------------------- */

    public void ChangeTextures() {
        /*
         * Update the textures used in this room. This to called when the player starts falling into previous rooms
         */
         
        /* Set the alternative textures and color tints of the wall */
        wallMaterial.SetTexture("_MainTex", mainWallTextureAlt);
        wallMaterial.SetTexture("_SecondTex", secondWallTextureAlt);
        /* If both the top and bottom color tints are the same, use a zero color tint for both sides */
        if(wallColorTint == wallColorTintAlt) {
            wallMaterial.SetVector("_RoomColorTint", Vector3.zero);
            wallMaterial.SetVector("_RoomColorTintAlt", Vector3.zero);
        }
        
        /* Set the alt texture of each room material */
        for(int i = 0; i < roomMaterials.Length; i++) {
            roomMaterials[i].SetTexture("_MainTex", roomAltTexture[i]);
        }
        /* Set the textures of the edges using the alt textures */
        for(int i = 0; i < roomEdgeMaterials.Length; i++) {
            roomEdgeMaterials[i].SetTexture("_MainTex", roomAltTexture[roomEdgeMainIndex[i]]);
            roomEdgeMaterials[i].SetTexture("_SecondTex", roomAltTexture[roomEdgeSecondIndex[i]]);
        }
    }


    /* -------- Helper Functions ---------------------------------------------------- */

    private void CreateWallMesh(GameObject wall, float xScale, float yScale, bool sideWall, Vector3 objectPos, float UV4YOffset) {
        /*
         * Use the given parameters to create the mesh that forms a wall.
         * 
         * The given boolean sideWall is true if the wall being created does not have an entrence or exit attached.
         */
        Mesh wallMesh = new Mesh();
        Vector3[] vertices = null;
        Vector2[] UV, UV2, UV3, UV4;
        int[] triangles = null;

        /* Use the meshCreator function to create the basics of the wall */
        CreateLargeMesh(xScale, yScale, ref vertices, ref triangles);

        /* Set the UVs of the plane */
        UV = new Vector2[vertices.Length];
        UV2 = new Vector2[vertices.Length];
        UV3 = new Vector2[vertices.Length];
        UV4 = new Vector2[vertices.Length];
        float fadeAmount = 0;
        if(fadeLength != 0) {
            fadeAmount = 1f/fadeLength;
        }
        for(int i = 0; i < vertices.Length/4; i++) {
            /* The UV is the UV of the main texture */
            UV[i*4 + 0] = new Vector2(1f/wallUVStretch*(vertices[i*4 + 0].x + objectPos.x), 1f/wallUVStretch*(vertices[i*4 + 0].y + objectPos.y));
            UV[i*4 + 1] = new Vector2(1f/wallUVStretch*(vertices[i*4 + 1].x + objectPos.x), 1f/wallUVStretch*(vertices[i*4 + 1].y + objectPos.y));
            UV[i*4 + 2] = new Vector2(1f/wallUVStretch*(vertices[i*4 + 2].x + objectPos.x), 1f/wallUVStretch*(vertices[i*4 + 2].y + objectPos.y));
            UV[i*4 + 3] = new Vector2(1f/wallUVStretch*(vertices[i*4 + 3].x + objectPos.x), 1f/wallUVStretch*(vertices[i*4 + 3].y + objectPos.y));

            /* The UV2 is the UV of the second texture */
            UV2[i*4 + 0] = new Vector2(1f/wallUV2Stretch*(vertices[i*4 + 0].x + objectPos.x), 1f/wallUV2Stretch*(vertices[i*4 + 0].y + objectPos.y));
            UV2[i*4 + 1] = new Vector2(1f/wallUV2Stretch*(vertices[i*4 + 1].x + objectPos.x), 1f/wallUV2Stretch*(vertices[i*4 + 1].y + objectPos.y));
            UV2[i*4 + 2] = new Vector2(1f/wallUV2Stretch*(vertices[i*4 + 2].x + objectPos.x), 1f/wallUV2Stretch*(vertices[i*4 + 2].y + objectPos.y));
            UV2[i*4 + 3] = new Vector2(1f/wallUV2Stretch*(vertices[i*4 + 3].x + objectPos.x), 1f/wallUV2Stretch*(vertices[i*4 + 3].y + objectPos.y));

            /* The UV3 is the UV of the noise texture */
            UV3[i*4 + 0] = new Vector2(1f/wallUV3Stretch*(vertices[i*4 + 0].x + objectPos.x), 1f/wallUV3Stretch*(vertices[i*4 + 0].y + objectPos.y));
            UV3[i*4 + 1] = new Vector2(1f/wallUV3Stretch*(vertices[i*4 + 1].x + objectPos.x), 1f/wallUV3Stretch*(vertices[i*4 + 1].y + objectPos.y));
            UV3[i*4 + 2] = new Vector2(1f/wallUV3Stretch*(vertices[i*4 + 2].x + objectPos.x), 1f/wallUV3Stretch*(vertices[i*4 + 2].y + objectPos.y));
            UV3[i*4 + 3] = new Vector2(1f/wallUV3Stretch*(vertices[i*4 + 3].x + objectPos.x), 1f/wallUV3Stretch*(vertices[i*4 + 3].y + objectPos.y));

            /* The UV4 is calculated using the height of the vertex in the world position */
            UV4[i*4 + 0] = new Vector2(gradPriority, fadeAmount*(fadeOffset + Mathf.Abs(objectPos.y + vertices[i*4 + 0].y) + UV4YOffset - preFadeLength));
            UV4[i*4 + 1] = new Vector2(gradPriority, fadeAmount*(fadeOffset + Mathf.Abs(objectPos.y + vertices[i*4 + 1].y) + UV4YOffset - preFadeLength));
            UV4[i*4 + 2] = new Vector2(gradPriority, fadeAmount*(fadeOffset + Mathf.Abs(objectPos.y + vertices[i*4 + 2].y) + UV4YOffset - preFadeLength));
            UV4[i*4 + 3] = new Vector2(gradPriority, fadeAmount*(fadeOffset + Mathf.Abs(objectPos.y + vertices[i*4 + 3].y) + UV4YOffset - preFadeLength));
        }

        /* Assign the parameters to the mesh */
        wallMesh.vertices = vertices;
        wallMesh.triangles = triangles;
        wallMesh.uv = UV;
        wallMesh.uv2 = UV2;
        wallMesh.uv3 = UV3;
        wallMesh.uv4 = UV4;
        wallMesh.RecalculateNormals();

        /* Add a meshFilter and a meshRenderer to be able to draw the wall */
        wall.AddComponent<MeshFilter>();
        wall.GetComponent<MeshFilter>().mesh = wallMesh;
        wall.AddComponent<MeshRenderer>();
        wall.GetComponent<MeshRenderer>().material = wallMaterial;

        /* Use a thick box collider for the wall's collisions */
        BoxCollider wallBox = wall.AddComponent<BoxCollider>();
        float colliderDepth = 1;
        wallBox.center = new Vector3(0, 0, colliderDepth/2f);
        /* If we are creating a sidewall, extend it's xScale to cover the corners of the room */
        if(sideWall) {
            wallBox.size = new Vector3(xScale + colliderDepth*2, yScale, colliderDepth);
        }
        else {
            wallBox.size = new Vector3(xScale, yScale, colliderDepth);
        }
    }

    public void CreateCloudsMesh(GameObject clouds, float xScale, float zScale, int cloudCount, float cloudDepth){
    	/*
    	 * Create and assign the mesh used to represent the clouds in the room's top and bottom bounds.
    	 */
    	Mesh cloudMesh = new Mesh();
        Vector3[] vertices = new Vector3[4*cloudCount];
        Vector3[] initialVertices = null;
        Vector2[] UV = new Vector2[4*cloudCount];
        int[] triangles = new int[6*cloudCount];
        int[] initialTriangles = null;
        float currentHeight;
        float uvOffset;
        
        /* Use the meshCreator function to create the first layer of the clouds, then reuse its values on the rest */
        CreateMesh(xScale, zScale, ref initialVertices, ref initialTriangles);
        
        /* Create the cloud mesh one layer at a time */
        for(int i = 0; i < cloudCount; i++){
        	
        	/* Create and assign the vertices for this layer */
        	currentHeight = -cloudDepth * ((float)i / (float)(cloudCount-1));
            vertices[4*i + 0] = initialVertices[0] + new Vector3(0, currentHeight, 0);
            vertices[4*i + 1] = initialVertices[1] + new Vector3(0, currentHeight, 0);
            vertices[4*i + 2] = initialVertices[2] + new Vector3(0, currentHeight, 0);
            vertices[4*i + 3] = initialVertices[3] + new Vector3(0, currentHeight, 0);
        
        	/* Create and assign the triangles using the previously set vertices */
        	triangles[6*i + 0] = 4*i + initialTriangles[0];
        	triangles[6*i + 1] = 4*i + initialTriangles[1];
        	triangles[6*i + 2] = 4*i + initialTriangles[2];
        	triangles[6*i + 3] = 4*i + initialTriangles[3];
        	triangles[6*i + 4] = 4*i + initialTriangles[4];
        	triangles[6*i + 5] = 4*i + initialTriangles[5];

            /* Set the UVs if this truangle set */
            uvOffset = Random.Range(0.1f, 0.5f);
            if(i % 2 == 1) {
                uvOffset *= -1;
            }
            UV[4*i + 0] = new Vector2(0, 0);
            UV[4*i + 1] = new Vector2(uvOffset, 0);
            UV[4*i + 2] = new Vector2(uvOffset, uvOffset);
            UV[4*i + 3] = new Vector2(0, uvOffset);
        }
        
        /* Assign the parameters to the mesh */
        cloudMesh.vertices = vertices;
        cloudMesh.triangles = triangles;
        cloudMesh.uv = UV;
        cloudMesh.RecalculateNormals();

        /* Add a meshFilter and meshRenderer to be able to draw the clouds */
        clouds.AddComponent<MeshFilter>();
        clouds.GetComponent<MeshFilter>().sharedMesh = cloudMesh;
        clouds.AddComponent<MeshRenderer>();
        clouds.GetComponent<MeshRenderer>().material = cloudMaterial;
        
        /* Place a single mesh at the end of the clouds to block the players view past the clouds */
        GameObject blocker = new GameObject();
        Mesh blockerMesh = new Mesh();
        blocker.name = "End of clouds";
        blocker.transform.position =  new Vector3(0, -cloudDensity - (cloudDepth/cloudCount), 0);
        blocker.transform.parent = clouds.transform;

        /* Create and assign the blocker's mesh */
        CreateMesh(xScale, zScale, ref initialVertices, ref initialTriangles);
        blockerMesh.vertices = initialVertices;
        blockerMesh.triangles = initialTriangles;
        blocker.AddComponent<MeshFilter>().sharedMesh = blockerMesh;
        blocker.AddComponent<MeshRenderer>().material = cloudBlockerMaterial;
    }
    
    public void CreateLargeMesh(float xScale, float yScale, ref Vector3[] vertices, ref int[] triangles){
        /*
    	 * Create a large mesh using the given scale values and save it's verts and triangles into the given references.
         * If the mesh is going to be very large, split it into multiple vertices and triangles.
    	 *
    	 * It expects the given arrays to not yet be initialized.
    	 */

        /* Calculate the sizes of the mesh and create it's arrays */
        float maxVerticeDifference = 100f;
        int verticeSections = Mathf.CeilToInt(yScale/maxVerticeDifference) + 1;
        vertices = new Vector3[4*verticeSections];
        triangles = new int[6*verticeSections];

        /* Populate the array by going through each section */
        float sectionSize = yScale/verticeSections;
        float currentY = -0.5f*yScale;
        float nextY = currentY + sectionSize;
        for(int i = 0; i < verticeSections; i++) {
            vertices[i*4 + 0] = new Vector3(0.5f*xScale, nextY, 0);
            vertices[i*4 + 1] = new Vector3(-0.5f*xScale, nextY, 0);
            vertices[i*4 + 2] = new Vector3(-0.5f*xScale, currentY, 0);
            vertices[i*4 + 3] = new Vector3(0.5f*xScale, currentY, 0);
            currentY = nextY;
            nextY += sectionSize;

            triangles[i*6 + 0] = i*4 + 2;
            triangles[i*6 + 1] = i*4 + 1;
            triangles[i*6 + 2] = i*4 + 0;
            triangles[i*6 + 3] = i*4 + 3;
            triangles[i*6 + 4] = i*4 + 2;
            triangles[i*6 + 5] = i*4 + 0;
        }
    }

    private void CreateMesh(float xScale, float zScale, ref Vector3[] vertices, ref int[] triangles) {
        /*
         * Use the parameters to create a simple mesh using 4 vertices.
         */

        /* Set the vertices */
        vertices = new Vector3[4];
        vertices[0] = new Vector3(0.5f*xScale, 0, 0.5f*zScale);
        vertices[1] = new Vector3(-0.5f*xScale, 0, 0.5f*zScale);
        vertices[2] = new Vector3(-0.5f*xScale, 0, -0.5f*zScale);
        vertices[3] = new Vector3(0.5f*xScale, 0, -0.5f*zScale);

        /* Set the triangles */
        triangles = new int[]{
            2, 1, 0,
            3, 2, 0
        };
    }
}
