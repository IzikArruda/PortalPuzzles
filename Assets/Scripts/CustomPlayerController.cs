using UnityEngine;
using System.Collections;

/*
 * A custom character controller that uses UserInputs to handle movement. It uses "legs" to keep
 * it's "body" above the floor, letting the player walk up and down stairs or slopes smoothly. 
 */
public class CustomPlayerController : MonoBehaviour {

    /* The UserInputs object linked to this player */
    private UserInputs inputs;

    /* The current position of the camera. Smoothly changes to restingCameraTransform each frame */
    /* The default transform of the camera. */
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

    //Default (0, 1, 0)
    public Vector3 givenPosition;
    //Default (0, 0, 1)
    public Vector3 givenDirection;
    //Default (0, 1, 0)
    public Vector3 givenUp;

    public float headHeight;

    public float cameraXRotation;
    public float cameraYRotation;

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
        //Vector3 pos = new Vector3(0, 1, 0);
        //Vector3 forw = new Vector3(0, 0, 1);
        //Vector3 upV = new Vector3(0, 1, 0);
        //Quaternion dir = Quaternion.LookRotation(forw, upV);
        //rot = new Quaternion();
        //RayTrace(ref pos, ref dir, ref dis, true, true);

        Vector3 pos = givenPosition;
        Quaternion givenRotation = Quaternion.LookRotation(givenDirection, givenUp);
        float dis = 10f;
        RayTrace(ref pos, ref givenRotation, ref dis, true, true);

        //Draw a line in the camera's forward vector
        Debug.DrawLine(playerCamera.transform.position, playerCamera.transform.position + playerCamera.transform.rotation*Vector3.forward*0.5f, Color.green);
    }


    /* ----------------- Update Functions ------------------------------------------------------------- */

    void PlayerInControl() {
        /*
         * Handle the inputs of the user and the movement of the player when they are in control
         */

        /* Use mouse input values to rotate the camera */
        //

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
        Vector3 u = currentCameraTransform.up;
        Vector3 r = currentCameraTransform.right;
        currentCameraTransform.rotation *= Quaternion.Euler(-cameraYRotation, -cameraXRotation, 0);
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
        inputVector = Quaternion.AngleAxis(-cameraXRotation, transform.up)*inputVector;
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
        Vector3 upDirection = transform.up;
        Vector3 gravityVector = Vector3.zero;

        /* If the player is standing, position their body relative to their foot position and the length of their legs.
         * Any distance travelled up or down from a step will be undone to the player's camera. */
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
         * Position and rotate the player camera according to their inputs.
         * The camera is positionned headHeight above the player origin.
         * 
         * The camera's position and rotation is calculated by firing a RayTrace command from the player origin
         * upwards (relative to the player) to the expected view position. The RayTrace will collide with walls
         * and will teleport from triggers, letting the player's "head" pass through portals without their "body".
         */
        Quaternion toCamRotation;
        Quaternion rotationDifference;
        Vector3 playerOrigin;
        float playerCameraHeight;

        /* Copy the player's transform to the camera as we take in user inputs to calculate it's facing direction */
        currentCameraTransform.position = transform.position;
        currentCameraTransform.rotation = transform.rotation;
        RotateCamera();

        /* Apply a rayTrace from the player's origin to the camera's position that is effected by teleport triggers */
        playerOrigin = transform.position;
        toCamRotation = Quaternion.LookRotation(transform.up, transform.forward);
        playerCameraHeight = headHeight;
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
        float remainingDistance = movementVector.magnitude;
        Vector3 position = transform.position;
        //The up vector doesnt mater i believe
        Quaternion direction = Quaternion.LookRotation(movementVector.normalized, transform.up);


        /* Fire a ray from the player model's transform using the given movement vector to find their next position */
        RayTrace(ref position, ref direction, ref remainingDistance, true, true);
        /* Get the difference in the rotation */
        Quaternion rotationDifference = Quaternion.Inverse(Quaternion.LookRotation(movementVector.normalized, transform.up)) * direction;

        /* Update the player's?]?? transform once the ray is done being fired */
        transform.position = position;
        transform.rotation *= rotationDifference;
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
            distance -= 0.001f;

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
                    Debug.Log("hit tele");
                }

                /* Hitting a solid collider will stop the rayTrace where it currently is */
                else if(!hitInfo.collider.isTrigger) {
                    stopRayTrace = true;
                    Debug.Log("hit wall");
                }

                /* non-teleport triggers will be ignored */
                else if(hitInfo.collider.isTrigger) {
                    Debug.Log("hit something?");
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
}
