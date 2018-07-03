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
    NULL,
    LoadingIntro,
    InIntro,
    LeavingIntro,
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
    public PlayerStates state;
    public bool inMenu;
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
    /* The Expected movements (in the form of Vector3) of the player for the upcomming physics update */
    private ArrayList expectedMovements;
    /* The amount of distance travelled since the last step tracker update */
    private Vector3 lastStepMovement;
    /* The direction and magnitude of player's movement input */
    private Vector3 inputVector = Vector3.zero;
    /* How fast a player moves using player inputs */
    public float movementSpeed;
    public float runSpeedMultiplier;
    /* Sliding determines how much of getAxis should be used over getAxisRaw. */
    [Range(1, 0)]
    public float sliding;
    /* Sensitivity for the mouse. mouseSens Controlled by the menu, mod controlled by programmer */
    [HideInInspector]
    public float mouseSens = 5;
    [HideInInspector]
    public float mouseSensMod = 5;
    /* How fast or slow the character acts in relation to time */
    public float playerTimeRate = 1;
    private float playerTimeChangeMod = 0.75f;
    public float soundsTimeRate = 1;
    private float soundsTimeChangeMod = 0.4f;
    public float roomTimeRate = 1;
    private bool noticedOutside = false;

    /* How fast a player accelerates towards their feet when falling. */
    public float gravity;
    /* The Y velocity of the player along with its max(positive) */
    public float currentYVelocity;
    public float maxYVelocity;
    /* The velocity modifier when the player is FastFalling */
    public int fastFallMod;
    /* Unique values for the fastFallMod value */
    [HideInInspector]
    public int fastFallModNormal = 15;
    [HideInInspector]
    public int fastFallModOutside = 5;

    /* Howm uch the gravity vector is modified. This is changed by the PuzzleRoomEditor to control falling speed */
    [HideInInspector]
    public float gravityVectorMod = 1;

    /* How fast a player travels upward when they jump */
    public float jumpSpeed;
    /* Used to determine the state of the jump. If true, the next jump opportunity will cause the player to jump. */
    private bool jumpPrimed;
    /* The state of the jump key on the current and previous frame. true = pressed */
    private bool jumpKeyPrevious = false;
    private bool jumpKeyCurrent = false;

    /* The amount of time needed for the player to reset upon pressing the R key */
    private float currentResetTime;
    public float resetTime;

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
    /* How much faster the Yoffset is reduced as the offset reaches it's limits */
    [Range(1, 0)]
    public float morphDiff;


    /* --- Player Scripts --------------------------- */
    /* Contains all the post processing effects that will be applied to the camera */
    public CustomPlayerCameraEffects cameraEffectsScript;

	/* Handles all sounds produced by the player */
	public PlayerSounds playerSoundsScript;

    /* Handles all footstep tracking */
    public FootstepTracker playerStepTracker;

    /* The last attached room the player was in. Must be set in the editor before starting. */
    public AttachedRoom lastRoom;

    /* The WaitingRoom the player will first encounter from the startingRoom. This room will be enabled on startup. */
    public WaitingRoom firstWaitingRoom;

    /* The StartingRoom script of the game's startingRoom. This will control the player's starting position. */
    public StartingRoom startingRoom;

    /* The last collider that was hit. Used when calling Raytrace and we want to save the collider the ray hits */
    private Collider lastHitCollider;
    
    /* The player is in the outside state if they enter outside through the startingRoom's window portal */
    private bool outsideState = false;

    /* The clipping plane of the player's camera. The portals will use this for their cameras. */
    public static float cameraFarClippingPlane = 10000f;
    public static float cameraNearClippingPlane = 0.02f;

    /* The type of step sound is played for the player footstep tracker */
    private int currentStepType = 0;

    /* --- Menu Variables --------------------------- */
    [HideInInspector]
    public Menu playerMenu;
    /* Camera position values */
    private Vector3 camDestinationPos;
    private Quaternion camDestinationRot;
    private float introCamDistance = -1;
    public Vector3 extraCamRot = Vector3.zero;

    /* Intro Animation values */
    private float IntroWindowStrafeSpeed = 0.25f;
    //Used to smoothly transition between InIntro and LeavingIntro
    private float remainingInIntroTime;
    private bool aboutToLeaveIntro = false;
    private float timeToLeaveIntro = 3;


    /* --- One-time use Variables --------------------------- */
    public bool fallingOutWindow = false;
    private Quaternion savedRotation = Quaternion.Euler(0, 0, 0);

    /* Controls how mnay legs need to be grounded for the player to be considered standing */
    private int requiredGroundedCount = 1;

    /* Debugging trackers */
    public static int renderedCameraCount = 0;
    public static int renderedCameraCount2 = 0;
    private System.DateTime before;


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
        
        /* Place the player in  the startingRoom, facing the window */
        SetupPlayer();
    }

    void FixedUpdate() {
        /*
         * Handle player movement. This includes moving the player in the inputted direction and stepping.
         * 
         * Also, handle a teleport check if an Update call was not made between the the last physics check.
         * This could be due to the high amount of physics checks or possibly a drop in framerate for the player.
         * The goal is to prevent the player from moving past if they are not updating fast enough.
         */

        /* Do not update the player if they are in the menu */
        if(!inMenu) {

            /* Move the player using their given input and the gravity vector. Take into account the player's time rate. */
            UpdateInputVector();
            MovePlayer(inputVector*playerTimeRate + GetGravityVector()*playerTimeRate);

            /* From the player's current position, execute a step check to see if they need to move along their Y axis */
            /* Do not use steps if the player is in the intro */
            if(!PlayerIsInIntro()) {
                StepPlayer();
            }

            /* Update the step tracker with whatever steps were made since the last FixedUpdate call */
            if(PlayerIsGrounded()) {
                playerStepTracker.AddHorizontalStep(Quaternion.Inverse(transform.rotation)*lastStepMovement);
                lastStepMovement = Vector3.zero;
            }
            
        }
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
        /* Get how long it's been since a time update *//*
        System.DateTime current = System.DateTime.Now;
        System.TimeSpan duration = current.Subtract(before);
        //Debug.Log(" ------- Since update: " + duration.Milliseconds);
        before = System.DateTime.Now;

        if(duration.Milliseconds > 100) {
            Debug.Log(" ------ NEW SLOW FRAME --------- ");
            Debug.Log("UPDATE TIME: " + duration.Milliseconds);
            Debug.Log("CAM COUNT: " + renderedCameraCount);
            Debug.Log("CAM UPDATE TIME: " + renderedCameraCount2);
        }
        renderedCameraCount = 0;
        renderedCameraCount2 = 0;*/

        
        /* Pressing the escape button will send a request to the menu and either open/close the menu */
        MenuKey();

        /* Update values relevent to the intro */
        if(PlayerIsInIntro()) {
            UpdateIntroValues();
        }
        
        /* Do not update the player if they are in the menu */
        if(!inMenu) {
            
            /* Update the player's inputs and stateTime */
            inputs.UpdateInputs();
            stateTime += Time.deltaTime;

            /* Check the player's inputs to see if they prime a jump */
            PrimeJumpingValue();

            /* Check if the player wants to reset their position */
            if(inputs.rKeyPressed) {
                StartPlayerReset();
            }

            /* Check if the user presses any other important keys */
            ArbitraryInput();

            /* Update and animated the resetTimer if the player wants to reset */
            if(currentResetTime > -1) {
                UpdateResetAnimation();
            }

            /* Update the player's stride progress to determine when a footstep sound effect should play */
            playerStepTracker.UpdateStride();

            //Draw a line in the camera's forward vector
            //////Debug.DrawLine(playerCamera.transform.position,
            //////        playerCamera.transform.position + playerCamera.transform.rotation*Vector3.forward*0.5f, Color.green);
        }
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
         
        /* Update the outside window's portal collider (enable it if we are outside so the camera can hit it) */
        if(fallingOutWindow) { FallingOutWindowUpdate(false); }

        /* Prevent the mouse from moving the camera while in a menu */
        if(!inMenu) {
            AdjustCameraRotation();
        }

        /* Update currentCameraTransform to control the transform of the camera, depending on the current state */
        if(state == PlayerStates.Landing) {
            /* Apply an animation to the camera while in the landing state */
            AnimatedCameraLanding();
        }
        else if(state == PlayerStates.FastFalling) {
            /* Apply an animation to the camera while in the fastFalling state */
            AnimateCameraFastFalling();
        }
        else if(PlayerIsInIntro()) {
            /* All the intro states use the same camera placement function */
            FireCameraRayInIntroState();
        }
        else {
            /* Any other state simply places the camera into it's default position */
            AdjustCameraPosition(GetCameraHeight());
        }
        
        /* Update the player's camera's transform to reflect the changes to currentCameraTransform */
        playerCamera.transform.position = currentCameraTransform.position;
        playerCamera.transform.rotation = currentCameraTransform.rotation;
        
        /* If the menu is open, allow it to control the final rotation of the camera */
        if(inMenu) {
            playerCamera.transform.localEulerAngles += extraCamRot;
        }

        /* Apply any needed special effects to the camera. Runs everytime the camera renders */
        /* Do not update these camera effects if the game is in a menu */
        if(!inMenu) {
            cameraEffectsScript.UpdateCameraEffects();
        }


        /* Update the camera's rendering layer if needed */
        if(fallingOutWindow) { FallingOutWindowUpdate(true); }
    }
    
    
    /* ----------------- Main Movement Function ------------------------------------------------------------- */
    
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
         * When recieveing a request to move the player, handle the movement instantly.
         */
         
        /* Check if there was any movement at all */
        if(movementVector.magnitude != 0 && movementVector != Vector3.zero) {

            /* Set the values used to fire the ray */
            float remainingDistance = movementVector.magnitude;
            Vector3 position = transform.position;
            Quaternion direction = Quaternion.LookRotation(movementVector.normalized, transform.up);
            bool teleported = false;
            /* Fire a ray of the player's movement that interracts with the world, including teleporters */
            Quaternion rotationDifference = RayTrace(ref position, ref direction, ref remainingDistance, ref teleported, true, true, false);
            /* If the player's movement passes through a teleporter, reposition their transform to reflect the teleport */
            transform.position = position;
            transform.rotation = rotationDifference * transform.rotation;
        }

        /* When grounded, add any movement to the stepTracker vector */
        if(PlayerIsGrounded()) {
            lastStepMovement += movementVector;
        }

        /* Empty the expectedMovements array after we have used them */
        expectedMovements.Clear();

        /* Freeze the player's rigidbody's velocity */
        Rigidbody rigidBody = GetComponent<Rigidbody>();
        rigidBody.velocity = Vector3.zero;
    }


    /* ----------------- Step Functions ------------------------------------------------------------- */

    void StepPlayerGrounded() {
        /*
    	 * Check each leg's distance and whether they can reach down to an object. 
    	 * If enough legs are no longer grounded, attempt to jump and enter the falling state.
    	 */

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
            ChangeState(PlayerStates.Falling);
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
            ChangeState(PlayerStates.Standing);

            /* Calculate the current foot position of the player by finding the new leg length */
            float newLegLength = 0;
            for(int i = 0; i < extraLegLenths.Length; i++) {
                if(extraLegLenths[i] >= 0) {
                    newLegLength += (extraLegLenths[i])/currentGroundedCount;
                }
            }

            /* Use the new legLength to make the player undergo a "step" */
            DoStep(newLegLength);
        }

        /* If not enough legs are grounded and the player is in the falling state... */
        else if(state == PlayerStates.Falling) {

            /* ...Apply the entering fastFall state */
            ApplyFastfall(false);
        }
    }
    
    void DoStep(float stepLegLength) {
        /*
    	 * Update the player's footingPosition along with the new
    	 * position for the body and the camera to complete a "step"
         * 
         * Given a step length, get the position the legs end at, and send
         * a request to move the player upward from the footPosition a 
         * distance equal to their standing leg length.
     	*/
        Vector3 upDirection = transform.up;

        /* Place the footPosition using the stepLegLength */
        currentFootPosition = transform.position - upDirection*(stepLegLength);
        
        /* Move the player's body so that their "legs" are now of proper length with their body in a standing position */
        float defaultLegLength = playerBodyLength/2f + givenLegLength;
        MovePlayer((currentFootPosition - transform.position) + upDirection*defaultLegLength);

        /* Revert any movement done to the camera to smooth the players view */
        cameraYOffset -= (defaultLegLength - stepLegLength);
    }
    

    /* ----------------- Camera Update Functions ------------------------------------------------------------- */

    void AdjustCameraRotation() {
        /*
         * Take in user inputs to properly rotate the currentCameraTransform's facing direction.
         */

        /* Copy the player's current rotation before rotating the camera */
        currentCameraTransform.rotation = transform.rotation;

        /* Add the X input rotation and ensure it does not overflow. Add a fraction of the time rate to modify the sens */
        float sens = (1/3f + playerTimeRate/3f) * mouseSens / mouseSensMod;
        cameraXRotation -= sens*inputs.mouseX;
        if(cameraXRotation < 0) { cameraXRotation += 360; }
        else if(cameraXRotation > 360) { cameraXRotation -= 360; }

        /* Add the Y input rotation and ensure it does not overflow */
        cameraYRotation += sens*inputs.mouseY;
        cameraYRotation = Mathf.Clamp(cameraYRotation, -75, 75);

        /* Apply the rotations to the camera's default transform */
        currentCameraTransform.rotation *= Quaternion.Euler(-cameraYRotation, -cameraXRotation, 0);
        //playerCamera.transform.rotation = currentCameraTransform.rotation;
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
        

        /* Fire two rayTraces: The first is used to "scout" ahead. The second is used to ensure the camera does not 
         * end up close to an object, so it will always move nearly the same distance as the first trace. */
        Vector3 scoutPos = cameraPosition;
        Quaternion scoutRot = toCamRotation;
        float scoutDist = cameraHeight;
        bool temp = false;

        /* Fire the scouting ray */
        rotationDifference = RayTrace(ref scoutPos, ref scoutRot, ref scoutDist, ref temp, true, true, false);

        /* Reduce the cameraHeight if the scout ray collided with anything */
        cameraHeight = (cameraHeight - scoutDist) - 0.25f;

        /* fire the actual ray for the camera */
        rotationDifference = RayTrace(ref cameraPosition, ref toCamRotation, ref cameraHeight, ref temp, true, true, false);


        /* Use the new position and rotation to find the camera's final position and rotation */
        currentCameraTransform.position = cameraPosition;
        currentCameraTransform.rotation = rotationDifference*currentCameraTransform.rotation;
        //playerCamera.transform.position = currentCameraTransform.position;
        //playerCamera.transform.rotation = currentCameraTransform.rotation;
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

        /* Camera's Y rotation will follow the sine graph of [0, PI] with x axis being time spent in those state */
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
            ChangeState(PlayerStates.Standing);
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
        float r = speedRatio*0.5f;

        /* Put the camera into it's normal resting position */
        AdjustCameraPosition(GetCameraHeight());

        /* Apply a random rotation effect to the camera */
        currentCameraTransform.rotation *= Quaternion.Euler(
                Random.Range(-r, r), Random.Range(-r, r), Random.Range(-r, r));
    }
    
    private void FireCameraRayInIntroState() {
        /*
         * A unified function used during the intro states for placing the camera
         */
        
        /* Set the parameters required for the ray trace */
        Vector3 currentCameraPosition = camDestinationPos;
        Quaternion currentCameraRotation = camDestinationRot;
        float cameraDistance = introCamDistance;
        bool teleported = false;
        RayTrace(ref currentCameraPosition, ref currentCameraRotation, ref cameraDistance, ref teleported, true, false, false);
        currentCameraTransform.rotation = currentCameraRotation;
        currentCameraTransform.position = currentCameraPosition;

        /* Depending on whether the camera has teleported or not, handle whether the camera will render terrain */
        PlayerRenderTerrain(teleported);
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
            jumpPrimed = false;
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
            }
            /* While falling downwards, have the leg's length become relative to the players falling speed. 
			 * This will prevent the player's torso from landing onto an object before their legs 
			 * Unless a large frame delay (time.deltatime) or feet dont hit enough objects. 
			 * the legs are the same length of the distance that will be travelled next frame + stepheight for insurance. */
            else {
                currentLegLength += -currentYVelocity;
            }
		}

        /* Don't change the leg lengths while in menus */
        else if(PlayerIsInIntro()) {

        }

        else {
            Debug.Log("WARNING: current state does not handle UpdateLegLengths");
        }
    }

    public void UpdateInputVector() {
        /*
         * Take in user input to calculate a direction vector for the player to move towards. 
         */
         
        /* Use two input types for each axis to allow more control on player movement */
        inputVector = new Vector3((1-sliding)*inputs.playerMovementXRaw + sliding*inputs.playerMovementX,
                0, (1-sliding)*inputs.playerMovementYRaw + sliding*inputs.playerMovementY);

        /* Keep the movement's magnitude from going above 1 */
        if(inputVector.magnitude > 1) {
            inputVector.Normalize();
        }
        
        /* Alter the used movementSpeed relative to the player's airborn state */
        float usedMovementSpeed = movementSpeed;

        /* Increase the player's base airborn movement speed if they are falling */
        if(PlayerIsAirborn()) {
            usedMovementSpeed += 0.5f*movementSpeed*Mathf.Clamp(Mathf.Abs(currentYVelocity)/maxYVelocity, 0, 1);

            /* While airborn and outside, increase the player's airborn movement  */
            if(outsideState) {
                usedMovementSpeed *= 4;
            }
        }
        /* If the player is in the Intro state, do not accept any movements from keyboard or mouse */
        else if(PlayerIsInIntro()) {
            inputVector = Vector3.zero;
        }

        /* Add the player speed to the movement vector */
        inputVector *= usedMovementSpeed;
        if(Input.GetKey(KeyCode.LeftShift)) {
            inputVector *= runSpeedMultiplier;
        }

        /* Rotate the input direction to match the player's view. Only use the view's rotation along the Y axis */
        inputVector = Quaternion.AngleAxis(-cameraXRotation, transform.up)*transform.rotation*inputVector;
    }

    void ResetPlayer(bool resetSounds) {
        /*
         * Reset the player's sounds, camera effects and positional values. 
         * Only reset the sounds if the given boolean is true. This is to allow
         * the playerSounds script to handle it's creation itself as the player resets
         * themselves on startup and the playerSounds script has not yet initialized.
         */

        /* Reset the player's sounds */
        if(resetSounds) {
            playerSoundsScript.ResetAll(true);
        }

        /* Reset the camera's effects and any extra animations it has */
        cameraEffectsScript.ResetCameraEffects();
        currentResetTime = -1;

        /* Reset the step tracker */
        playerStepTracker.ResetFootTiming(); 
        playerStepTracker.ResetStrideProgress();
        playerStepTracker.ResetStepBuffer();

        /* Start the player in the standing state */
        state = PlayerStates.NULL;
        ChangeState(PlayerStates.Standing);

        /* Empty the arraylist of vectors that track the player's upcomming movement */
        if(expectedMovements != null) { expectedMovements.Clear(); }
        expectedMovements = new ArrayList();

        /* Set the fastFall mod to it's normal value */
        fastFallMod = fastFallModNormal;

        /* Reset the player's position and rotation depending on whether the player is given a starting room */
        if(lastRoom != null) {
            /* Use the lastRoom as the player's starting room by using it's reset function */
            Transform newTransform = lastRoom.ResetPlayer();
            gameObject.transform.position = newTransform.position;
            gameObject.transform.rotation = savedRotation;
            cameraXRotation = 0;
            cameraYRotation = 0;

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

        /* The player starts immobile */
        lastStepMovement = Vector3.zero;

    }

    void SetupPlayer() {
        /*
         * Runs on startup, it properly positions the player and puts them in the proper state 
         * for the startup of the game.
         */

        /* Reset the step tracker */
        playerStepTracker.ResetFootTiming();
        playerStepTracker.ResetStrideProgress();
        playerStepTracker.ResetStepBuffer();
        
        /* Empty the arraylist of vectors that track the player's upcomming movement */
        if(expectedMovements != null) { expectedMovements.Clear(); }
        expectedMovements = new ArrayList();

        /* Reset the camera's effects and any extra animations it has */
        cameraEffectsScript.ResetCameraEffects();
        currentResetTime = -1;

        /* Set the camera's rendering distance to reflect the fact it's outside */
        playerCamera.farClipPlane = cameraFarClippingPlane;
        //playerCamera.nearClipPlane = 0.4f;

        /* Place the player to be standing on the top of the startingRoom's stairs.
         * Add the leg gap distance so the player is not stepping on the stairs. */
        transform.position = startingRoom.exit.exitPointBack.transform.position + new Vector3(0, 0, legGap*playerBodyRadius);

        /* Adjust the player model's position to reflect the player's body and leg length */
        currentFootPosition = transform.position;
        transform.localPosition += new Vector3(0, playerBodyLength/2f + givenLegLength, 0);
        
        /* Have the player facing towards the window */
        transform.localEulerAngles = new Vector3(0, 180, 0);

        /* Have the camera render the outside terrain as the camera will be outside on startup */
        PlayerRenderTerrain(true);

        /* The player starts immobile */
        lastStepMovement = Vector3.zero;

        /* Set the fastFall mod to it's normal value */
        fastFallMod = fastFallModNormal;

        /* Set the camera's offset to it's natural default value */
        cameraYOffset = 0;

        /* Start the player in the LoadingIntro state */
        state = PlayerStates.NULL;
        inMenu = true;
        ChangeState(PlayerStates.LoadingIntro);
        //After entering the intro state, we want to update certain values that will be used with the intro animation
        AdjustCameraPosition(GetCameraHeight());
        camDestinationPos = currentCameraTransform.position;
        camDestinationRot = currentCameraTransform.rotation;
        introCamDistance = 15f;
    }

    void StartPlayerReset() {
        /*
         * Start the animation for the player being reset
         */

        /* Start the player reset vignette effect */
        currentResetTime = resetTime;
        cameraEffectsScript.StartPlayerReset(resetTime);
    }

    void UpdateResetAnimation() {
        /*
         * Update the reset timer and animate the camera's vignette and lower the volume of the player sounds.
         * If the user inputs any movement inputs (directional, jump). 
         * 
         * currentResetProgress tracked the start (1) to the end (0) of the animation, finishing with a reset.
         */
        float currentResetProgress = (1 - (resetTime - currentResetTime)/resetTime);

        /* Check if the user inputted any movement inputs or is outside */
        if(inputs.spaceBarHeld == true || inputs.playerMovementXRaw != 0 || 
                inputs.playerMovementYRaw != 0f || fallingOutWindow || outsideState) {
            /* Stop the animation */
            StopResetAnimation();
        }
        
        /* Continue updating the animation */
        else {
            currentResetTime -= Time.deltaTime;

            /* Update the volume of the audio mixer. Delay the time until the audio starts changing */
            float delayAudioChange = 0.35f;
            if((1 - currentResetProgress) > delayAudioChange) {
                playerSoundsScript.TemporaryMixerAdjustment((currentResetProgress)/(1-delayAudioChange));
            }

            /* Update the vignette effect for the camera */
            cameraEffectsScript.UpdatePlayerReset(currentResetTime);

            /* Reset the player once the reset animation is finished */
            if(currentResetTime <= 0) {
                ResetPlayer(true);
            }
        }
    }
    
    void StopResetAnimation() {
        /*
         * Stop the reset animation for the player and the camera before it finishes.
         * Do not run .DisableVignette() as the camera will disable the vignette itself
         * if it is not currently being used.
         */

        currentResetTime = -1;

        /* Reset the vignette effect for the camera */
        cameraEffectsScript.StopPlayerReset();

        /* Reset the audio levels of the audio mixer */
        playerSoundsScript.ResetAudioMixerVolume();
    }

    void FallingOutWindowUpdate(bool updateLayer) {
        /*
         * This is called every update when the player has broken the startingRoom's window 
         * and has not yet made it outside. It observes the player's position relative to 
         * the outside camera and decides what to do about the window's portal collider 
         * and the player camera's rendering layer.
         * 
         * The given boolean determines whether we update the camera layer or the window collider
         */

        /* Get the Z positions of the player camera and the staringRoom's outside window */
        float windowExit = startingRoom.windowExit.position.z;
        float camPos = playerCamera.transform.position.z;
        float playerPos = transform.position.z;

        /* Check the camera's position and update the camera's rendering layer */
        if(updateLayer) {
            if(windowExit > camPos) {
                /* The camera is past the window - The camera is outside */
                PlayerRenderTerrain(true);
            }
            else {
                /* Camera is behind the window - Camera is inside */
                PlayerRenderTerrain(false);
            }
        }

        /* Check the player's position and update the outside window's collider */
        else {
            if(windowExit > playerPos) {
                /* The player's body is outside */
                startingRoom.window.portalSet.ExitPortal.TriggerContainer.GetChild(0).GetComponent<BoxCollider>().enabled = true;
                EnteredOutside();
            }
            else {
                /* The player is inside */
                startingRoom.window.portalSet.ExitPortal.TriggerContainer.GetChild(0).GetComponent<BoxCollider>().enabled = false;
            }
        }
        
        /* 
		 * Adjust the timeFlow of the player, StartingRoom and playerSounds 
		 */
		/* Don't Update the rates in a menu. Instead, freeze the sounds and the room */
		if(inMenu){
			if(roomTimeRate != 1){ startingRoom.UpdateTimeRate(0, 0); }
        	if(soundsTimeRate != 1){ playerSoundsScript.UpdateTimeRate(soundsTimeRate); }
        }

		/* Increment the time rates relative to the player's position */
		else{
            /* Start returning the time rate if the conditions are met */
            if((windowExit > playerPos + 15f) || (windowExit > playerPos + 3f && cameraYRotation > 65)) {
                noticedOutside = true;
            }

			if(noticedOutside) {
                playerTimeRate += Time.deltaTime*playerTimeChangeMod*0.25f;
                roomTimeRate += Time.deltaTime*playerTimeChangeMod*0.25f;
                soundsTimeRate += Time.deltaTime*soundsTimeChangeMod*0.25f;
                /* Prevent the rates from going above the default */
                if(playerTimeRate > 1) { playerTimeRate = 1; }
                if(roomTimeRate > 1) { roomTimeRate = 1; }
                if(soundsTimeRate > 1) { soundsTimeRate = 1; }
            }
			else {
                playerTimeRate -= Time.deltaTime*playerTimeChangeMod;
                roomTimeRate -= Time.deltaTime*playerTimeChangeMod;
                soundsTimeRate -= Time.deltaTime*soundsTimeChangeMod;
                /* Prevent the timeRates from going bellow a given limit */
                if(playerTimeRate < 0.025f) { playerTimeRate = 0.025f; }
                if(roomTimeRate < 0.075f) { roomTimeRate = 0.075f; }
                if(soundsTimeRate < 0.5f) { soundsTimeRate = 0.5f; }
            }
			/* Update the rooms with their new timeRates */
			startingRoom.UpdateTimeRate(roomTimeRate, soundsTimeRate);
            playerSoundsScript.UpdateTimeRate(soundsTimeRate);
		}

        /* If the player has fallen far enough away from the window, stop running this update call */
        if(fallingOutWindow && windowExit > playerPos + 20) {
            fallingOutWindow = false;

            /* Inform the menu that the player has some distance from the outside window */
            playerMenu.PlayerEnteredOutside();

            /* Upgrade the currently playing music */
            playerSoundsScript.UpgradeMusic();

            /* Reset the timeRates incase they have not yet reached their default */
            playerTimeRate = 1;
            roomTimeRate = 1;
            soundsTimeRate = 1;
            playerSoundsScript.UpdateTimeRate(soundsTimeRate);
			startingRoom.UpdateTimeRate(roomTimeRate, soundsTimeRate);
        }
    }

    void UpdateIntroValues() {
        /*
         * Runs on Update when in the intro, it is used to contain and update all values pertinent to the intro
         */

        /* The game will only start the intro once the outside terrain has loaded */
        if(state == PlayerStates.LoadingIntro) {
            if(playerMenu.terrainController.GetLoadingPercent() == 1) {
                /* Leave the loading state and start the InIntro state */
                ChangeState(PlayerStates.InIntro);

                /* Start playing the outside background sounds */
                playerSoundsScript.PlayStartupMusic();
            }
        }

        /* Once in the InIntro, animate the startingRoom's outside window */
        else if(state == PlayerStates.InIntro) {

            /* If the player is about the leave the intro, reduce window's strafing speed */
            float strafeSpeed = IntroWindowStrafeSpeed;
            if(aboutToLeaveIntro) {
                float earlyTimeToStop = 0.5f;
                float remainingTimeRatio = (remainingInIntroTime / (timeToLeaveIntro - earlyTimeToStop));
                if(remainingTimeRatio < 0) { remainingTimeRatio = 0; }
                strafeSpeed *= remainingTimeRatio;
            }

            /* Strafe the window */
            startingRoom.windowExit.position = startingRoom.windowExit.position + new Vector3(strafeSpeed*Time.deltaTime*40, 0, 0);
            startingRoom.UpdateOutsideWindowPositon(false);

            /* If we are leaving the InMenu state, decrement remainingInMenuTime */
            if(aboutToLeaveIntro) {
                /* Decrease remainingInIntroTime */
                remainingInIntroTime -= Time.deltaTime;
                if(remainingInIntroTime <= 0) {
                    /* When time runs out, change to the leavingIntro state */
                    ChangeState(PlayerStates.LeavingIntro);
                    aboutToLeaveIntro = false;
                }
            }
        }

        /* When leaving the intro, reduce the distance the camera is from the player */
        else if(state == PlayerStates.LeavingIntro) {
            
            /* Reduce the introCamDistance distance every frame during this intro animation.
             * The amount that gets reduced is relative to the amount of distance remaining. */
            float consistentReduction = Time.deltaTime;
            float var1 = 2f;
            float var2 = 0.9f;
            if(introCamDistance > var1) {
                introCamDistance -= consistentReduction;
            }
            else {
                introCamDistance -= (introCamDistance/var1)*(consistentReduction*var2) + (consistentReduction*(1 - var2));
            }

            /* Get the distance from the camDestinationPos to the startingRoom's portal. 
             * This is to prevent the near-clipping plane from cutting off the portal. */
            float portalDistance = startingRoom.window.portalSet.EntrancePortal.backwardsPortalMesh.transform.position.z;
            portalDistance = Mathf.Abs(portalDistance - transform.position.z);
            float closeDistance = 0.2f;
            /* Prevent the cameraDistance from placing the camera too close to the portal mesh */
            if(introCamDistance < portalDistance && introCamDistance > portalDistance - closeDistance) {
                introCamDistance = portalDistance - closeDistance;
            }

            /* If the cam distance reaches 0, have the player enter the standing state as they are done the intro animation */
            if(introCamDistance <= 0) {
                currentCameraTransform.position = camDestinationPos;
                currentCameraTransform.rotation = camDestinationRot;
                ChangeState(PlayerStates.Standing);
            }
        }
    }


    /* ----------- Event Functions ------------------------------------------------------------- */

    void ChangeState(PlayerStates newState) {
        /*
         * Change the player's current state to the given newState. Run certain lines if
         * certain states change into other specific states (fast falling > standing)
         */

        /* Dont change anything if the player is already in the new state */
        if(state != newState) {

            /* Going from FastFalling to a grounded state... */
            if(state == PlayerStates.FastFalling && StateIsGrounded(newState)) {
                /*... Will have the player undergo a hard landing. */
                if(outsideState) {
                    /* If the player is outside, a hard landing will play a normal landing sound */
                    playerStepTracker.PlayLanding(-currentYVelocity/maxYVelocity);
                }
                else {
                    /* Play the hard landing clip if the player did not escape yet */
                    playerStepTracker.PlayHardLanding();
                }
            }

            /* Going from an airborn state to a grounded state... */
            if(StateIsAirborn(state) && StateIsGrounded(newState)) {

                /*... Will inform the footstep tracker of the landing. */
                playerStepTracker.PlayLanding(-currentYVelocity/maxYVelocity);
            }

            /* Entering the Standing state... */
            if(newState == PlayerStates.Standing){
			
				/*... When leaving the Falling state... */
				if(state == PlayerStates.Falling){
					/*... Will lower the camera offset relative to the falling speed */
					cameraYOffset = -(headHeight + playerBodyLength/2f)*RatioWithinRange(0, (maxYVelocity), -currentYVelocity);

                }
				
				/*... When leaving the FastFalling state... */
				if(state == PlayerStates.FastFalling){
					/*... Causes a "hard fall", forcing the player into a landing animation. */
                	newState = PlayerStates.Landing;
                	cameraYOffset = 0;
                }
			}

            /* Exitting the LeavingIntro state will set certain values pertinent to leaving the intro */
            if(state == PlayerStates.LeavingIntro) {
                inMenu = false;
            }
			
			/* Entering the Falling state... */
			if(newState == PlayerStates.Falling){
				
				/*...When leaving the Landing state... */
				if(state == PlayerStates.Landing){
					//Should we set the camera height to its relative position in the landing animstion?
				}
			}
			
			/* Entering the FastFalling state... */
			if(newState == PlayerStates.FastFalling){

                /*... Will start playing the fastfalling audio (if the player is not outside) */
                if(!outsideState) { playerSoundsScript.PlayFastFall(); }
                
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
         * 
    	 */

        if(jumpPrimed == true && PlayerIsGrounded()) {
            jumpPrimed = false;
            ChangeState(PlayerStates.Falling);
            currentYVelocity = jumpSpeed;
        }
    }

    void LegCollisionTest(ref Vector3 position, ref Quaternion direction, ref float length, int index, ref DetectPlayerLegRay playerLegScript) {
        /*
         * Use the given values to send a ray trace of the player's leg and return the distance of the ray.
         * Update the arrays that track the status of the leg with the given index. If the given
         * index is -1, then do not update the array.
         * 
         * If the ray collides with an object, set playerLegScriptto the detected DetectPlayerLegRay script.
         * If no collision occurs or there was no script, set the playerLegScript to null.
         */

        /* Use the RayTrace function */
        float preLength = length;
        bool temp = false;
        lastHitCollider = null;

        /* Fire the ray and use the hit collider to update the playerLegScript */
        RayTrace(ref position, ref direction, ref length, ref temp, true, true, true);
        if(lastHitCollider != null && lastHitCollider.GetComponent<DetectPlayerLegRay>() != null) {
            playerLegScript = lastHitCollider.GetComponent<DetectPlayerLegRay>();
        }else {
            playerLegScript = null;
        }
		
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
         * 
         * When a leg hits an object, check if the object has a "DetectPlayerLegRay" script attached to it.
    	 */
        Vector3 upDirection = transform.up;
		Vector3 tempLegPos;
		Quaternion tempLegRotation;
		float tempLegLength;
        DetectPlayerLegRay[] legRayScripts = new DetectPlayerLegRay[extraLegs + 1];

        /* Test the first leg. It goes straight down relative to the player's center. */
        tempLegRotation = Quaternion.LookRotation(-transform.up, transform.forward);
        tempLegPos = transform.position;
        tempLegLength = currentLegLength + currentStepHeight;
        LegCollisionTest(ref tempLegPos, ref tempLegRotation, ref tempLegLength, 0, ref legRayScripts[0]);
		
		/* Test the collision for the other legs */
		for(int i = 1; i < extraLegLenths.Length; i++) {

            /* Check if nothing is blocking the space between the leg starting point and the player's center */
            float legGapDistance = legGap*playerBodyRadius;
			tempLegPos = transform.position;
			tempLegRotation = Quaternion.AngleAxis(i*(360/(extraLegLenths.Length-1)), upDirection)*transform.rotation;
			LegCollisionTest(ref tempLegPos, ref tempLegRotation, ref legGapDistance, -1, ref legRayScripts[i]);
			
			/* Fire the actual leg ray if there is nothing blocking the leg gab */
			if(legGapDistance == 0){
                /* Rotate the leg so it is rayTracing downward */
                tempLegRotation = Quaternion.LookRotation(tempLegRotation*-Vector3.up, tempLegRotation*Vector3.forward	);
				tempLegLength = currentLegLength + currentStepHeight;
				LegCollisionTest(ref tempLegPos, ref tempLegRotation, ref tempLegLength, i, ref legRayScripts[i]);
            }
			
			//Dont use this leg if the gap between the leg and the player is blocked
			else {
                extraLegLenths[i] = -1;
                legRayScripts[i] = null;
            }
		}

        /* Check each collider hit for a DetectPlayerLegRay script. Run it's PlayerStep command if it exists. */
        DetectLegRayCollisions(legRayScripts);
    }

    void DetectLegRayCollisions(DetectPlayerLegRay[] legRayScripts) {
        /*
         * Given an array of DetectPlayerLegRay, tell the scripts that the player has stepped on them.
         * Depending on each objectType of the script, have a different reaction.
         * 
         * If the stepIndex is already 1, dont bother checking the type and simply use the 1.
         */
        int stepIndex = currentStepType;

        if(currentStepType == 0) {
            for(int i = 0; i < legRayScripts.Length; i++) {
                if(legRayScripts[i] != null && legRayScripts[i].GetComponent<DetectPlayerLegRay>() != null) {

                    /* Run the script */
                    legRayScripts[i].GetComponent<DetectPlayerLegRay>().PlayerStep(gameObject);

                    /* Check the object type and react accordingly */
                    if(legRayScripts[i].GetComponent<DetectPlayerLegRay>().objectType == 0) {
                        stepIndex = Mathf.Max(stepIndex, legRayScripts[i].GetComponent<DetectPlayerLegRay>().returnValue);
                    }

                    else if(legRayScripts[i].GetComponent<DetectPlayerLegRay>().objectType == 2) {
                        /* If the player is falling fast enough, crack the glass */
                        if(currentYVelocity < -0.1 && legRayScripts[i].GetComponent<DetectPlayerLegRay>().returnValue != -1) {
                            legRayScripts[i].GetComponent<DetectPlayerLegRay>().CrackGlass();
                            playerSoundsScript.PlayWindowCrack();
                            //Maybe play a glass crack sound?
                        }
                    }
                }
            }
        }


        /* Update the step tracker with the new sound index to use */
        playerStepTracker.ChangeStepIndex(stepIndex);
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
            if(state == PlayerStates.FastFalling) {
                currentYVelocity -= gravity*Time.deltaTime*60*fastFallMod/5f;
                if(currentYVelocity < -maxYVelocity*fastFallMod) { currentYVelocity = -maxYVelocity*fastFallMod; }
            }

            else {
                currentYVelocity -= gravity*Time.deltaTime*60;
                if(currentYVelocity < -maxYVelocity) { currentYVelocity = -maxYVelocity; }
            }
            gravityVector = currentYVelocity*transform.up*gravityVectorMod;
        }

        /* Reset the player's yVelocity if they are grounded */
        else if(PlayerIsGrounded()) {
            currentYVelocity = 0;
        }

        /* Reset the player's yVelocity if they are in the intro */
        else if(PlayerIsInIntro()) {
            currentYVelocity = 0;
        }

        else {
            Debug.Log("WARNING: CURRENT STATE DOES NOT HANDLE GRAVITY VECTOR");
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
        float minOffset = -(headHeight + playerBodyLength/2f);
        float maxOffset = playerBodyLength/2f;
        if(cameraYOffset > maxOffset) {
            cameraYOffset = maxOffset;
        }
        
        else if(cameraYOffset < minOffset) {
            cameraYOffset  = minOffset;
        }

        /* If the offset is very small, snap it to 0 */
        if(cameraYOffset < 0.01 && cameraYOffset > -0.01) {
            cameraYOffset = 0;
        }

        /* Use the cameraYOffset to get the head's offset */
        headOffset = headHeight + cameraYOffset;

        /* Reduce the cameraYOffset once it gets used. If the offset is past a certain limit, reduce it faster. */
        float morphAmount = morphPercentage;
        if(cameraYOffset > 0) {
            morphAmount -= morphDiff*RatioWithinRange(0, maxOffset, cameraYOffset);
        }
        else if(cameraYOffset < 0) {
            morphAmount -= morphDiff*(1 - RatioWithinRange(minOffset, 0, cameraYOffset));
        }

        /* Apply the morph to the offset to make it return to 0 over a few frames */
        cameraYOffset *= morphAmount;
        
        return headOffset;
    }
    
    private void ArbitraryInput() {
        /*
         * Catch any inputs from the keyboard that are used for debugging or other uses.
         */
         
        /* If the player presses v, force them into the 90 degrees rotation. Used to land on the StartingRoom's window */
        if(Input.GetKeyDown("v")) {
            transform.eulerAngles = new Vector3(90, 0, 0);
        }

        /* Pressing certain keys will raise or lower a value of the player stats. The values being changed are: */
        /* maxYVelocity: change it to see if the player should fall any faster while outside. */
        if(Input.GetKeyDown("-")) {
            maxYVelocity -= 0.2f;
        }
        if(Input.GetKeyDown("+")) {
            maxYVelocity += 0.2f;
        }
        /* fastFallMod: change how fast the player speeds up when fast falling. Maybe use a new value for outside? */
        if(Input.GetKeyDown("9")) {
            fastFallMod -= 1;
        }
        if(Input.GetKeyDown("0")) {
            fastFallMod += 1;
        }

        /* If the player presses y, force the player into fastFall if they are falling */
        if(Input.GetKeyDown("y")) {
            if(state == PlayerStates.Falling) {
                ChangeState(PlayerStates.FastFalling);
            }
        }

        /* Pressing the K key will "Upgrade" the current music */
        if(Input.GetKeyDown("y")) {
            playerSoundsScript.UpgradeMusic();
        }
    }

    void MenuKey() {
        /*
         * Handle the act of sending requests to the menu about opening or closing the menu
         */
         
        if(Input.GetKeyDown(KeyCode.Escape)) {
            
            /* Pressing escape during the startup will skip into the main menu */
            if(playerMenu.state == MenuStates.Startup) {
                playerMenu.PlayerRequestMenuChange();
            }
            
            /* Pressing escape will skip the intro */
            else if((aboutToLeaveIntro && state == PlayerStates.InIntro) || state == PlayerStates.LeavingIntro) {
                /* Make sure the state properly exits the intro */
                if(state == PlayerStates.InIntro) {
                    remainingInIntroTime = 0;
                    ChangeState(PlayerStates.LeavingIntro);
                }
                introCamDistance = 0;
                /* Ensure the camera ray is fired so certain functions are run (PlayerRenderTerrain) */
                FireCameraRayInIntroState();

                /* If the intro music has not yet started, force it to start */
                playerSoundsScript.ForceIntroMusic();
            }
            
            else {
                /* Set whether the menu should prevent user input or not */
                inMenu = playerMenu.PlayerRequestMenuChange();
            }
        }
    }
    
    public void StartButtonPressed() {
        /*
         * This is run when the player presses the start button on the main menu in the intro
         */

        /* Start a timer for when we will leave the inMenu state */
        aboutToLeaveIntro = true;
        remainingInIntroTime = timeToLeaveIntro;
        
        /* Set the startingRoom's outside window to be in the terrain layer so we can render it later */
        startingRoom.window.outsideWindowContainer.layer = PortalSet.maxLayer + 2;
        for(int i = 0; i < startingRoom.window.outsideWindowContainer.transform.childCount; i++) {
            startingRoom.window.outsideWindowContainer.transform.GetChild(i).gameObject.layer = PortalSet.maxLayer + 2;
        }
        startingRoom.window.portalSet.ExitPortal.backwardsPortalMesh.layer = PortalSet.maxLayer + 2;

        /* Hide and lock the cursor since the player will regain control */
        Cursor.lockState = CursorLockMode.Locked;

        /* Play the starting music for the game */
        playerSoundsScript.PlayIntroMusic();
    }

    public void ContinueButtonPressed() {
        /*
         * This is run when the player presses the continue button on the main menu
         */

        inMenu = false;

        /* Hide and lock the cursor since the player will regain control */
        Cursor.lockState = CursorLockMode.Locked;
    }
    
    public void EnteredOutside() {
        /*
         * This is called (multiple times) when the player first enters the outside state.
         * Changes certain values of the player controller to change how it plays.
         */

        /* When outside, give the player's fastFall a slow speed increases but high max velocity */
        outsideState = true;
        fastFallMod = fastFallModOutside;
        maxYVelocity = 1.5f;

        /* Now the player requires all legs to be grounded to be considered standing */
        requiredGroundedCount = extraLegs;

        /* The player's steps will now be stepping on soft ground */
        currentStepType = 1;
    }

    public void ApplyFastfall(bool outsidePuzzlePlayArea) {
        /* 
         * Put the player into the FastFalling state if they meet the appropriate conditions:
         * - In an airborn state that is not the fastFalling state
         * - Player's current velocity is nearing a specified value relative to their max
         * 
         * The given boolean will be true if the function is called from the player being 
         * outside the puzzleRoom's playArea. This will control whether the outsideState value
         * must be true. This means the player will only ever enter FastFall state if
         * they are either outside the puzzleRoom's play area or outside in the terrain.
         */
         
        /* Check if the player is in the right state */
        if(PlayerIsAirborn() && state != PlayerStates.FastFalling && (outsideState || outsidePuzzlePlayArea)) {

            /* The required falling speed is determined by the player's current state */
            float minimumSpeed = maxYVelocity;
            if(outsidePuzzlePlayArea) {

                /* Having the correct escape angle requires little speed to enter fastFall */
                if(transform.up == new Vector3(0, 0, 1)) {
                    minimumSpeed *= 0.5f;
                }
                /* having the incorrect escape angle requires nearly max speed to enter fastFall */
                else {
                    minimumSpeed *= 0.95f;
                }

                /* Check if they are falling at the correct speed */
                if(Mathf.Abs(currentYVelocity) > minimumSpeed) {
                    ChangeState(PlayerStates.FastFalling);
                }
            }
        }
    }


    /* ----------- Outside Called Functions ------------------------------------------------------------- */

    public float GetYVelocityFastFallRatio() {
        /*
         * A very specific equaition to get a very specific value used with camera post-processing effects.
         * It returns the ratio of the player's velocity as it approaches it's fastFalling terminal velocity.
         * Value ranges between [0, 1]. 0 being <= max Falling velocity. 1 being >= max FastFalling velocity 
         */

        return RatioWithinRange(maxYVelocity, maxYVelocity*fastFallMod, -currentYVelocity);
    }

    public void ChangeLastRoom(AttachedRoom newRoom) {
        /*
         * Called by an attachedRoom when the player enters, it changes the player's last room to the given one.
         * Also update their saved rotation value when they enter the room.
         */

        lastRoom = newRoom;

        /* If the player is right-side up, do not use it's Y axis (This ensures they reset facing forward) */
        savedRotation = transform.rotation;
        if(savedRotation.eulerAngles.x == 0 && savedRotation.eulerAngles.z == 0) {
            savedRotation = Quaternion.Euler(0, 0, 0);
        }
    }
    
    public void StartFallingOutside() {
        /*
         * Called when the player has entered a state where they are expected to fall out 
         * of the startingRoom's window. 
         */

        /* Set the fallingOutWindow to true  */
        fallingOutWindow = true;
    }
    
    public void PlayClickSound() {
        /*
         * Play a click sound for the menu
         */

        /* Play the menu sound clicking clip */
        playerSoundsScript.PlayMenuClick();
    }


    /* ----------- Helper Functions ------------------------------------------------------------- */

    Quaternion RayTrace(ref Vector3 position, ref Quaternion rotation, ref float distance, 
        ref bool teleported, bool detectTeleportTriggers, bool detectOtherColliders, bool saveCollider) {
        /*
         * Fire a ray from the given position with the given rotation forwards for the given distance.
         * The quaternion returned represents the amount of rotation that the given rotation underwent.
         * 
         * detectTeleportTriggers as true will cause any collision with a teleportTrigger to teleport 
         * the position and rotation to the hit trigger's partner using it's teleportParameters function.
         * teleportTrigger Collisions with detectTeleportTriggers as false will be ignored.
         * 
         * detectOtherColliders as true will cause any collision with any non-teleporter trigger to cause
         * the position to stop at the point of collision and reduce the distance amount respectively.
         * these type of collisions will be ignored if detectOtherColliders is false.
         * 
         * The teleported reference will be set to false if no teleporter was encountered. It will be
         * set to true if it collides with a teleporter and moves the position and rotation.
         * 
         * If saveCollider is set to true, then colliding with a non-teleporter collider will update the 
         * global lastHitCollider to be the collider that was hit. Do not change lastHitCollider if nothing was hit.
         */
        Quaternion totalRotation = Quaternion.identity;
        Quaternion rotationDifference;
        RaycastHit hitInfo = new RaycastHit();
        bool stopRayTrace = false;
        LayerMask rayLayerMask = 0;
        teleported = false;

        /* Include teleport triggers into the layerMask */
        if(detectTeleportTriggers) {rayLayerMask = rayLayerMask | (1 << 8);}
        /* Include all colliders into the layerMask. Assume all colliders use the "Default" layer.  */
        if(detectOtherColliders) { rayLayerMask = rayLayerMask | (1 << 0); }
        /* Include terrain to stop the player when outside */
        rayLayerMask = rayLayerMask | (1 << (PortalSet.maxLayer + 2));

        /* Travel towards the rotation's forward for the remaining distance */
        while(distance > 0 && stopRayTrace == false) {
            
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
                }

                /* Hitting a solid collider will stop the rayTrace where it currently is */
                else if(!hitInfo.collider.isTrigger) {
                    stopRayTrace = true;

                    /* Save the collider hit if needed */
                    if(saveCollider) {
                        lastHitCollider = hitInfo.collider;
                    }
                }

                /* non-teleport triggers that are hit will signal an error. All triggers should be on the IgnoreRaycastLayer. */
                else if(hitInfo.collider.isTrigger) {
                    /* Warn the player that they hit an unknown trigger */
                    Debug.Log("WARNING: Player's RayCast hit an unknown trigger");
                    stopRayTrace = true;
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
    
    bool StateIsGrounded(PlayerStates givenState){
        /*
    	 * Return true if the player is in a grounded state with their legs linked to an object
    	 */
        bool isGrounded = false;

        if(givenState == PlayerStates.Standing || givenState == PlayerStates.Landing) {
            isGrounded = true;
        }

        return isGrounded;
    }

    bool StateIsAirborn(PlayerStates givenState) {
        /* 
    	 * Return true if the given state is airborn, with their legs not connected to an object
    	 */
        bool isFalling = false;

        if(givenState == PlayerStates.Falling || givenState == PlayerStates.FastFalling) {
            isFalling = true;
        }

        return isFalling;
    }

    bool StateIsIntro(PlayerStates givenState) {
        /*
         * Return true if the player is in a "intro" state.
         */
        bool inIntro = false;

        if(givenState == PlayerStates.LoadingIntro || 
                givenState == PlayerStates.LeavingIntro || 
                givenState == PlayerStates.InIntro) {
            inIntro = true;
        }

        return inIntro;
    }

    bool PlayerIsGrounded() {
        /*
         * Return true of the player is in the grounded state.
         */

        return StateIsGrounded(state);
    }

    bool PlayerIsAirborn(){
    	/*
    	 * Return true if the player is in a freefall state/legs do not connect to an object
    	 */

    	return StateIsAirborn(state);
    }
    
    bool PlayerIsInIntro() {
        /*
         * Return true if the player is in one of the "intro" states
         */

        return StateIsIntro(state);
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

    public void PlayerRenderTerrain(bool renderTerrain) {
        /*
         * Calling this function will change the culling mask of the player's camera.
         * True will have the camera render only the terrain layer layers. 
         * False will render all but the terrain layer.
         * 
         * Do not change the player's clipping planes as the camera will still be very close to 
         * the outside window while they start entering the outside.
         */

        if(renderTerrain) {
            playerCamera.cullingMask = 1 << PortalSet.maxLayer + 2;
            //playerCamera.nearClipPlane = cameraNearClippingPlane;
            playerCamera.farClipPlane = cameraFarClippingPlane;
        }
        else {
            playerCamera.cullingMask = ~(1 << PortalSet.maxLayer + 2);
            //playerCamera.nearClipPlane = cameraNearClippingPlane;
            playerCamera.farClipPlane = cameraFarClippingPlane;
        }
    }
}
