using UnityEngine;
using System.Collections;

/* 
 * The potential states the player can be in.
 * 
 * standing: Legs are connecting the player to the ground, 
 * Effenctivly removing any effect a gravity vector could have.
 * 
 * landing: The player went from the fast falling state to a grounded state 
 * and must undergo an animation before they can regain control.
 * 
 * falling: Legs are looking for an object to connect to
 * As the player is being pulled down by gravity.
 * 
 * fastfalling: Similar to falling, but has an increased falling
 * speed and terminal velocity. 
 */
public enum PlayerStates{
    Standing,
    Landing,
	Falling,
    FastFalling
};


/*
 * A custom character controller that uses UserInputs to handle movement. It uses "legs" to keep
 * it's "body" above the floor, letting the player walk up and down stairs or slopes smoothly. 
 */
public class CustomPlayerController : MonoBehaviour {
    public int state;
    /* How long the player has spent in the current state */
    public float stateTime;

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
    private float currentYVelocity;
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


    /* --- Camera Positioning ---------------------- */
    /* Current rotations of the camera */
    private float cameraXRotation;
    private float cameraYRotation;
    /* How high the player camera is from their body's origin */
    public float headHeight;
    /* An offset that differentiates currentCameraTransform from the expected head height */
    public float cameraYOffset;
    /* How fast cameraYOffset morphs towards 0 each frame, in percentage. */
    [Range(1, 0)]
    public float morphPercentage;
    
    public int cameraEffectsEnabled = -1;
    

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

        /* Start the player in the standing state so they can link themselves to the floor */
        state = -1;
        ChangeState((int) PlayerStates.Standing);
   }

    void Update() {
        /*
         * Handle any player inputs. If they need to be redirected to a new script,
         * send the input signals to the current overriddenScript.
         */
        inputs.UpdateInputs();
        stateTime += Time.deltaTime;
        
        /* Apply any needed effects to the camera */
        UpdateCameraEffects();

        /* Run a given set of functions depending on the player state */
        if(state == (int) PlayerStates.Standing){
			UpdateStanding();
		}

        else if(state == (int) PlayerStates.Landing) {
            UpdateLanding();
        }

        else if(state == (int) PlayerStates.Falling) {
            UpdateFalling();
        }
        
        else if(state == (int) PlayerStates.FastFalling) {
        	UpdateFalling();
        }


        
        //Draw a line in the camera's forward vector
        Debug.DrawLine(playerCamera.transform.position, playerCamera.transform.position + playerCamera.transform.rotation*Vector3.forward*0.5f, Color.green);
    }


    /* ----------------- Main Update Functions ------------------------------------------------------------- */

    void UpdateStanding() {
        /*
         * Handle the inputs of the user and the movement of the player
         * when they are standing on an object.
         */
         
        /* Move the player using the input vector and gravity */
        MovePlayer();

        /* Update the player's jumping conditions, letting the player prime their jump */
        UpdateJumpingValues();

        /* Find the footPosition of the player and check if they are falling or standing. Place the player's body position. */
        StepPlayer();

        /* Adjust the camera's default transform (currentCameraTransform) now that the player has moved */
        AdjustCameraRotation();
        AdjustCameraPosition(GetCameraHeight());
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
        AdjustCameraRotation();
        AdjustCameraPosition(GetCameraHeight());
    }
    
    void UpdateLanding() {
        /*
         * Handle how the player will react when recovering from a fast fall.
         * Prevent the player from moving and force the camera into an animation.
         * Do not let them input any movement or prime a jump.
         */

        /* Move the player using the input vector and gravity */
        MovePlayer();

        /* Let the player undergo any steps that may occur */
        StepPlayer();

        /* Force the camera to follow a specific path/animation, but still respond to mouse inputs */
        AdjustCameraRotation();
        AnimatedCameraLanding();
    }

    
    /* ----------------- Secondairy Update Functions ------------------------------------------------------------- */

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
        if(PlayerIsGrounded()) {
            StepPlayerStanding();
        }
        else if(PlayerIsAirborn()) {
            StepPlayerFalling();
        }
        else {
            Debug.Log("Warning: state " + state + " does not handle player stepping");
        }
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
    
    void AdjustCameraRotation() {
        /*
         * Take in user inputs to properly rotate the currentCameraTransform's facing direction.
         */

        /* Copy the player's current rotation before rotating the camera */
        currentCameraTransform.rotation = transform.rotation;

        /* Ensure the X rotation does not overflow */
        cameraXRotation -= inputs.mouseX;
        if(cameraXRotation < 0) { cameraXRotation += 360; }
        else if(cameraXRotation > 360) { cameraXRotation -= 360; }

        /* Prevent the Y rotation from rotating too high or low */
        cameraYRotation += inputs.mouseY;
        cameraYRotation = Mathf.Clamp(cameraYRotation, -75, 75);

        /* Apply the rotations to the camera's default transform */
        currentCameraTransform.rotation *= Quaternion.Euler(-cameraYRotation, -cameraXRotation, 0);
        playerCamera.transform.rotation = currentCameraTransform.rotation;
    }

    void AdjustCameraPosition(float cameraHeight) {
        /*
         * Position and rotate the player camera according to the player's inputs.
         * The camera is positionned headHeight above the player origin,
         * with the cameraYOffset applying an offset.
         * 
         * Properly position the camera where the player's "head" should be. The new position and rotation 
         * is calculated by firing a RayTrace command from the player origin upwards (relative to the player) 
         * to the expected view position. The RayTrace will collide with walls and will teleport from triggers, 
         * letting the player's "head" pass through portals without their "body".
         * 
         * The cameraHeight value should be "GetCameraHeight()" as default 
         * unless a function wants to set the camera's offset on it's own.
         */
        Quaternion toCamRotation;
        Quaternion rotationDifference;
        Vector3 cameraPosition;

        /* Place the camera onto the player origin before firing the rayTrace */
        currentCameraTransform.position = transform.position;
        
        /* Prepare the values used with the rayTrace */
        cameraPosition = transform.position;
        toCamRotation = Quaternion.LookRotation(transform.up, transform.forward);

        /* If the distance is negative, have the camera's rotation go down instead of up */
        //WE DONT KNOW IF THIS WILL PROPERLY TELEPORT.NEED TO TEST WITH THE PLAYER STEPPING THROUGH A TILTED TELEPORTER 
        if(cameraHeight < 0) {
            cameraHeight *= -1;
            toCamRotation = Quaternion.LookRotation(-transform.up, transform.forward);
        }

        /* RayTrace from the player's origin to the camera's position */
        rotationDifference = RayTrace(ref cameraPosition, ref toCamRotation, ref cameraHeight, true, true);

        /* Use the new position and rotation to find the camera's final position and rotation */
        currentCameraTransform.position = cameraPosition;
        currentCameraTransform.rotation = rotationDifference*currentCameraTransform.rotation;
        playerCamera.transform.position = currentCameraTransform.position;
        playerCamera.transform.rotation = currentCameraTransform.rotation;
    }
    
    void AnimatedCameraLanding() {
        /*
         * Apply an offset to the camera by setting it to a specific position.
         * 
         * The animation that the camera undergoes can be defined by these stages, sepperated by stateTime:
         * state1: Animate the camera from the player's headHeight to their body base
         * state2: Move the camera upwards
         * Once the final state ends, change the state back to standing.
         */
        float xRot = 0;
        float yRot = 0;
        float cameraHeightOffset = 0;
        float posState1 = 0.05f;
        float posState2 = 0.75f;
        float rotState1 = 0.15f;
        float rotState2 = 0.65f;
        float angleRotation = 10;

        //Have it tilt down for 75% then tilt back up
        /* Set the camera's y rotation values to follow the sine graph [0, PI]> */
        xRot = cameraXRotation;
		if(stateTime < rotState1) {
			yRot = cameraYRotation - angleRotation*Mathf.Sin(0 + (Mathf.PI/2f)*RatioWithinRange(0, rotState1, stateTime));
		}else if (stateTime < rotState2) {
			yRot = cameraYRotation - angleRotation*Mathf.Sin((Mathf.PI/2f) + (Mathf.PI/2f)*RatioWithinRange(rotState1, rotState2, stateTime));
		}else {
            yRot = cameraYRotation;
        }

        

        /* Set the camera's positional values depending on the time spent in the current state */
        if(stateTime < posState1) {
            cameraHeightOffset -= (headHeight + playerBodyLength/2f) * Mathf.Sin((Mathf.PI/2f)*RatioWithinRange(0, posState1, stateTime));
        } else if(stateTime < posState2) {
            cameraHeightOffset -= (headHeight + playerBodyLength/2f) * (Mathf.Cos(Mathf.PI*RatioWithinRange(posState1, posState2, stateTime))+1)/2f;
        }

        /* Set the values to their expected value before entering the standing state */
        else {
            cameraHeightOffset = 0;
            ChangeState((int) PlayerStates.Standing);
        }

        /* Set the rotation of the camera on it's own */
        currentCameraTransform.rotation = transform.rotation;
        currentCameraTransform.rotation *= Quaternion.Euler(-yRot, -xRot, 0);

        /* Set the position of the camera using it's own process */
        AdjustCameraPosition(GetCameraHeight() + cameraHeightOffset);
    }

    /* ----------------- Step Functions ------------------------------------------------------------- */

    void StepPlayerStanding() {
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

        /* Change to the "falling" state and attempt to jump if the player lost their footing */
        if(currentGroundedCount < requiredGroundedCount) {
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

    void StepPlayerFalling() {
        /*
         * While in the general state of airborn, check if enough legs can properly hit objects and ground the player.
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
    
    void DoStep(float stepLegLength) {
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
    

    /* ----------------- Value Updating Functions ------------------------------------------------------------- */
    
    void UpdateJumpingValues() {
        /*
         * Prime a jump if the jump key was recently pressed. Also check if the player
         * is attempting a jump by looking for the release of the jump key.
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
		
		/* Keep the leg lengths their default size when grounded */
		if(PlayerIsGrounded()){
			currentLegLength = givenLegLength + playerBodyLength/2f;
            currentStepHeight = givenStepHeight;
		}
		
		/* Shorten the legs when airborn */
		else if(PlayerIsAirborn()){
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


    /* ----------- Event Functions ------------------------------------------------------------- */

    void ChangeState(int newState) {
        /*
         * Change the player's current state to the given newState. Run certain lines if
         * certain states change into other specific states (fast falling > standing)
         */

        /* Dont change anything if the player is already in the new state */
        if(state != newState) {

            /* FastFalling > Standing : A "hard fall" occurs and forces the player into a landing animation. */
            if(state == (int) PlayerStates.FastFalling && newState == (int) PlayerStates.Standing) {
                Debug.Log("HARD FALL");
                newState = (int) PlayerStates.Landing;
                cameraYOffset = 0;
            }
            
            /* Changing to the fastfalling state will start camera effects */
            if(newState == (int) PlayerStates.FastFalling){
            	StartCameraEffects();
            }
            /* Leaving the fastfalling state will stop camera effects */
            if(state == (int) PlayerStates.FastFalling){
            	
            }


            stateTime = 0;
            state = newState;
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

    void JumpAttempt() {
        /*
    	 * Try to make the player jump. A jump must be primed (jumpPrimed == true) for the player to jump.
    	 */

        if(jumpPrimed == true && PlayerIsGrounded()) {
            jumpPrimed = false;
            ChangeState((int) PlayerStates.Falling);
            currentYVelocity = jumpSpeed;
        }
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
            }
            else {
                /* Fire a ray from the leg's starting point to it's end point, updating the leg array with the distance it reached */
                LegCollisionTest(transform.position + tempForwardVector*legGap*playerBodyRadius, -upDirection, currentLegLength + currentStepHeight, i);
            }
        }
    }

    Vector3 GetGravityVector() {
        /*
    	 * Calculate the vector used to apply gravity to the player.
    	 * No gravity is applied to the player if they are standing.
    	 */
        Vector3 gravityVector = Vector3.zero;

        /* If the player is falling, apply gravity to their yVelocity */
        if(PlayerIsAirborn()) {

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
        else if(PlayerIsGrounded()) {
            currentYVelocity = 0;
        }

        return gravityVector;
    }

    float GetCameraHeight() {
        /*
		 * Get the height offset the player camera is from the player origin.
         * Re-adjust the cameraYOffset after using it and prevent it from being too large.
		 */
        float headOffset;

        /* If the player is falling, adjust the cameraYOffset to reflect it's falling velocity */
        if(PlayerIsAirborn()) {
            cameraYOffset += currentYVelocity/5f;
        }


        /* Prevent the offset from becoming larger than half the player's body length */
        if(cameraYOffset > playerBodyLength/2f) {
            cameraYOffset = playerBodyLength/2f;
        }
        else if(cameraYOffset < -playerBodyLength/2f) {
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


    /* ----------- Outside Called Functions ------------------------------------------------------------- */

    public void ApplyFastfall() {
        /* 
    	 * Put the player into the fast fall state if they are currently airborn
    	 */

        /* Do not go into fastfall if the player is not currently falling or already fastfalling */
        if(PlayerIsAirborn() && state != (int) PlayerStates.FastFalling) {
            ChangeState((int) PlayerStates.FastFalling);
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
                Debug.DrawLine(position, position + rotation*Vector3.forward*hitInfo.distance, Color.red);
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
    
    bool PlayerIsGrounded(){
    	/*
    	 * Return true if the player is in a grounded state
    	 * (standing, landing)
    	 */
    	bool isGrounded = false;
    
    	if(state == (int) PlayerStates.Standing || state == (int) PlayerStates.Landing) {
    		isGrounded = true;
    	}
    
    	return isGrounded;
    }
    
    bool PlayerIsAirborn(){
    	/*
    	 * Return true if the player is in a freefall state/legs do not connect
    	 * (falling, fastFalling)
    	 */
    	bool isFalling = false;
    
    	if(state == (int) PlayerStates.Falling || state == (int) PlayerStates.FastFalling){
    		isFalling = true;
    	}
    
    	return isFalling;
    }

    float RatioWithinRange(float min, float max, float value) {
        /*
         * Return the ratio of the value between min and max. Returns 0 if
         * value is equal to or less than min, 1 if value is more or equal to max.
         * 0.5 if it is equally between both min and max.
         */
        float ratio;

        if(value < min) {
            ratio = 0;
        }else if(value > max) {
            ratio = 1;
        }else {
            ratio = (value - min)/(max - min);
        }

        return ratio;
    }
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    void StartCameraEffects(){
    	/*
    	 * Enable the value to tell the camera to add effects
    	 */
    
    	cameraEffectsEnabled = 0;
    }
    
    void StopCameraEffects(){
    	/*
    	 * Disable the camera effects value to stop any camera effects
    	 */
    
    	cameraEffectsEnabled = -1;
    }
    
    void UpdateCameraEffects(){
    	/*
    	 * Add the effects to the camera every frame
    	 */
    
    	/* Add camera effects for fast falling */
    	if(cameraEffectsEnabled == 0){
    
    	}
    }
}
