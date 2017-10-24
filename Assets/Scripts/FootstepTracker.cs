using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
 * Track the player's footsteps by retaining the player's last set of moves and calculating whether
 * they have travelled enough distance to warrent playing a footstep sound effect.
 */
public class FootstepTracker : MonoBehaviour {

    /* The sound script that will be used to play the footsteps */
    private PlayerSounds playerSoundsScript;

    /* --- Player stats Values ------------------- */
    private float playerMovementSpeed;
    private float playerLegLength;

    /* --- Stride Values ------------------- */
    /* The current distance the player has moved since the last footstep effect was played */
    private float currentHorizontalStride;
    private float currentVerticalStride;
    /* How far the player can move before a footstep sound should play. */
    public float maxHorizontalStride;
    public float maxVerticalStride;

    /* --- User Inputted Values ------------------- */
    /* Modifies the required horizontal distance to play a footstep. Relative to playerMovementSpeed */
    public float horiStrideMod = 40;
    /* Modifies the required vertical distance to play a footstep. Relative to playerLegLength */
    public float vertStrideMod = 0.9f;


    /* --- Footstep Tracker ------------------- */
    /* How many past directions are tracked */
    public float lookbackCount;
    /* A list of past movement directions to calculate the average */
    private List<Vector3> pastDirections = new List<Vector3>();
    /* The time between the last footstep effect for a given foot */
    public float[] timeSinceStep;


    /* ----------- Set-up Functions ------------------------------------------------------------- */

    public void Start() {

        /* Setup the step times array to track each foot's timing */
        timeSinceStep = new float[2];
        ResetFootTiming();
    }
    
    public void CalculateStrideDistances(float movementSpeed, float legLength) {
        /*
         * Calculate the max stride distances, which represents how far 
         * a player will travel before a footstep effect will play.  
         */
        playerMovementSpeed = movementSpeed;
        playerLegLength = legLength;
        maxHorizontalStride = playerMovementSpeed*horiStrideMod;
        maxVerticalStride = playerLegLength*vertStrideMod;
    }

    public void SetSoundsScript(PlayerSounds givenSoundsScript) {
        /*
         * Use the given script as the playerSounds script that will play the footstep sounds.
         */

        playerSoundsScript = givenSoundsScript;
    }


    /* ----------- Step/Stride Functions ------------------------------------------------------------- */

    public void UpdateStride() {
        /*
         * Check the player's current stride values to determine if a footstep sound effect should play.
         * This is run everytime the player's Update function runs while in a grounded state.
    	 */
        //Draw a ray of the avg direction
        Debug.DrawRay(transform.position, AverageStepDirection()*10, Color.red);

        /* Update the footstep timing values */
        for(int i = 0; i < timeSinceStep.Length; i++) {
            timeSinceStep[i] += Time.deltaTime;
        }

        /* Check whether a footstep sound effect should be played by comparing the current stride progress */
        if(Mathf.Abs(currentVerticalStride) >= maxVerticalStride || Mathf.Abs(currentHorizontalStride) >= maxHorizontalStride) {
            PlayStep();
        }
    }

    public void AddHorizontalStep(Vector3 horizontalInputDirection) {
        /*
         * Update the horizontal (x, z) stride distance and the pastDirections array with a new
         * direction. This will be run every frame whenever the player is in a grounded state.
         *
         * If the player is moving slowly, don't add to the horizontal stride progress as 
         * slow careful steps are silent. This could be changed by simply adjusting the
         * sound of the step relative to the average speed upon making the footstep effect.
         *
         * If the player is immobile, reset the tracking values as they would not have
         * any momentum or make any footstep sounds while standing still.
         */

        /* The player is immobile - reset the step tracking variables */
        if(horizontalInputDirection.magnitude == 0) {

            pastDirections.Clear();
            ResetFootTiming();
            ResetStrideProgress();

            //Add a condition that checks if the pastDirections is full and has a faily fast avg speed. This will imdicate a hard stop
        }

        /* Continuously staying under a slow speed will prevent the producing of any footsteps */
        else if(pastDirections.Count >= lookbackCount &&
                horizontalInputDirection.magnitude <= playerMovementSpeed/2f &&
                AverageStepSpeed() <= playerMovementSpeed/2f) {
            ResetFootTiming();
            ResetStrideProgress();
            Debug.Log("Slow walking");
        }

        
        /* Player is moving and is above the "slow" movement condition */
        else {
            
            /* Only add to the horizontal stride progress if the player is moving above the slow speed */
            if(horizontalInputDirection.magnitude > playerMovementSpeed/2f) {
                currentHorizontalStride += CalculateStepValue(horizontalInputDirection);
            }
            /* Let the step timings increase without incrementing the horizontal stride progress */
            else {
                Debug.Log("entering slowWalk");
            }
            
            /* Track the directionnal input by adding it to the pastDirections array */
            pastDirections.Add(horizontalInputDirection);
            if(pastDirections.Count > lookbackCount) {
                /* Remove the oldest input if we reached max directions to track */
                pastDirections.RemoveAt(0);
            }
        }



    }

    public float CalculateStepValue(Vector3 horizontalInputDirection) {
        /*
         * Compare the input to the average direction produced by pastDirections.
         * This average will represent the player's current momentum.
         *
         * Depending on how different (direction and magnitude) the input and average are,
         * the distance value to be added to the current horizontal stride will increase.
         * Moving against momentum (turning, slowing down) requires more effort.
    	 */
        Vector3 avgDirection = AverageStepDirection();
        float avgSpeed = AverageStepSpeed();
        float stepAngleDiff = Vector3.Angle(avgDirection, horizontalInputDirection);
        float stepSpeedDiff = Mathf.Abs(horizontalInputDirection.magnitude - avgSpeed);
        float stepValue = horizontalInputDirection.magnitude;
        float speedValue = 0;
        float angleValue = 0;

        /* Use the angle difference between the given and the average directions */
        /* Set key angle values that mark when the angle effects the stepValue */
        float minAngle = 5;
        float maxAngle = 75;
        float resetAngle = 125f;
        float angleIncreaseRatio = 2;

        /* Step value remains uneffected */
        if(stepAngleDiff < minAngle) { }

        /* Step value increases as the angle difference increases */
        else if(stepAngleDiff < maxAngle) {
            angleValue += stepValue*angleIncreaseRatio*((stepAngleDiff-minAngle) / (maxAngle-minAngle));
        }

        /* Step value increases by an amount as if the angle difference is equal to angleMax */
        else if(stepAngleDiff < resetAngle) {
            angleValue += stepValue*angleIncreaseRatio;
        }
        

        /* Use the difference between the input speed and the average speed */
        /* Set key speed values that mark when the speed difference starts effecting the step value */
        float maxSpeed = playerMovementSpeed/2f;
        float speedIncreaseRatio = 4;

        /* A speed difference bellow the max will effect the stepValue relative to the difference */
        if(stepSpeedDiff < maxSpeed) {
            speedValue += playerMovementSpeed*speedIncreaseRatio*(stepSpeedDiff/maxSpeed);
        }
        
        /* A speed difference above the max will not add any extra value to the stepValue */
        else{
            speedValue += playerMovementSpeed*speedIncreaseRatio;
        }


        /* Add the extra "distance" from the angle and speed to the stepValue */
        stepValue += angleValue + speedValue;

        return stepValue;
    }

    public void AddVerticalStep(float verticalDistance) {
        /*
         * The player made a step along their relative Y axis.
    	 * Add the given distance to the current vertical stride distance
    	 */

        currentVerticalStride += verticalDistance;
    }

    public void PlayStep() {
        /*
    	 * A step has been made, so send a command to play a footstep sound effect.
    	 * SFX will be applied to the step depending on current variables.
    	 */

        //print the foot times
        Debug.Log("---");
        for(int i = 0; i < timeSinceStep.Length; i++) {
            Debug.Log(timeSinceStep[i]);
        }
        Debug.Log("---");


        /* Get the amount of time since the next foot played a footstep effect */
        float timing = ResetFootTiming(GetNextFoot());

        /* Adjust the FX of the sound using tracked foostep stats.
         * timing: Controls the playback speed. Short time between steps mean 
         * 		they are taking quick, short steps. Shorter steps will also 
         * 		sound quieter and end quicker while longer steps may have a slight echo.
         * currentVerticalStride: Controls the pitch. Stepping up will gave a higher frequency
         * 		while stepping down has a lower pitch. Should be slight, but more than the variance.
         */
        playerSoundsScript.PlayFootstep();


        /* Reset the current stride distances */
        ResetStrideProgress();
    }
    
    public void Landing() {
        /*
         * Runs when the player lands from a fall. This will play a specific footstep
         * effect used to landing and it will reset the current momentum.
         */

        //Play the landing effect

        /* Reset the player momentum (current stride and pastDirections) */
        ResetStrideProgress();
        ResetFootTiming();
        pastDirections.Clear();
    }


    /* ----------- Helper Functions ------------------------------------------------------------- */

    public Vector3 AverageStepDirection() {
        /* 
    	 * Return the average step direction. Each direction is converted
    	 * to a unit vector as we do not want their magnitude to effect the direction.
         * If the pastDirections array is empty, return a zero vector.
    	 */
        Vector3 avgDirection = Vector3.zero;

        /* Add all tracked normalized direction */
        foreach(Vector3 dir in pastDirections) {
            avgDirection += dir.normalized;
        }

        /* Get the average of all the directions */
        if(pastDirections.Count > 0) {
            avgDirection = (avgDirection/pastDirections.Count).normalized;
        }

        return avgDirection;
    }

    public float AverageStepSpeed() {
        /*
    	 * Get the average step speed using the magnitude of each pastDirections vector.
         * If the pastDirections array is empty, just return 0. If the list is not full, 
         * assume all other entries have a speed of 0.
    	 */
        float avgSpeed = 0;

        /* Add the magnitude of all tracked directions */
        foreach(Vector3 dir in pastDirections) {
            avgSpeed += dir.magnitude;
        }

        /* Get the average of the magnitudes */
        if(pastDirections.Count > 0) {
            avgSpeed /= lookbackCount;
        }

        return avgSpeed;
    }

    public int GetNextFoot() {
        /*
    	 * Search the foot timing variables and return the index
    	 * to the next foot that will hit the ground (longest time since step)
    	 */
        int nextFootRef = 0;

        /* Search the array for the "oldest" foot */
        for(int i = 1; i < timeSinceStep.Length; i++) {
            if(timeSinceStep[i] > timeSinceStep[nextFootRef]) {
                nextFootRef = i;
            }
        }

        return nextFootRef;
    }

    public float ResetFootTiming(int footIndex) {
        /* 
    	 * With the given reference to a foot's last play time,
    	 * reset it's timing back to 0 and return what it's final time was.
    	 */
        float footFinalTime = timeSinceStep[footIndex];

        timeSinceStep[footIndex] = 0;

        return footFinalTime;
    }

    public void ResetFootTiming() {
        /*
    	 * Reset the timings of each footstep.
    	 * Runs when the player is immobile or lands.
    	 */

        for(int i = 0; i < timeSinceStep.Length; i++) {
            timeSinceStep[i] = 0;
        }
    }

    public void ResetStrideProgress() {
        /*
    	 * Reset the player's current stride progress back to 0
    	 */

        currentHorizontalStride = 0;
        currentVerticalStride = 0;
    }
}