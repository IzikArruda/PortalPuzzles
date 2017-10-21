using UnityEngine;
using System.Collections;
using System.Collections.Generic;
//idea. use the previous 3 step directions and weight the new steps against that.
//3 steps go north. stepFavor is full north.
//next step goes north. stepFavor is north. step is now half value, stepFavor remains.
//next step goes south. stepFavor is north. step is worth double, avg of north north south is north.
//another south, makes favor become south.
//steps like northSouth with a favor of north will be worth a bit more than half and change a bit of the favor.
//*This only handles direction. The power/distance of a dtep will have a sepperate favor.
//*a change in stride will cause a quick step to adjust to the new speed (walk to run, run to walk)
//3 idle steps in a row will indicate the player stopped.

/*
 * Track the player's footsteps by retaining the player's last set of moves and calculating whether
 * they have travelled enough distance to warrent playing a footstep sound effect.
 */
public class FootstepTracker : MonoBehaviour {

    /* The sound script that will be used to play the footsteps */
    private PlayerSounds playerSoundsScript;


    /* --- Stride Values ------------------- */
    /* The current distance the player has moved since the last footstep effect was played */
    private float currentHorizontalStride;
    private float currentVerticalStride;
    /* How far the player can move before a footstep sound should play. */
    private float maxHorizontalStride;
    private float maxVerticalStride;
    /* Variables used to calculate the maxStride values. Relative to the player's movementSpeed */
    public float horizontalStrideRelative;


    /* --- Footstep Tracker ------------------- */
    /* How many past directions are tracked */
    public float lookbackCount;
    /* A list of past movement directions to calculate the average */
    //Note: this should reset upon landing. Add a reset steps function that also resets the stride progress.
    private List<Vector3> pastDirections = new List<Vector3>();


    /* ----------- Set-up Functions ------------------------------------------------------------- */

    public void CalculateStrideDistances(float playerMovementSpeed) {
        /*
         * Calculate the max stride distances, which represents how far 
         * a player will travel before a footstep effect will play.  
         */
        //40 is a good value if the person is moving forward forever
        horizontalStrideRelative = 40;

        maxHorizontalStride = playerMovementSpeed*horizontalStrideRelative;
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

        /* Check whether a footstep sound effect should be played by comparing the current stride progress */
        if(Mathf.Abs(currentHorizontalStride) > maxHorizontalStride) {
            PlayStep();
        }
    }

    public void AddHorizontalStep(Vector3 horizontalDistance) {
        /*
         * Update the horizontal stride distance and the pastDirections array with a new
         * direction. This will be run every frame whenever the player is in a grounded state.
         * 
         * 
         * Movement along the player's relative horizontal (x, z) was made.
    	 * Add the given distance to the current horizontal stride distance.
         * 
         * Use an array of past step directions that track previous footstep directions
         * to calculate how much the player is "changing their momentum".
         * Having an input identical to the average of the previous step directions
         * means very little momentum change, i.e. longer strides/more distance between each step.
         * Having a large difference means a sharp turn, i.e. short, quick steps.
    	 */
        Vector3 avgDirection = AverageStepDirection();
        float avgDistance = AverageStepDistance();
        float stepValue, stepAngleDiff;
        //Draw the input from the player
        Debug.DrawRay(transform.position, horizontalDistance*50, Color.green);

        /* Get the angle between the player's inputted direction and the average step direction */
        stepAngleDiff = Vector3.Angle(avgDirection, horizontalDistance);

        /* Calculate a "step value" by comparing the current step made to the average step */
        stepValue = horizontalDistance.magnitude;

        /////
        //Do stuff with the step value here
        /////



        /* Add the final step value to the current horizontal stride progress */
        currentHorizontalStride += stepValue;

        /* Update the past directions list by adding the new direction and removing an old one */
        pastDirections.Add(horizontalDistance);
        if(pastDirections.Count > lookbackCount) {
            pastDirections.RemoveAt(0);
        }
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
    	 */

        /* Play a footstep sound */
        playerSoundsScript.PlayFootstep();

        /* Reset the current stride distances */
        currentHorizontalStride = 0;
        currentVerticalStride = 0;
    }


    /* ----------- Helper Functions ------------------------------------------------------------- */

    public Vector3 AverageStepDirection() {
        /* 
    	 * Return the average step direction. Each direction is converted
    	 * to a unit vector as we do not want their magnitude to effect the direction.
    	 */
        Vector3 avgDirection = Vector3.zero;

        /* Add all tracked normalized direction */
        foreach(Vector3 dir in pastDirections) {
            avgDirection += dir.normalized;
        }

        /* Get the average of all the directions */
        avgDirection = (avgDirection/pastDirections.Count).normalized;

        return avgDirection;
    }

    public float AverageStepDistance() {
        /*
    	 * Get the average step distance using the magnitude of each pastDirections vector
    	 */
        float avgDistance = 0;

        /* Add the magnitude of all tracked directions */
        foreach(Vector3 dir in pastDirections) {
            avgDistance += dir.magnitude;
        }

        /* Get the average of the magnitudes */
        avgDistance /= pastDirections.Count;

        return avgDistance;
    }
}