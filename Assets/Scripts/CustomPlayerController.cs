using UnityEngine;
using System.Collections;

/* 
 * The potential states the player can be in.
 * standing: Legs are connecting the player to the ground, 
 * 	Effenctivly removing any effect a gravity vector could hsve.
 * falling: Legs are looking for an object to connect to
 *	 As the player is being pulled down by gravity.
 */
public enum PlayerStates{
    Standing,
	Falling,
    FastFalling
};


/*
 * A custom character controller that uses UserInputs to handle movement. It uses "legs" to keep
 * it's "body" above the floor, letting the player walk up and down stairs or slopes smoothly. 
 */
public class CustomPlayerController : MonoBehaviour {
    public int state;

	/* --- Attached GameObjects ------------------- */
    /* The UserInputs object linked to this player */
    private UserInputs inputs;
    /* The current position of the camera. Smoothly morphs to restingCameraTransform each frame */
    public Transform currentCameraTransform;
    /* The camera used for the player's view */
    public Camera playerCamera;


	/* --- Player Control/Movement ----------------- */
    /* The direction and magnitude of player's movement input */
    private Vector3 inputVector = Vector3.zero;
    /* How fast a player moves using player inputs */
    public float movementSpeed;
    public float runSpeedMultiplier;
    /* Sliding determines how much of getAxis should be used over getAxisRaw. */
    [Range(1, 0)]
    public float sliding;

    /* How fast a player accelerates towards their feet when falling. */
    public float gravity;
    /* The Y velocity of the player along with its max(positive) */
    public float currentYVelocity;
    public float maxYVelocity;
    
    /* How fast a player travels upward when they jump */
    public float jumpSpeed;
    /* Used to determine the state of the jump. If true, the next jump opportunity will cause the player to jump. */
    private bool jumpPrimed;
    /* The state of the jump key on the current and previous frame. true = pressed */
    private bool jumpKeyPrevious = false;
    private bool jumpKeyCurrent = false;


	/* --- Body/Leg Sizes---------------------- */
    /* The sizes of the player's capsule collider */
    public float playerBodyLength;
    public float playerBodyRadius;

    /* Percentage of player radius that is used to sepperate the legs from the player's center */
    [Range(1, 0)]
    public float legGap;
    /* How much distance will be between the player's collider and the floor */
    public float givenLegLength;
    private float currentLegLength;
    /* How low a player can step down from their legLength to snap to the ground */
    public float givenStepHeight;
    private float currentStepHeight;

    /* How many extra legs are used when handling grounded checks */
    public int extraLegs;
    /* The length of each leg of the player */
    private float[] extraLegLenths;
    
    /* The average position of all the player's standing feet */
    private Vector3 currentFootPosition;




	
    //Default (0, 1, 0)
    //DOES GIVENPOS HAVE A VALUE? WE DONT NEED IT
    public Vector3 givenPosition;
    //Default (0, 0, 1)
    public Vector3 givenDirection;
    //Default (0, 1, 0)
    public Vector3 givenUp;


	/* --- Camera Positioning ---------------------- */
    /* The viewing angle of the player's camera */
	/* Current rotations of the camera */
	/* These should be private */
    public float cameraXRotation;
    public float cameraYRotation;
    /* How high the player camera is from their body's origin */
    public float headHeight;
    /* An offset that differentiates currentCameraTransform from the expected head height */
    public float cameraYOffset;
    /* How fast cameraYOffset morphs towards 0 each frame, in percentage. */
    [Range(1, 0)]
    public float morphPercentage;
    
    
    
    
    
    

    

    /* -------------- Built-in Unity Functions ---------------------------------------------------------- */

    void Start() {
        /*
         * Initilize required objects and set starting values for certain variables 
         */

        /* Create the UserInputs object linked to this player */
        inputs = new UserInputs();

        /* Initilize the leg lengths */
        extraLegLenths = new float[extraLegs + 1];

        /* Put the starting foot position at the base of the default player model */
        currentFootPosition = transform.TransformPoint(new Vector3(0, -GetComponent<CapsuleCollider>().height/2, 0));

        /* Adjust the player's height and width */
        GetComponent<CapsuleCollider>().height = playerBodyLength;
        GetComponent<CapsuleCollider>().radius = playerBodyRadius;

        /* Adjust the player model's position to reflect the player's leg length */
        transform.position = currentFootPosition;
        transform.localPosition += new Vector3(0, playerBodyLength/2f + 0, 0);
 
		/* Start the player in the falling state so they can link themselves to the floor */
		state = (int) PlayerStates.Standing;
   }

    void Update() {
        /*
         * Handle any player inputs. If they need to be redirected to a new script,
         * send the input signals to the current overriddenScript.
         */
        inputs.UpdateInputs();
         
		/* Run a given set of functions depending on the player state */
		if(state == (int) PlayerStates.Standing){
			UpdateStanding();
		}

        else if(PlayerIsFalling()) {
            UpdateFalling();
        }




		/* Draw a debug ray from the camera */
        //Vector3 pos = givenPosition;
        Vector3 pos = transform.position;
        Quaternion givenRotation = Quaternion.LookRotation(givenDirection, givenUp);
        float dis = 10f;
        RayTrace(ref pos, ref givenRotation, ref dis, true, true);

        //Draw a line in the camera's forward vector
        Debug.DrawLine(playerCamera.transform.position, playerCamera.transform.position + playerCamera.transform.rotation*Vector3.forward*0.5f, Color.green);
    }


    /* ----------------- Update Functions ------------------------------------------------------------- */

    void UpdateStanding() {
        /*
         * Handle the inputs of the user and the movement of the player
         * when their feet are connected to an object.
         */
         
        /* Move the player using the input vector and gravity */
        MovePlayer();

        /* Update the player's jumping conditions, letting the player prime their jump */
        UpdateJumpingValues();

        /* Find the footPosition of the player and check if they are falling or standing. Place the player's body position. */
        StepPlayer();

        /* Adjust the camera's default transform (currentCameraTransform) now that the player has moved */
        AdjustCameraPosition();
    }

	void UpdateFalling(){
        /*
		 * Handle player input and movement when the player
		 * Does not have any footing.
		 */

        /* Move the player using the input vector and gravity */
        MovePlayer();

        /* Find the footPosition of the player and check if they are falling or standing. Place the player's body position. */
        StepPlayer();

        /* Adjust the camera's default transform (currentCameraTransform) now that the player has moved */
        AdjustCameraPosition();
	}
    
    void RotateCamera() {
        /*
         * Take in user inputs to properly rotate the player camera's facing direction.
         */
         
        /* Ensure the X rotation does not overflow */
        cameraXRotation -= inputs.mouseX;
        if(cameraXRotation < 0) { cameraXRotation += 360; }
        else if(cameraXRotation > 360) { cameraXRotation -= 360; }

        /* Prevent the Y rotation from rotating too high or low */
        cameraYRotation += inputs.mouseY;
        cameraYRotation = Mathf.Clamp(cameraYRotation, -75, 75);

        /* Apply the rotations to the camera's default transform */
        currentCameraTransform.rotation *= Quaternion.Euler(-cameraYRotation, -cameraXRotation, 0);
    }

    void UpdateJumpingValues() {
        /*
         * Update the jumping values and attempt to jump in the right conditions.
    	 */
        jumpKeyPrevious = jumpKeyCurrent;
        jumpKeyCurrent = inputs.spaceBarHeld;

        /* Pressing the jump key will prime the jump */
        if(jumpKeyCurrent == true && jumpKeyPrevious == false) {
            jumpPrimed = true;
        }
        
        /* Releasing the jump key will attempt to make the player jump */
        else if(jumpKeyCurrent == false && jumpKeyPrevious == true) {
            JumpAttempt();
        }
    }

    void UpdateLegLengths() {
        /*
         * Change the player's leg lengths depending on the state they are in.
         * The legs start at the base of the player model, so legswill always
         * have a length equal or larger than half the bodyLength.
         */
		
		/* Keep the leg lengths their default size when standing */
		if(state == (int) PlayerStates.Standing){
			currentLegLength = givenLegLength + playerBodyLength/2f;
            currentStepHeight = givenStepHeight;
		}
		
		/* Shorten the legs when falling */
		else if(PlayerIsFalling()){
            /* While falling upwards, heavily shorten the legs */
            if(currentYVelocity >= 0) {
                currentLegLength = givenLegLength*0.1f + playerBodyLength/2f;
                currentStepHeight = givenStepHeight*0.1f;
            }
            /* While falling downwards, have the leg's length become relative to the players falling speed */
            else {
                currentLegLength = -currentYVelocity + playerBodyLength/2f;
                currentStepHeight = givenStepHeight*0.5f;
            }


            //Note: Add a new "fastfall", where the player falls much faster and their legs extend much longer
		}
    }

    public void UpdateInputVector() {
        /*
         * Take in user input to calculate a direction vector for the player to move towards. 
         */
         
        /* Use two input types for each axis to allow more control on player movement */
        inputVector = new Vector3((1-sliding)*inputs.playerMovementXRaw + sliding*inputs.playerMovementX,
                0, (1-sliding)*inputs.playerMovementYRaw + sliding*inputs.playerMovementY);

        /* Keep the movement's maginitude from going above 1 */
        if(inputVector.magnitude > 1) {
            inputVector.Normalize();
        }

        /* Add the player speed to the movement vector */
        if(Input.GetKey(KeyCode.LeftShift)) {
            inputVector *= movementSpeed*runSpeedMultiplier;
        }
        else {
            inputVector *= movementSpeed;
        }

        
        /* Keep the movement's magnitude from going bellow 0.01. This prevents passing through teleportweTriggers */
        if(inputVector.magnitude < 0.01f) {
            inputVector = Vector3.zero;
        }

        /* Rotate the input direction to match the player's view. Only use the view's rotation along the Y axis */
        inputVector = Quaternion.AngleAxis(-cameraXRotation, transform.up)*transform.rotation*inputVector;
    }
    
    public void StepPlayer() {
        /*
         * A step is when the player's body shifts along it's y axis to position the body relative to legLength.
		 * This will require the camera to remain unchanged to allow a smooth
		 * camera translation when handling bumpy terrain like stairs.  
         *
         * To determine if the player has taken a step down or up, compare the legLengths before 
         * and after moving the player. If their legLenths are different, then a step will be taken.
         * 
         * If not enough "legs" connect to an object, the player will not take
         * a step and instead will change states to "falling".
         */
        Vector3 upDirection = transform.rotation*Vector3.up;

        /* Update the legLengths of the player before making a step */
        UpdateLegLengths();
        
        /* Fire the leg rays to get their lengths for the player's current position */
        FireLegRays();
        
        /* Run sepperate functions depending on the state */
		if(state == (int) PlayerStates.Standing){
			StepPlayerStanding();
		} else if(PlayerIsFalling()){
			StepPlayerFalling();
		}
    }
    
    void StepPlayerStanding(){
    	/*
    	 * Check each leg's distance and whether they can reach down to an object. 
    	 * If enough legs are no longer grounded, attempt to jump and enter the falling state.
    	 */
        int requiredGroundedCount = 1;
            
    	/* Get how many legs are keeping the player "grounded" */
        int currentGroundedCount = 0;
        for(int i = 0; i < extraLegLenths.Length; i++) {
            if(extraLegLenths[i] >= 0) {
                currentGroundedCount++;
            }
        }
        
		/* Attempt to jump and change the state to "falling" if the player lost their footing */
        if(currentGroundedCount < requiredGroundedCount){
            JumpAttempt();
        	ChangeState((int) PlayerStates.Falling);
        }

		/* Update the footPosition and the player's position if they are still standing */
		else {
		
			/* Calculate the current foot position of the player by finding the new leg length */
            float newLegLength = 0;
            for(int i = 0; i < extraLegLenths.Length; i++) {
                if(extraLegLenths[i] >= 0) {
                    newLegLength += extraLegLenths[i]/currentGroundedCount;
                }
            }
            
			/* Use the new legLength to make the player undergo a "step" */
			DoStep(newLegLength);
		}
    }
    
    void StepPlayerFalling(){
        /*
         * While falling, check if enough legs can properly hit objects and ground the player.
         * If this occurs, get the new footPosition and snap the player to the floor 
         * using their normal standing leg length.
         * 
         * During the falling state, the player's leg lengths will be drastically changed.
		 */
        //note: as the player falls/ graviry increases, the footStep distance increases.
        //meaning the faster the player travels the further they are abke to snap to a floor



        int requiredGroundedCount = 1;

        /* Get how many legs are keeping the player "grounded" */
        int currentGroundedCount = 0;
        for(int i = 0; i < extraLegLenths.Length; i++) {
            if(extraLegLenths[i] >= 0) {
                currentGroundedCount++;
            }
        }

        /* If enough legs are grounded, the player will change to the standing state */
        if(currentGroundedCount >= requiredGroundedCount) {
            /* Now that the player has landed, reset them to their expected standing position */
            ChangeState((int) PlayerStates.Standing);
            UpdateLegLengths();


            /* Calculate the current foot position of the player by finding the new leg length */
            //AdjustBodyAfterStep/
            float newLegLength = 0;
            for(int i = 0; i < extraLegLenths.Length; i++) {
                if(extraLegLenths[i] >= 0) {
                    newLegLength += extraLegLenths[i]/currentGroundedCount;
                }
            }

            /* Use the new legLength to make the player undergo a "step" */
            DoStep(newLegLength);
        }
    }
    
    void DoStep(float stepLegLength){
        /*
    	 * Update the player's footingPosition along with the new
    	 * position for the body and the camera to complete a "step"
     	 */
        Vector3 upDirection = transform.rotation*Vector3.up;

        /* Place the footPosition using the stepLegLength */
        currentFootPosition = transform.position - upDirection*(stepLegLength);
    
    	/* Move the player's body so that their "legs" are now of proper length */
    	MovePlayer(-transform.position + currentFootPosition + upDirection*(currentLegLength));

        /* Revert any movement done to the camera to smooth the players view */
        //currentCameraTransform.transform.position -= upDirection*(currentLegLength - stepLegLength);
        cameraYOffset -= (currentLegLength - stepLegLength);
    }
    
    public void MovePlayer() {
        /*
         * Use player input to move the player along their relative X and Z axis. Gravity is activated if the player is 
         * in the proper state, in which the player will gain momentum in their relative negative Y axis.
         */

		/* Calculate the gravity vector that will be applied to the player */
        Vector3 gravityVector = GetGravityVector();
        
        /* Update the player's input vector to get the player's input direction */
        UpdateInputVector();

        /* Send a move command to the player using the gravity and input vectors */
        //MovePlayer(gravityVector + (inputVector)*Time.deltaTime*60);
        MovePlayer(gravityVector + inputVector);
    }
    
    Vector3 GetGravityVector(){
    	/*
    	 * Calculate the vector used to apply gravity to the player.
    	 * No gravity is applied to the player if they are standing.
    	 */
    	Vector3 gravityVector = Vector3.zero;
    
         /* If the player is falling, apply gravity to their yVelocity */
    	if(PlayerIsFalling()) {

            /* If the player is FastFalling, Increase the falling speed and maximum limit */
            if(state == (int) PlayerStates.FastFalling) {
                int fastFallMod = 10;
                currentYVelocity -= gravity*Time.deltaTime*60*fastFallMod/10f;
                if(currentYVelocity < -maxYVelocity*fastFallMod) { currentYVelocity = -maxYVelocity*fastFallMod; }
            }
            
            else {
                currentYVelocity -= gravity*Time.deltaTime*60;
                if(currentYVelocity < -maxYVelocity) { currentYVelocity = -maxYVelocity; }
            }
            gravityVector = currentYVelocity*transform.up;
        }

        /* Reset the player's yVelocity if they are grounded */
        else if(state == (int) PlayerStates.Standing) {
            currentYVelocity = 0;
        }
    
    	return gravityVector;
    }
    
    void AdjustCameraPosition() {
        /*
         * Position and rotate the player camera according to their inputs.
         * The camera is positionned headHeight above the player origin,
         * with the cameraYOffset applying an offset when needed.
         * 
         * The camera's position and rotation is calculated by firing a RayTrace command from the player origin
         * upwards (relative to the player) to the expected view position. The RayTrace will collide with walls
         * and will teleport from triggers, letting the player's "head" pass through portals without their "body".
         */
        Quaternion toCamRotation;
        Quaternion rotationDifference;
        Vector3 playerOrigin;
        float playerCameraHeight;

        /* Copy the player's transform to the camera as we get it's facing direction */
        currentCameraTransform.position = transform.position;
        currentCameraTransform.rotation = transform.rotation;
        RotateCamera();
		
		//maybe adjust the yoffset at this point. ie use the morph percentage
        /* Apply a rayTrace from the player's origin to the camera's position that is effected by teleport triggers */
        playerOrigin = transform.position;
        toCamRotation = Quaternion.LookRotation(transform.up, transform.forward);
        playerCameraHeight = GetCameraOffset();
        rotationDifference = RayTrace(ref playerOrigin, ref toCamRotation, ref playerCameraHeight, true, true);

        /* Use the new position and rotation to find the camera's final position and rotation */
        currentCameraTransform.position = playerOrigin;
        currentCameraTransform.rotation = rotationDifference*currentCameraTransform.rotation;
        playerCamera.transform.position = currentCameraTransform.position;
        playerCamera.transform.rotation = currentCameraTransform.rotation;

        //Draw lines that represent the camera's final rotation
        //Debug.DrawRay(playerCamera.transform.position, currentCameraTransform.up, Color.cyan);
        //Debug.DrawRay(playerCamera.transform.position, currentCameraTransform.forward, Color.cyan);
    }

	float GetCameraOffset(){
        /*
		 * Get the height offset the player camera is from the player origin.
         * Re-adjust the cameraYOffset after using it and prevent it from being too large.
		 */
        float headOffset;

        /* If the player is falling, adjust the cameraYOffset to reflect it's falling velocity */
        if(PlayerIsFalling()) {
            cameraYOffset += currentYVelocity/5f;
        }


        /* Prevent the offset from becoming larger than half the player's body length */
        if(cameraYOffset > playerBodyLength/2f){
            cameraYOffset = playerBodyLength/2f;
		}else if(cameraYOffset < -playerBodyLength/2f){
            cameraYOffset  = -playerBodyLength/2f;
		}

        /* If the offset is very small, snap it to 0 */
        if(cameraYOffset < 0.001 && cameraYOffset > -0.001) {
            cameraYOffset = 0;
        }

        /* Use the cameraYOffset to get the head's offset */
        headOffset = headHeight + cameraYOffset;

        /* Reduce the cameraYOffset once it gets used */
        cameraYOffset *= morphPercentage;

        return headOffset;
	} 
	

    /* ----------- Event Functions ------------------------------------------------------------- */
    
    public void ApplyFastfall(){
    	/* 
    	 * Put the player into the fast fall state if possible
    	 */
    
    	/* Only go into fast fall if the state is already in free fall */
    	if(PlayerIsFalling() && state != (int) PlayerStates.FastFalling){
    		ChangeState((int) PlayerStates.FastFalling);
    	}
    }
    
    
    
    void FireLegRays() {
    	/*
    	 * Use the player's current position and their current leg length
    	 * to fire off a ray for each "leg" in the leg array,tracking how long they reach.
    	 */
        Vector3 upDirection = transform.rotation*Vector3.up;
        Vector3 forwardVector = transform.rotation*Vector3.forward;
        Vector3 tempForwardVector = Vector3.zero;

        /* Test the collision for the first leg, protruding downward from the player's center */
        LegCollisionTest(transform.position, -upDirection, currentLegLength + currentStepHeight, 0);

        /* Test the collision for each leg that forms a circle around the player using legGap*playerBodyRadius as a radius */
        for(int i = 1; i < extraLegLenths.Length; i++) {
            tempForwardVector = Quaternion.AngleAxis(i*(360/(extraLegLenths.Length-1)), upDirection)*forwardVector;

            /* Fire a ray from the players center to the leg's starting point to ensure nothing is blocking the leg */
            Debug.DrawLine(transform.position, transform.position + tempForwardVector*(legGap*playerBodyRadius));
            if(Physics.Raycast(transform.position, tempForwardVector, legGap*playerBodyRadius)) {
                /* If we cant reach the leg from the player's center, do not use the leg in finding the footPosition */
                extraLegLenths[i] = -1;
            }else {    
                /* Fire a ray from the leg's starting point to it's end point, updating the leg array with the distance it reached */
                LegCollisionTest(transform.position + tempForwardVector*legGap*playerBodyRadius, -upDirection, currentLegLength + currentStepHeight, i);
            }
        }
    }

    void ChangeState(int newState) {
        /*
         * Change the player's current state to the given newState. Run certain lines if
         * certain states change into other specific states (fast falling > standing)
         */

        if(state == (int) PlayerStates.FastFalling && newState == (int) PlayerStates.Standing) {
            Debug.Log("HARD FALL");
            cameraYOffset = -10;
        }

        state = newState;
    }

    void LegCollisionTest(Vector3 position, Vector3 direction, float length, int index) {
        /*
         * Use the given values to send a ray trace of the player's leg and return the distance of the ray.
         * Update the arrays that track the status of the leg with the given index. If the given
         * index is -1, then do not update the array
         */
        RaycastHit hitInfo = new RaycastHit();
        Ray bodyToFeet = new Ray(position, direction);

        if(Physics.Raycast(bodyToFeet, out hitInfo, length)) {
            extraLegLenths[index] = hitInfo.distance;

            /* Draw the point for reference */
            Debug.DrawLine(position, position + direction*(length), Color.green);
        }
        else {
            /* Draw the point for reference */
            Debug.DrawLine(position, position + direction*(length), Color.red);
            extraLegLenths[index] = -1;
        }
    }

    void JumpAttempt() {
        /*
    	 * Try to make the player jump. A jump must be primed (jumpPrimed == true) for the player to jump.
    	 */

        if(jumpPrimed == true && state == (int) PlayerStates.Standing) {
            jumpPrimed = false;
            ChangeState((int) PlayerStates.Falling);
            currentYVelocity = jumpSpeed;
        }
    }


    void MovePlayer(Vector3 movementVector) {
        /*
         * Move the player by the given movementVector. To move the player, run a rayTrace command using the
         * given movementVector as a direction and distance. The position used will be the player's origin,
         * i.e. the transform this script is attached to. This means for the player to be teleported, their
         * origin point must collide with a teleporterTrigger when using a rayTrace command.
         */
        Quaternion rotationDifference;

        if(movementVector.magnitude != 0) {
            /* Set values to be used with the rayTrace call */
            Vector3 position = transform.position;
            Quaternion direction = Quaternion.LookRotation(movementVector.normalized, transform.up);
            float remainingDistance = movementVector.magnitude;

            /* Fire the rayTrace command and retrive the rotation difference */
            rotationDifference = RayTrace(ref position, ref direction, ref remainingDistance, true, true);

            /* Update the player's transform with the updated parameters */
            transform.position = position;
            transform.rotation = transform.rotation * rotationDifference;
        }
    }

    
    /* ----------- Helper Functions ------------------------------------------------------------- */
    
    Quaternion RayTrace(ref Vector3 position, ref Quaternion rotation,
        ref float distance, bool detectTeleportTriggers, bool detectOtherColliders) {
        /*
         * Fire a ray from the given position with the given rotation forwards for the given distance.
         * The quaternion returned represents the amount of rotation that the given rotation underwent.
         * 
         * detectTeleportTriggers as true will cause any collision with a teleportTrigger to teleport 
         * the position and rotation to the hit trigger's partner using it's teleportParameters function.
         * teleportTrigger Collisions with detectTeleportTriggers as false will be ignored.
         * 
         * detectOtherColliders as true will cause any collision with any other trigger to cause
         * the position to stop at the point of collision and reduce the distance amount respectively.
         * these type of collisions will be ignored if detectOtherColliders is false.
         */
        Quaternion totalRotation = Quaternion.identity;
        Quaternion rotationDifference;
        RaycastHit hitInfo = new RaycastHit();
        bool stopRayTrace = false;
        LayerMask rayLayerMask = 0;

        /* Include teleport triggers into the layerMask */
        if(detectTeleportTriggers) {rayLayerMask = rayLayerMask | (1 << LayerMask.NameToLayer("Portal Trigger"));}
        /* Include all non-teleporter triggers colliders into the layerMask */
        if(detectOtherColliders) {rayLayerMask = rayLayerMask | ~(1 << LayerMask.NameToLayer("Portal Trigger"));}
        
        /* Travel towards the rotation's forward for the remaining distance */
        while(distance > 0 && stopRayTrace == false) {
            //reduce the distance every loop to prevent infinite loops
            //distance -= 0.001f;

            /* Check for any collisions from the current position towards the current direction */
            if(Physics.Raycast(position, rotation*Vector3.forward, out hitInfo, distance, rayLayerMask)) {
                /* When hitting a collider, move the position up to the collision point */
                Debug.DrawLine(position, rotation*Vector3.forward*hitInfo.distance, Color.white);
                position += rotation * Vector3.forward * hitInfo.distance;
                distance -= hitInfo.distance;
                
                /* Hitting a teleport trigger will teleport the current position and direction to it's partner trigger */
                if(hitInfo.collider.GetComponent<TeleporterTrigger>() != null) {
                    /* Teleport the parameters and apply the rotation that was underwent to the totalRotation quaternion */
                    rotationDifference = hitInfo.collider.GetComponent<TeleporterTrigger>().TeleportParameters(ref position, ref rotation);
                    totalRotation = rotationDifference*totalRotation;
                    //Prevent the rayTrace from hitting another trigger after teleporting
                    rayLayerMask = 0;
                    //Debug.Log("hit tele");
                }

                /* Hitting a solid collider will stop the rayTrace where it currently is */
                else if(!hitInfo.collider.isTrigger) {
                    stopRayTrace = true;
                    //Debug.Log("hit wall");
                }

                /* non-teleport triggers will be ignored */
                else if(hitInfo.collider.isTrigger) {
                    //Debug.Log("hit something?");
                }
            }

            /* The raytrace hit nothing, so travel along the direction for the remaining distance */
            else {
                Debug.DrawLine(position, position + rotation*Vector3.forward*distance);
                position += rotation*Vector3.forward*distance;
                distance = 0;
            }
        }

        return totalRotation;
    }
    
    
    
    bool PlayerIsStanding(){
    	/*
    	 * Return true if the player is in a grounded state
    	 * (standing)
    	 */
    	bool isGrounded = false;
    
    	if(state == (int) PlayerStates.Standing){
    		isGrounded = true;
    	}
    
    	return isGrounded;
    }
    
    bool PlayerIsFalling(){
    	/*
    	 * Return true if the player is in a freefall state
    	 * (falling, fastFalling)
    	 */
    	bool isFalling = false;
    
    	if(state == (int) PlayerStates.Falling || state == (int) PlayerStates.FastFalling){
    		isFalling = true;
    	}
    
    	return isFalling;
    }
}
