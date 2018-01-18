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
 * 
 * Every movement request is tallied up and reset at the start of a FixedUpdate.
 * Also, on every Update and FixedUpdate call, the script will check between their current
 * and last position for any teleporters. If they encounter a teleporter trigger, they will
 * teleport the player to their appropriate location by simply using a ray to represent
 * their previous movement and allow the ray to interact with a teleport trigger.
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
    /* The amount of distance travelled since the last step tracker update */
    private Vector3 lastStepMovement;
    /* The position the player was in on the previous update frame */
    private Vector3 lastSavedPosition;
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
    /* The velocity modifier when the player is FastFalling */
    public int fastFallMod;
    
    /* How fast a player travels upward when they jump */
    public float jumpSpeed;
    /* Used to determine the state of the jump. If true, the next jump opportunity will cause the player to jump. */
    private bool jumpPrimed;
    /* The state of the jump key on the current and previous frame. true = pressed */
    private bool jumpKeyPrevious = false;
    private bool jumpKeyCurrent = false;

    /* The Expected movements (in the form of Vector3) of the player for the upcomming physics update */
    private ArrayList expectedMovements;


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
    public float[] extraLegLenths;
    
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

	
	/* --- Player Scripts --------------------------- */
    /* Contains all the post processing effects that will be applied to the camera */
    public CustomPlayerCameraEffects cameraEffectsScript;

	/* Handles all sounds produced by the player */
	public PlayerSounds playerSoundsScript;

    /* Handles all footstep tracking */
    public FootstepTracker playerStepTracker;

    /* The last attached room the player was in. Must be set in the editor before starting. */
    public AttachedRoom lastRoom;


    /* -------------- Built-in Unity Functions ---------------------------------------------------------- */

    void Start() {
        /*
         * Initilize required objects and set starting values for certain variables 
         */

        /* Link the player's step tracker to their sound script */
        playerStepTracker.SetSoundsScript(playerSoundsScript);

        /* Set up the footstep tracker */
        playerStepTracker.CalculateStrideDistances(movementSpeed, givenLegLength);

        /* Set up the camera's post-processing effects */
        cameraEffectsScript.SetupPostProcessingEffects(playerCamera, this);

        /* Create the UserInputs object linked to this player */
        inputs = new UserInputs();

        /* Initilize the player's leg lengths */
        extraLegLenths = new float[extraLegs + 1];

        /* Adjust the player's height and width */
        GetComponent<CapsuleCollider>().height = playerBodyLength;
        GetComponent<CapsuleCollider>().radius = playerBodyRadius;
        
        /* Reset the player's positional values and camera effects */
        ResetPlayer();
    }
    
    void FixedUpdate() {
        /*
         * Handle player movement. This includes moving the player in the inputted direction and stepping.
         * 
         * Also, handle a teleport check if an Update call was not made between the the last physics check.
         * This could be due to the high amount of physics checks or possibly a drop in framerate for the player.
         * The goal is to prevent the player from moving past if they are not updating fast enough.
         */
         
        /* Handle the conditions that need to be checked after the player moves (teleport, update footstep tracker) */
        HandlePlayerMovement(true);
        
        /* Update the step tracker with whatever steps were made since the last FixedUpdate call */
        if(PlayerIsGrounded()) {
            playerStepTracker.AddHorizontalStep(Quaternion.Inverse(transform.rotation)*lastStepMovement);
            lastStepMovement = Vector3.zero;
        }
        
        /* Empty the expectedMovements array as we are about to add new movements */
        expectedMovements.Clear();

        /* From the player's current position, execute a step check to see if they need to move along their Y axis */
        StepPlayer();
        
        /* Move the player using their given input and the gravity vector */
        MovePlayer(inputVector + GetGravityVector());


        /* Apply the final tallied movement vector to the player's position */
        Rigidbody rigidBody = GetComponent<Rigidbody>();
        Vector3 newPosition = transform.position;
        for(int i = 0; i < expectedMovements.Count; i++) {
            newPosition += (Vector3) expectedMovements[i];
        }
        rigidBody.MovePosition(newPosition);
        /* Freeze the player's rigidbody's velocity */
        rigidBody.velocity = Vector3.zero;


        /* Save the player's current position as the lastSavedPosition */
        lastSavedPosition = transform.position;
    }

    void Update() {
        /*
         * Update the player's inputs and handle most input conditions. Player movement is handled in fixedUpdate.
         * Jumping and steppping are handled in this function. 
         * 
         * This may bring up a situation where if the game fails to update a frame after a long enough time, 
         * the player will end up moving across a gap as falling/stepping is handled in this update function.
         * If we were to move the falling into the FixedUpdate function, the player may fall/move past a portal
         * and not teleport. 
         * THE FIX: after every physics update, save the player's position. Once this Update function is called,
         * handle each movement sepratly. The written example shows this situation.
         * 
         * ALSO: WE CAN ADD A TELEPORT HANDLER IN THE FIXEDUPDATE FUNCTION. If we end up moving past a teleport trigger,
         * just teleport the player normally, then maybe reset the saved position values? The idea of this is that
         * after a physics update, we detected a teleport SHOULD have occured, but we did not update the frame yet,
         * so by teleporting the player in that moment they will NOT render a frame of them PAST the teleport trigger.
         */
         
        /* Update the player's inputs and stateTime */
        inputs.UpdateInputs();
        UpdateInputVector();
        stateTime += Time.deltaTime;

        /* Handle the conditions that need to be checked after the player moves (teleport, update footstep tracker) */
        HandlePlayerMovement(false);

        /* Check the player's inputs to see if they prime a jump */
        PrimeJumpingValue();

        /* Check if the player wants to reset their position */
        if(inputs.rKeyPressed) {
            ResetPlayer();
        }

        /* Update the player's stride progress to determine when a footstep sound effect should play */
        playerStepTracker.UpdateStride();
        
        //Draw a line in the camera's forward vector
        Debug.DrawLine(playerCamera.transform.position, 
                playerCamera.transform.position + playerCamera.transform.rotation*Vector3.forward*0.5f, Color.green);
        
    }

    void LateUpdate() {
        /*
         * Handle all camera effects after the player has finished moving/teleporting for the frame.
         * Depending on the player's state, apply different effects to the camera.
         * 
         * --------
         * Currently, a camera can undergo an animation (fastFalling, landing) and only one animation.
         * A common scenario of undergoing a landing followed by entering the falling state again will
         * seem jaring due to the camera moving suddently to it's neutral position.
         * 
         * This can be avoided by having each animation undergo it's animation process assuming the
         * camera has already been setup in it's default position AND by having each animation animate
         * independent of the player's state. Each animation should have a "Stop" and "Start" command.
         * --------
         */


        /* Use player inputs to rotate the player's view */
        if(state == (int) PlayerStates.Standing ||
                state == (int) PlayerStates.Landing ||
                state == (int) PlayerStates.Falling ||
                state == (int) PlayerStates.FastFalling) {
            AdjustCameraRotation();
        }


        /* Reposition the camera into it's expected position. Goes through portals. */
        if(state == (int) PlayerStates.Landing) {
            /* Apply an animation to the camera while in the landing state */
            AnimatedCameraLanding();
        }
        else if(state == (int) PlayerStates.FastFalling) {
            /* Apply an animation to the camera while in the fastFalling state */
            AnimateCameraFastFalling();
        }
        else {
            /* Any other state simply places the camera into it's default position */
            AdjustCameraPosition(GetCameraHeight());
        }

        
        /* Apply any needed special effects to the camera. Runs everytime the camera renders */
        cameraEffectsScript.UpdateCameraEffects();
    }

    
    /* ----------------- Main Movement Function ------------------------------------------------------------- */

    void HandlePlayerMovement(bool fixedUpdate) {
        /*
         * Given the player's current position and their last known saved position, cast a vector between 
         * the two points and use it as a movement vecotr. This means teleport the player if it
         * collides with a teleporter and update the footstep tracker with the vector.
         * 
         * We only add to the step tracker if there was movement because this function can run multiple times 
         * between physics updates and frame updates, meaning immobile steps could accidentally be sent.
         * 
         * The given update boolean indicates what function made the call: true for FixedUpdate, false for Update.
         */

        /* Get the vector of the player's movement between now and the last time they were checked */
        Vector3 movementVector = transform.position - lastSavedPosition;

        /* Check if there was any movement at all */
        if(movementVector.magnitude != 0) {

            /* Fire a ray of the player's movement that interracts with the world, including teleporters */
            Vector3 position = lastSavedPosition;
            Quaternion direction = Quaternion.LookRotation(movementVector.normalized, transform.up);
            float remainingDistance = movementVector.magnitude;
            bool teleported = false;
            Quaternion rotationDifference = RayTrace(ref position, ref direction, ref remainingDistance, ref teleported, true, true);

            /* If the player's movement passes through a teleporter, reposition their transform to reflect the teleport */
            if(teleported) {
                transform.position = position;
                transform.rotation = transform.rotation * rotationDifference;
            }
        }

        /* When grounded, add any movement to the stepTracker vector */
        if(PlayerIsGrounded()) {
            lastStepMovement += movementVector;
        }

        /* When jumping, if the player is pushed downward (like hits a ceiling), remove all jump velocity */
        else {
            if(currentYVelocity > 0 && movementVector.y < 0) {
                currentYVelocity = 0;
            }
        }

        /* Update the player's lastSavedPosition with it's new position */
        lastSavedPosition = transform.position;
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
        if(PlayerIsGrounded()) {
            StepPlayerGrounded();
        }
        else if(PlayerIsAirborn()) {
            StepPlayerAirborn();
        }
        else {
            Debug.Log("Warning: state " + state + " does not handle player stepping");
        }
    }
    
    void MovePlayer(Vector3 movementVector) {
        /*
         * Add the given vector to the player's expectedMovements, which will be handled every FixedUpdate.
         * Do not add the movement if it has a magnitude of 0.
         */

        if(movementVector.magnitude != 0) {
            expectedMovements.Add(movementVector);
        }
    }


    /* ----------------- Step Functions ------------------------------------------------------------- */

    void StepPlayerGrounded() {
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

            /////Since the player is grounded,
            /* Taking a vertical step will add the depth of the step to the step tracker */
            if(PlayerIsGrounded()) {
                playerStepTracker.AddVerticalStep((currentLegLength - newLegLength));
            }

            /* Use the new legLength to make the player undergo a "step" */
            DoStep(newLegLength);
        }
    }

    void StepPlayerAirborn() {
        /*
         * While in the general state of airborn, check if enough legs can properly hit objects and ground the player.
         * If this occurs, get the new footPosition and snap the player to the floor 
         * using their normal standing leg length.
         * 
         * During the falling state, the player's leg lengths will be drastically changed.
		 */
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
         * 
         * I think this is like this:
         * The player is always trying to go back to their currentLegLength. The given value
         * is how long their legs are currently. Therefore, the goal of this function
         * is to find the differnce in the leg lenths, and move the player so that they go from
         * stepLegLength to current
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
    

    /* ----------------- Camera Update Functions ------------------------------------------------------------- */

    void AdjustCameraRotation() {
        /*
         * Take in user inputs to properly rotate the currentCameraTransform's facing direction.
         */

        /* Copy the player's current rotation before rotating the camera */
        currentCameraTransform.rotation = transform.rotation;

        /* Add the X input rotation and ensure it does not overflow */
        cameraXRotation -= inputs.mouseX;
        if(cameraXRotation < 0) { cameraXRotation += 360; }
        else if(cameraXRotation > 360) { cameraXRotation -= 360; }

        /* Add the Y input rotation and ensure it does not overflow */
        cameraYRotation += inputs.mouseY;
        cameraYRotation = Mathf.Clamp(cameraYRotation, -75, 75);

        /* Apply the rotations to the camera's default transform */
        currentCameraTransform.rotation *= Quaternion.Euler(-cameraYRotation, -cameraXRotation, 0);
        playerCamera.transform.rotation = currentCameraTransform.rotation;
    }

    void AdjustCameraPosition(float cameraHeight) {
        /*
         * Position the player's camera according to the player's current position and rotation.
         * The camera is positionned headHeight above the player origin with a cameraYOffset offset.
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
        bool temp = false;
        rotationDifference = RayTrace(ref cameraPosition, ref toCamRotation, ref cameraHeight, ref temp, true, true);

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
         * state2: Move the camera upwards from the player's.body base to the default head height.
         
         * Once the final state ends, change the state back to standing.
         * Note that the player can still walk off into the falling state mid-animation.
         */
        float xRot = 0;
        float yRot = 0;
        float cameraHeightOffset = 0;
        float angleRotation = 10;
        float posState1 = 0.05f;
        float posState2 = 0.75f;
        float rotState1 = 0.15f;
        float rotState2 = 0.65f;

        /* Camera's Y rotation will follow the sine graph of [0, PI] with x axis being time spent in thos state */
        xRot = cameraXRotation;
        if(stateTime < rotState1) {
            yRot = cameraYRotation - angleRotation*Mathf.Sin(0 + (Mathf.PI/2f)*RatioWithinRange(0, rotState1, stateTime));
        }
        else if(stateTime < rotState2) {
            yRot = cameraYRotation - angleRotation*Mathf.Sin((Mathf.PI/2f) + (Mathf.PI/2f)*RatioWithinRange(rotState1, rotState2, stateTime));
        }
        else {
            yRot = cameraYRotation;
        }

        /* Camera's y position offset also follows a trig function relative to time spent in this state */
        if(stateTime < posState1) {
            cameraHeightOffset -= (headHeight + playerBodyLength/2f) * Mathf.Sin((Mathf.PI/2f)*RatioWithinRange(0, posState1, stateTime));
        }
        else if(stateTime < posState2) {
            cameraHeightOffset -= (headHeight + playerBodyLength/2f) * (Mathf.Cos(Mathf.PI*RatioWithinRange(posState1, posState2, stateTime))+1)/2f;
        }

        /* Switch to the standing state if the camera animation is complete */
        if(stateTime > posState2 && stateTime > rotState2) {
            cameraHeightOffset = 0;
            ChangeState((int) PlayerStates.Standing);
        }

        /* Set the rotation of the camera on it's own */
        currentCameraTransform.rotation = transform.rotation;
        currentCameraTransform.rotation *= Quaternion.Euler(-yRot, -xRot, 0);

        /* Set the position of the camera using te expected height + the calculated offset */
        AdjustCameraPosition(GetCameraHeight() + cameraHeightOffset);
    }

    void AnimateCameraFastFalling() {
        /*
    	 * Apply a slight random rotation to the camera depending on
    	 * the player speed ti signify how fast they are falling.
    	 */
        float speedRatio = RatioWithinRange(maxYVelocity, maxYVelocity*fastFallMod, -currentYVelocity);
        float r = speedRatio*0.2f;

        /* Put the camera into it's normal resting position */
        AdjustCameraPosition(GetCameraHeight());

        /* Apply a random rotation effect to the camera */
        currentCameraTransform.rotation *= Quaternion.Euler(
                Random.Range(-r, r), Random.Range(-r, r), Random.Range(-r, r));
        playerCamera.transform.rotation = currentCameraTransform.rotation;
    }


    /* ----------------- Value Updating Functions ------------------------------------------------------------- */
    
    void PrimeJumpingValue() {
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
         * Change the player's leg and step lengths depending on the state they are in.
         * The legs start at the base of the player model, so legswill always
         * have a length equal or larger than half the bodyLength.
         */
         
         /* The legs must always reach atleast the base of the player's.body */
		currentLegLength = playerBodyLength/2f; 
		currentStepHeight = givenStepHeight;
		
		/* Keep the leg lengths their default size when grounded */
		if(PlayerIsGrounded()){
			currentLegLength += givenLegLength;
		}
		
		/* Change the leg length while airborn */
		else if(PlayerIsAirborn()){
            /* While falling upwards, heavily shorten the legs */
            if(currentYVelocity >= 0) {
                currentLegLength = givenLegLength*0.1f;
                ///currentStepHeight = givenStepHeight*0.1f;
            }
            /* While falling downwards, have the leg's length become relative to the players falling speed. 
			 * This will prevent the player's torso from landing onto an object before their legs 
			 * Unless a large frame delay (time.deltatime) or feet dont hit enough objects. 
			 * the legs are the same length of the distance that will be travelled next frame + stepheight for insurance. */
            else {
                currentLegLength += -currentYVelocity;
            }
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
        inputVector *= movementSpeed;
        if(Input.GetKey(KeyCode.LeftShift)) {
            inputVector *= runSpeedMultiplier;
        }

        /* Rotate the input direction to match the player's view. Only use the view's rotation along the Y axis */
        inputVector = Quaternion.AngleAxis(-cameraXRotation, transform.up)*transform.rotation*inputVector;
    }

    void ResetPlayer() {
        /*
         * Reset the player's sounds, camera effects and positional values.
         */

        /* Reset the player's sounds */
        playerSoundsScript.ResetAll();

        /* Reset the camera's effects */
        cameraEffectsScript.ResetCameraEffects();

        /* Start the player in the standing state so they can link themselves to the floor */
        state = -1;
        ChangeState((int) PlayerStates.Standing);

        /* Empty the arraylist of vectors that track the player's upcomming movement */
        if(expectedMovements != null) { expectedMovements.Clear(); }
        expectedMovements = new ArrayList();

        /* The "previous frame" had the player starting in it's current position */
        lastSavedPosition = transform.position;

        /* The player starts immobile */
        lastStepMovement = Vector3.zero;

        /* Reset the player's position depending on whether the player is given a starting room */
        if(lastRoom != null) {
            /* Use the lastRoom as the player's starting room by using it's reset function */
            Transform newTransform = lastRoom.ResetPlayer();
            gameObject.transform.position = newTransform.position;
            gameObject.transform.rotation = newTransform.rotation;
        }
        else {
            Debug.Log("Player was not linked a starting room");
        }

        /* Use the player's current position as their foot position */
        currentFootPosition = transform.position;

        /* Adjust the player model's position to reflect the player's body and leg length */
        transform.localPosition += new Vector3(0, playerBodyLength/2f + givenLegLength, 0);

        /* Set the camera's offset to it's natural default value */
        cameraYOffset = 0;
    }


    /* ----------- Event Functions ------------------------------------------------------------- */

    void ChangeState(int newState) {
        /*
         * Change the player's current state to the given newState. Run certain lines if
         * certain states change into other specific states (fast falling > standing)
         */
    
    
    	/* Going from FastFalling to a grounded state... */
    	if(state == (int) PlayerStates.FastFalling){
    		/*... Will have the player undergo a hard landing. */
    		playerStepTracker.PlayHardLanding();
    	}
    	/* Going from an airborn state to a grounded state... */
        if(StateIsAirborn(state) && !StateIsAirborn(newState)) {
        	
            /*... Will inform the footstep tracker of the landing. */
            playerStepTracker.PlayLanding(-currentYVelocity/maxYVelocity);
        }


        /* Dont change anything if the player is already in the new state */
        if(state != newState) {

			/* Entering the Standing state... */
			if(newState == (int) PlayerStates.Standing){
			
				/*... When leaving the Falling state... */
				if(state == (int) PlayerStates.Falling){
					/*... Will lower the camera offset relative to the falling speed */
					cameraYOffset = -(headHeight + playerBodyLength/2f)*RatioWithinRange(0, (maxYVelocity), -currentYVelocity);

                }
				
				/*... When leaving the FastFalling state... */
				if(state == (int) PlayerStates.FastFalling){
					/*... Causes a "hard fall", forcing the player into a landing animation. */
					Debug.Log("HARD FALL");
                	newState = (int) PlayerStates.Landing;
                	cameraYOffset = 0;
                }
			}
			
			/* Entering the Falling state... */
			if(newState == (int) PlayerStates.Falling){
				
				/*...When leaving the Landing state... */
				if(state == (int) PlayerStates.Landing){
					//Should we set the camera height to its relative position in the landing animstion?
				}
			}
			
			/* Entering the FastFalling state... */
			if(newState == (int) PlayerStates.FastFalling){

                /*... Will start playing the fastfalling audio */
                playerSoundsScript.PlayFastFall();

                /*... Will start a set of post processing effects. */
                cameraEffectsScript.StartEffectVignette();
                cameraEffectsScript.StartChromaticAberration();
			}

			/* Set the new state and reset the stateTimer */
            stateTime = 0;
            state = newState;
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

    void LegCollisionTest(ref Vector3 position, ref Quaternion direction, ref float length, int index) {
        /*
         * Use the given values to send a ray trace of the player's leg and return the distance of the ray.
         * Update the arrays that track the status of the leg with the given index. If the given
         * index is -1, then do not update the array
         */

		/* Use the RayTrace function */
		float preLength = length;
        bool temp = false;
		RayTrace(ref position, ref direction, ref length, ref temp, true, true);
		
		/* Update the legLengths array if needed */
		if(index != -1){
		
			/* The leg did not hit any objects/colliders */
			if(length == 0){
                extraLegLenths[index] = -1;
			} 
			
			/* The leg hit an object/collider */
			else {
                extraLegLenths[index] = preLength - length;
			}
		}
    }

    void FireLegRays() {
        /*
    	 * Use the player's current position and their current leg length to fire off a ray for each "leg" 
         * in the leg array, tracking how far they reach. If the leg does not reach an object 
         * or the gap between the leg and the pleyer's center is blocked, set the  distance 
         * of the leg in the extraLegLengths to -1, indicating the leg is not used.
    	 */
        Vector3 upDirection = transform.rotation*Vector3.up;
		Vector3 tempLegPos;
		Quaternion tempLegRotation;
		float tempLegLength;

        /* Test the first leg. It goes straight down relative to the player's center. */
        tempLegRotation = Quaternion.LookRotation(-transform.up, transform.forward);
        tempLegPos = transform.position;
        tempLegLength = currentLegLength + currentStepHeight;
        LegCollisionTest(ref tempLegPos, ref tempLegRotation, ref tempLegLength, 0);
		
		/* Test the collision for the other legs */
		for(int i = 1; i < extraLegLenths.Length; i++) {

            /* Check if nothing is blocking the space between the leg starting point and the player's center */
            float legGapDistance = legGap*playerBodyRadius;
			tempLegPos = transform.position;
			tempLegRotation = Quaternion.AngleAxis(i*(360/(extraLegLenths.Length-1)), upDirection)*transform.rotation;
			LegCollisionTest(ref tempLegPos, ref tempLegRotation, ref legGapDistance, -1);
			
			/* Fire the actual leg ray if there is nothing blocking the leg gab */
			if(legGapDistance == 0){
                /* Rotate the leg so it is rayTracing downward */
                tempLegRotation = Quaternion.LookRotation(tempLegRotation*-Vector3.up, tempLegRotation*Vector3.forward	);
				tempLegLength = currentLegLength + currentStepHeight;
				LegCollisionTest(ref tempLegPos, ref tempLegRotation, ref tempLegLength, i);
			}
			
			//Dont use this leg if the gap between the leg and the player is blocked
			else {
                extraLegLenths[i] = -1;
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
                currentYVelocity -= gravity*Time.deltaTime*60*fastFallMod/5f;
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

		/* Keep the range of cameraYOffset to be [bodyLength/2, -(height + boyLength/2] */
        if(cameraYOffset > playerBodyLength/2f) {
            cameraYOffset = playerBodyLength/2f;
        }
        
        else if(cameraYOffset < -(headHeight + playerBodyLength/2f)) {
            cameraYOffset  = -(headHeight + playerBodyLength/2f);
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
    	 * Put the player into the fast fall state if they are currently airborn.
    	 * This will run any time the player is outside the play area of a room.
    	 *
    	 * When the player is standing while outside the play area
    	 * (Walking on the wall), A good idea will be to have a 
    	 * "press R to reset". Also add a r to reset function.
    	 */

        /* Do not go into fastfall if the player is not currently falling or already fastfalling */
        if(PlayerIsAirborn() && state != (int) PlayerStates.FastFalling) {
            ChangeState((int) PlayerStates.FastFalling);
        }
    }

    public float GetYVelocityFastFallRatio() {
        /*
         * A very specific equaition to get a very specific value used with camera post-processing effects.
         * It returns the ratio of the player's velocity as it approaches it's fastFalling terminal velocity.
         * Value ranges between [0, 1]. 0 being <= max Falling velocity. 1 being >= max FastFalling velocity 
         */

        return RatioWithinRange(maxYVelocity, maxYVelocity*fastFallMod, -currentYVelocity);
    }


    /* ----------- Helper Functions ------------------------------------------------------------- */

    Quaternion RayTrace(ref Vector3 position, ref Quaternion rotation, ref float distance, 
        ref bool teleported, bool detectTeleportTriggers, bool detectOtherColliders) {
        /*
         * Fire a ray from the given position with the given rotation forwards for the given distance.
         * The quaternion returned represents the amount of rotation that the given rotation underwent.
         * 
         * detectTeleportTriggers as true will cause any collision with a teleportTrigger to teleport 
         * the position and rotation to the hit trigger's partner using it's teleportParameters function.
         * teleportTrigger Collisions with detectTeleportTriggers as false will be ignored.
         * 
         * detect other Colliders as true will cause any collision with any other trigger to cause
         * the position to stop at the point of collision and reduce the distance amount respectively.
         * these type of collisions will be ignored if detectOtherColliders is false.
         * 
         * The teleported reference will be set to false if no teleporter was encountered. It will be
         * set to true if it collides with a teleporter and moves the position and rotation.
         */
        Quaternion totalRotation = Quaternion.identity;
        Quaternion rotationDifference;
        RaycastHit hitInfo = new RaycastHit();
        bool stopRayTrace = false;
        LayerMask rayLayerMask = 0;
        teleported = false;

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
                Debug.DrawLine(position, position + rotation*Vector3.forward*hitInfo.distance, Color.green);
                position += rotation * Vector3.forward * hitInfo.distance;
                distance -= hitInfo.distance;
                
                /* Hitting a teleport trigger will teleport the current position and direction to it's partner trigger */
                if(hitInfo.collider.GetComponent<TeleporterTrigger>() != null) {
                    /* Teleport the parameters and apply the rotation that was underwent to the totalRotation quaternion */
                    rotationDifference = hitInfo.collider.GetComponent<TeleporterTrigger>().TeleportParameters(ref position, ref rotation);
                    totalRotation = rotationDifference*totalRotation;
                    teleported = true;
                    //Prevent the rayTrace from hitting another trigger after teleporting
                    //rayLayerMask = 0;
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
                Debug.DrawLine(position, position + rotation*Vector3.forward*distance, Color.white);
                position += rotation*Vector3.forward*distance;
                distance = 0;
            }
        }

        return totalRotation;
    }
    
    bool PlayerIsGrounded(){
    	/*
    	 * Return true if the player is in a grounded state with theor legs linked to an object
    	 */
    
    	return !StateIsAirborn(state);
    }
    
    bool PlayerIsAirborn(){
    	/*
    	 * Return true if the player is in a freefall state/legs do not connect to an object
    	 */

    	return StateIsAirborn(state);
    }
    
    bool StateIsAirborn(int givenState){
    	/* 
    	 * Return true if the given state is airborn. So far, a state 
    	 * can only either be airborn or grounded (not airborn).
    	 */
    	bool isFalling = false;
    
    	if(givenState == (int) PlayerStates.Falling || givenState == (int) PlayerStates.FastFalling){
    		isFalling = true;
    	}
    
    	return isFalling;
    }
    
    public static float RatioWithinRange(float min, float max, float value) {
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
}
