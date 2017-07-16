using UnityEngine;
using System.Collections;

/*
 * A custom character controller that uses UserInputs to handle movement. It uses "legs" to keep
 * it's "body" above the floor, letting the player walk up and down stairs or slopes smoothly. 
 */
public class CustomPlayerController : MonoBehaviour {

    /* The UserInputs object linked to this player */
    private UserInputs inputs;

    /* The expected position of the camera */
    public Transform restingCameraTransform;

    /* The current position of the camera. Smoothly changes to restingCameraTransform each frame */
    public Transform currentCameraTransform;

    /* The camera used for the player's view */
    public Camera playerCamera;

    /* The viewing angle of the player's camera */
    private float xRotation;
    private float yRotation;


    /* The direction and magnitude of player input */
    private Vector3 inputVector = Vector3.zero;

    /* Sliding determines how much of getAxis should be used over getAxisRaw. */
    [Range(1, 0)]
    public float sliding;

    /* How fast a player moves using player inputs */
    public float movementSpeed;
    public float runSpeedMultiplier;

    /* How fast a player accelerates towards their feet when falling. */
    public float gravity;

    /* How fast a player travels upward when they jump */
    public float jumpSpeed;

    /* The Y velocity of the player along with its max(positive) */
    public float currentYVelocity;
    public float maxYVelocity;

    /* Used to determine the state of the jump. If true, the next jump opportunity will cause the player to jump. */
    private bool jumpPrimed;
    /* The state of the jump key on the current and previous frame. true = pressed */
    private bool jumpKeyPrevious = false;
    private bool jumpKeyCurrent = false;

    /* The sizes of the player's capsule collider */
    public float playerBodyLength;
    public float playerBodyRadius;

    /* Percentage of player radius that is used to sepperate the legs from the player's center */
    [Range(1, 0)]
    public float legGap;

    /* How much distance will be between the player's collider and the floor */
    public float playerLegLength;
    private float currentLegLength;

    /* The length of the player's leg at this frame */
    private float expectedLegLength;

    /* How low a player can step down for them to snap to the ground */
    public float maxStepHeight;
    private float currentStepHeight;

    /* How many extra feet are used when handling ground checks */
    public int extraFeet;

    /* The position of the player's foot. The player body will always try to be playerLegLength above this point. */
    private Vector3 currentFootPosition;

    /* The length of each leg of the player */
    private float[] extraLegLenths;

    /* If the player is falling with gravity or standing with their legs */
    private bool falling = false;

    /* The Y distance that the camera is from it's resting position while the player is in control */
    public float cameraYOffset;
    /* How fast currentCameraTransform morphs to restingCameraTransform each frame, in percentage. */
    [Range(1, 0)]
    public float morphPercentage;


    /* -------------- Built-in Unity Functions ---------------------------------------------------------- */

    void Start() {
        /*
         * Set the values of the player model to be equal to the values set in the script
         */

      /* Create the UserInputs object linked to this player */
      inputs = new UserInputs();

        /* Initilize the leg lengths */
        extraLegLenths = new float[extraFeet + 1];

        /* Put the starting foot position at the base of the player model */
        currentFootPosition = transform.TransformPoint(new Vector3(0, -GetComponent<CapsuleCollider>().height/2, 0));

        /* Adjust the player's height and width */
        GetComponent<CapsuleCollider>().height = playerBodyLength;
        GetComponent<CapsuleCollider>().radius = playerBodyRadius;

        /* Adjust the player model's position to reflect the player's leg length */
        transform.position = currentFootPosition;
        transform.localPosition += new Vector3(0, playerBodyLength/2f + playerLegLength, 0);
    }

    void Update() {
        /*
         * Handle any player inputs. If they need to be redirected to a new script,
         * send the input signals to the current overriddenScript.
         */

        /* Update the inputs of the player */
        inputs.UpdateInputs();
        PlayerInControl();

        //Fire a long ray
        Vector3 pos = new Vector3(0, 1, 0);
        Vector3 dir = new Vector3(0, 0, 1);
        float dis = 10f;
        Quaternion rot = Quaternion.identity;
        //rot = new Quaternion();
        RayTrace(ref pos, ref dir, ref rot, ref dis, true, true);

    }


    /* ----------------- Update Functions ------------------------------------------------------------- */

    void PlayerInControl() {
        /*
         * Handle the inputs of the user and the movement of the player when they are in control
         */

        /* Use mouse input values to rotate the camera */
        RotateCamera();

        /* Update the player's jumping conditions */
        JumpingCondition();

        /* Change the player's leg lengths depending on their state */
        UpdateLegLengths();

        /* Get an input vector that is relative to the player's rotation and takes into account the player's speed */
        UpdateInputVector();

        /* Find the footPosition of the player and check if they are falling or standing */
        StepPlayer();

        /* Apply the movement to the player from taking steps, inputting directions and falling from gravity. */
        MovePlayer();

        /* Adjust the camera's position now that the player has moved */
        AdjustCameraPosition();
    }

    void RotateCamera() {
        /*
         * Use the user's mouse movement to rotate the player's camera
         */

        /* Ensure the X rotation does not overflow */
        xRotation -= inputs.mouseX;
        if(xRotation < 0) { xRotation += 360; }
        else if(xRotation > 360) { xRotation -= 360; }

        /* Prevent the Y rotation from rotating too high or low */
        yRotation += inputs.mouseY;
        yRotation = Mathf.Clamp(yRotation, -75, 75);

        /* Apply the rotation to the camera's currentCameraTransform */
        currentCameraTransform.transform.localEulerAngles = new Vector3(-yRotation, -xRotation, 0);
        restingCameraTransform.transform.localEulerAngles = new Vector3(0, -xRotation, 0);

        /* Update the camera's position with the new currentCameraTransform */
        playerCamera.transform.rotation = currentCameraTransform.transform.rotation;
    }

    void JumpingCondition() {
        /*
    	 * Check if the player is holding the jump key. The player jumps when they release the key.
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
         * If the player is standing, keep their leg length to it's expected amount.
         * If the player is falling, give them short legs.
         * If the player is falling, but is travelling against gravity, given them very short leg lengths.
         */

        if(falling == false) {
            currentLegLength = playerLegLength;
            currentStepHeight = maxStepHeight;
        }
        else if(currentYVelocity < 0) {
            currentLegLength = playerLegLength*0.5f;
            currentStepHeight = maxStepHeight*0.5f;
        }
        else {
            currentLegLength = playerLegLength*0.1f;
            currentStepHeight = maxStepHeight*0.1f;
        }
    }

    public void UpdateInputVector() {

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

        /* Rotate the input direction to match the player's view. Only use the view's rotation along the Y axis */
        inputVector = restingCameraTransform.rotation*inputVector;
    }

    public void StepPlayer() {
        /*
         * Use the given inputVector to move the player in the proper direction and use the given
         * fixedPlayerView as the player's rotation to keep Vector.Up/Foward relative to the player.
         * 
         * To determine if the player has taken a step down or up, compare a rayTrace taken before this frame and 
         * a rayTrace taken at this frame. If their legLenths are different, then a step will be taken.
         * 
         * If the currentLegLength rayTrace does not connect to the floor, the player will undergo the effects
         * of gravity instead of taking a step. When under the effects of graivty, the previous step is ignored.
         */
        Vector3 upDirection = transform.rotation*Vector3.up;
        Vector3 forwardVector = transform.rotation*Vector3.forward;
        Vector3 tempForwardVector = Vector3.zero; ;

        /* Update the currentlegLength values for the legs that form a circle around the player */
        LegCollisionTest(transform.position - upDirection*playerBodyLength/2.5f, -upDirection, currentLegLength+currentStepHeight, 0);
        for(int i = 1; i < extraLegLenths.Length; i++) {
            tempForwardVector = Quaternion.AngleAxis(i*(360/(extraLegLenths.Length-1)), upDirection)*forwardVector;
            LegCollisionTest(transform.position + tempForwardVector*legGap*playerBodyRadius - upDirection*playerBodyLength/2.5f, -upDirection, currentLegLength+currentStepHeight, i);
        }

        /* Get how many legs are touching an object */
        int standingCount = 0;
        for(int i = 0; i < extraLegLenths.Length; i++) {
            if(extraLegLenths[i] >= 0) {
                standingCount++;
            }
        }

        /* If enough legs are touching an object, the player is considered "standing" */
        int requiredCount = 1;
        if(standingCount >= requiredCount) {
            falling = false;
            currentYVelocity = 0;
        }
        else {
            /* Attempt to consume a jump if the player was standing but will now start falling */
            JumpAttempt();
            falling = true;
        }

        /* If the player is standing, check if they have taken a step */
        if(falling == false) {

            /* Calculate the current foot position of the player by finding the expectedLegLength */
            expectedLegLength = 0;
            for(int i = 0; i < extraLegLenths.Length; i++) {
                if(extraLegLenths[i] >= 0) {
                    expectedLegLength += extraLegLenths[i];
                }
            }
            expectedLegLength /= standingCount;
            currentFootPosition = transform.position - upDirection*(playerBodyLength/2f + expectedLegLength);
        }
    }

    public void MovePlayer() {
        /*
         * IDEA: move the player and then do a raytrace to detect if they will need to be teleported.
         * Do the raytrace from their camera resting positon. To check for flooring, 
         * do a raytrace from their camera resting position to the leg start and then
         * start the leg raytracing.
         * 
         * To do this, we can follow these steps:
         * Save the current positon.
         * Move the given distance.
         * Ray trace from original position to ray traced position. Hitting a teleporter will teleport player
         * 
         * 
         * 
         * 
         * 
         * Move the player relative to what has occured this frame so far, such as any steps taken
         * or if the player should be falling. 
         * 
         * Step movements are done by setting the  player's position 
         * relative to their foot position and saved legLength for this frame.
         * 
         * Gravity is determined by tracking the upward/downward velocity of the player and whether they are falling.
         */
        Vector3 upDirection = transform.rotation*Vector3.up;
        Vector3 gravityVector = Vector3.zero;

        /* If the player is standing, position their body relative to their foot position and the length of their legs.
         * Any distance travelled up or down from a step will not be applied to the player's camera. */
        if(falling == false) {
            /* Check if moving the player to their proper footing pushes them through a portal */
            MovePlayer(-transform.position + currentFootPosition + upDirection*(playerBodyLength/2f + currentLegLength));
            //transform.position = currentFootPosition + upDirection*(playerBodyLength/2f + currentLegLength);
            currentCameraTransform.transform.position -= upDirection*(currentLegLength - expectedLegLength);

        }


        /* If the player is falling, apply gravity to their yVelocity. Reset yVelocity if they are standing. */
        if(falling == true) {
            currentYVelocity -= gravity*Time.deltaTime*60;
            /* Prevent the player from falling faster than terminal velocity */
            if(currentYVelocity < -maxYVelocity) {
                currentYVelocity = -maxYVelocity;
            }
            gravityVector = currentYVelocity*upDirection;
        }
        else {
            currentYVelocity = 0;
        }

        /* Apply the movement of the players input */
        GetComponent<Rigidbody>().velocity = Vector3.zero;
        //GetComponent<Rigidbody>().MovePosition(transform.position + gravityVector + (inputVector)*Time.deltaTime*60);
        /* Check if the player has passed through a portal */
        MovePlayer(gravityVector + (inputVector)*Time.deltaTime*60);
        //transform.position = transform.position + gravityVector + (inputVector)*Time.deltaTime*60;


    }


    void AdjustCameraPosition() {
        /*
         * Move the currentCameraTransform towards restingCameraTransform.
         */
        float minimumPositionDifference = 0.01f;
        float maximumPositionDifference = playerBodyLength/3f;

        /* Get the position of the camera and derive a ray to go from the player' transform to the camera */
        Vector3 cameraPosition = restingCameraTransform.position + transform.up*cameraYOffset;
        Vector3 position = transform.position;
        Vector3 direction = (cameraPosition - position).normalized;
        Quaternion rotation = Quaternion.identity;
        float distance = (cameraPosition - position).magnitude;

        /* Fire a ray from the player's center to the camera's new position, respecting teleport triggers and colliders */
        RayTrace(ref position, ref direction, ref rotation, ref distance, true, true);

        /* Update the camera's position once the ray trace is done */
        currentCameraTransform.position = position;
        currentCameraTransform.rotation = rotation;



        /* Reduce the cameraYOffset so the camera slides towards it's resting position */
        if(Mathf.Abs(cameraYOffset) < minimumPositionDifference) {
            cameraYOffset = 0;
        }else if(Mathf.Abs(cameraYOffset) > maximumPositionDifference) {
            cameraYOffset = Mathf.Sign(cameraYOffset)*maximumPositionDifference;
        }else {
            cameraYOffset *= morphPercentage;
        }

        //different idea: find the difference with the new direction and apply it instead
        //Debug.Log(direction);
        /* Place the player camera using currentCameraTransform */
        playerCamera.transform.position = currentCameraTransform.position;
        playerCamera.transform.rotation *= currentCameraTransform.rotation;
    }

    
    /* ----------- Event Functions ------------------------------------------------------------- */

    void LegCollisionTest(Vector3 position, Vector3 direction, float length, int index) {
        /*
         * Use the given values to send a ray trace of the player's leg and return the distance of the ray.
         * Update the arrays that track the status of the leg with the given index.
         */
        RaycastHit hitInfo = new RaycastHit();
        Ray bodyToFeet = new Ray(position, direction);

        if(Physics.Raycast(bodyToFeet, out hitInfo, length)) {
            extraLegLenths[index] = hitInfo.distance;
            ///* Draw the point for reference */
            //Debug.DrawLine(
            //    position,
            //    position + direction*(currentLegLength+currentStepHeight),
            //    col);
        }
        else {
            extraLegLenths[index] = -1;
        }
    }

    void JumpAttempt() {
        /*
    	 * Try to make the player jump. A jump must be primed (jumpPrimed == true) for the player to jump.
    	 */

        if(jumpPrimed == true && falling == false) {
            jumpPrimed = false;
            falling = true;
            currentYVelocity = jumpSpeed;
        }
    }


    void MovePlayer(Vector3 movementVector) {
        /*
         * Use the given movementVector to move the player's body by the given amount.
         */

        /* Set values used to track the player's current transform state */
        float remainingDistance = (movementVector).magnitude;
        Vector3 currentPosition = transform.position;
        Vector3 currentDirection = (movementVector).normalized;
        Quaternion currentRotation = transform.rotation;

        /* Fire a ray from the player model's transform using the given movement vector to find their next position */
        RayTrace(ref currentPosition, ref currentDirection, ref currentRotation, ref remainingDistance, true, true);

        /* Update the player's transform once the ray is done being fired */
        transform.position = currentPosition;
        transform.rotation = currentRotation;
    }


    
    /* ----------- Helper Functions ------------------------------------------------------------- */

    void RayTrace(ref Vector3 position, ref Vector3 direction, ref Quaternion totalRotation, 
            ref float distance, bool detectTeleportTriggers, bool detectOtherColliders) {
        /*
         * Fire a ray from the given position towards the given direction for the given length. The given
         * parameters determines what kind of collisions will be detected.
         * 
         * If it collides with any teleport trigger, teleport it's position and properly
         * rotate it's direction and totalRotation.
         * 
         * If it collides into a solid collider, have distance be set to the remaining distance not travelled. 
         * 
         * 
         * 
         * NOTE: ONCE A RAY TELEPORTS, WE NEED TO IGNORE THE COLLIDER THAT IT WAS TELEPORTED TO    .
         * Actually, we cannot ignore the collider in the scenario that the ray stops the moment it teleports.
         * What we should do instead is once it teleports, clamp it to be touching the partnerCollider, 
         * so that it will be inside of it and cannot ray trace to hit it.
         * 
         * Hold on, when the ray gets teleported, it should always be on the outside of the mesh. the ray
         * should be at the edge of the mesh it got teleported to   
         */
        RaycastHit hitInfo = new RaycastHit();
        bool hitSolidCollider = false;
        GameObject partnerCollider = null;

        /* Change what the ray will collide with using the given parameters. Default is no colliders */
        LayerMask rayLayerMask = 0;
        if(detectTeleportTriggers) {
            /* Include teleport triggers into the layerMask */
            rayLayerMask = rayLayerMask | (1 << LayerMask.NameToLayer("Portal Trigger"));
        }
        if(detectOtherColliders) {
            /* Include all non-teleporter triggers colliders into the layerMask */
            rayLayerMask = rayLayerMask | ~(1 << LayerMask.NameToLayer("Portal Trigger"));
        }


        //Debug.Log("start  " + distance);
        /* Travel towards the direction for the remaining distance */
        while(distance > 0 && hitSolidCollider == false) {
            //reduce the distance every loop to prevent infinite loops
            distance -= 0.001f;

            /* Check for any collisions from the current position towards the current direction */
            if(Physics.Raycast(position, direction, out hitInfo, distance, rayLayerMask)) {

                /* When hitting a collider, move the position up to the collision point */
                Debug.DrawLine(position, position + direction*hitInfo.distance);
                position += direction*hitInfo.distance;
                distance -= hitInfo.distance;


                //Debug.Log("hit...");
                /* Hitting a teleport trigger will teleport the current position, direction and update the rotation */
                if(hitInfo.collider.GetComponent<TeleporterTrigger>() != null) {

                    //FOR NOW DO NOT TELEPORT AGAIN AFTER TELEPORTING ONCE
                    rayLayerMask = 0;
                    hitInfo.collider.GetComponent<TeleporterTrigger>().TeleportParameters(ref position, ref direction, ref totalRotation);
                    
                    //Debug.Log("hit tele");
                }
                /* Hitting a solid collider will stop the ray where it currently is */
                else if(!hitInfo.collider.isTrigger) {
                    hitSolidCollider = true;
                    //Debug.Log("hit wall");
                }
                /* non-teleport triggers will be ignored */
                else if(hitInfo.collider.isTrigger) {
                    //Debug.Log("hit something?");
                }
            }
            else {

                /* The raytrace hit nothing, so travel along the direction for the remaining distance */
                Debug.DrawLine(position, position + direction*distance);
                position += direction*distance;
                distance = 0;
                //Debug.Log("kept going");
            }
        }
    }
}
