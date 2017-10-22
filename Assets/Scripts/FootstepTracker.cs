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
        if(Mathf.Abs(currentHorizontalStride) >= maxHorizontalStride) {
            PlayStep();
        }
    }

    public void AddHorizontalStep(Vector3 horizontalInputDirection) {
        /*
         * Update the horizontal (x, z) stride distance and the pastDirections array with a new
         * direction. This will be run every frame whenever the player is in a grounded state.
         * 
         * Use an array of past step directions that track previous footstep directions
         * to calculate how much the player is "changing their momentum".
         * Having an input identical to the average of the previous step directions
         * means very little momentum change, i.e. longer strides/more distance between each step.
         * Having a large difference means a sharp turn, i.e. short, quick steps.
    	 */
        float stepValue = 0;

        /* If the player gave an input that was not immobile, calculate a stepValue */
        if(horizontalInputDirection.magnitude != 0) {
            stepValue = CalculateStepValue(horizontalInputDirection);
        }
        /* If the player was previously moving but now stopped, force a footstep effect to imply they stopped */
        else if(AverageStepDirection().magnitude != 0 && pastDirections.Count >= lookbackCount) {
            Debug.Log("stopped");
            currentHorizontalStride = maxHorizontalStride;
            pastDirections.Clear();
        }


        /* If the player is speeding up/slowing down compared to their average distance, increase the stepValue */
        if(horizontalInputDirection.magnitude != 0) {
            //stepValue can increase by up to 5*.
            //If the difference between the magnitudes is larger than the player's normal speed, remain at 5* increase.
            float distanceDiff = Mathf.Abs(horizontalInputDirection.magnitude - AverageStepDistance());
            if(distanceDiff < 0.04f) {
                stepValue += 5*stepValue*(distanceDiff/0.04f);
            }
            else {
                stepValue += 5*stepValue;
            }
        }




        /* Add the final stepValue to the current horizontal stride progress */
        currentHorizontalStride += stepValue;

        /* Update the past directions list by adding the new inputted direction */
        pastDirections.Add(horizontalInputDirection);
        if(pastDirections.Count > lookbackCount) {
            /* Remove the oldest input if we reached max directions to track */
            pastDirections.RemoveAt(0);
        }
    }

    public float CalculateStepValue(Vector3 horizontalInputDirection) {
        /*
         * Using a series of set angle values and the user's given direction compared to the average direction,
         * return a "stepValue" that determines how much the player moved given the input.
         * If the player's given input goes againts the average direction, it can be seen as moving againts
         * their current momentum. Moving against your momentum will increase the step value.
         * 
         * A given direction with a magnitude of 0 means the player is not moving.
         */
        Vector3 avgDirection = AverageStepDirection();
        float stepValue;
        float stepAngleDiff;

        /* Get the angle between the player's inputted direction and the average step direction */
        stepAngleDiff = Vector3.Angle(avgDirection, horizontalInputDirection);

        /* Set the stepValue to be relative to the player's inputted direction's magnitude */
        stepValue = horizontalInputDirection.magnitude;

        //Debug.Log(stepAngleDiff);
        /* Change the stepValue relative to the angle difference of the input and average direction */
        //Track what kind of value change occurs. Steps that have had a lot fo value change will be 
        //quick ones that have the player seem very agile and quick on their feet, like spinning in a circle.
        //These steps should sound different, like a quicker step. A vlaue will need to be tracked
        //so that once the footstep is played it takes into account how much value change has occured
        /* minAngle marks when an angle difference starts effecting the stepValue */
        float minAngle = 0;
        /* maxAngle marks when any increase in angle difference will have no extra change to the stepValue */
        float maxAngle = 75;
        /* Above resetAngle will force a step and reset the pastdirections. Simulate a sharp sudden turn. */
        float resetAngle = 125f;
        float increaseAmount = 3;
        float relativeIncrease = 1;

        if(stepAngleDiff < minAngle) {
            /* Step value remains unchanged */
        }
        else if(stepAngleDiff < maxAngle) {
            /* Increase the step value depending on the angleDifference */
            stepValue += stepValue*increaseAmount*((stepAngleDiff-minAngle) / (maxAngle-minAngle));
            relativeIncrease += increaseAmount*((stepAngleDiff-minAngle) / (maxAngle-minAngle));
        }
        else if(stepAngleDiff < resetAngle) {
            /* Increase the step value by a (relatively) set amount */
            stepValue += stepValue*increaseAmount;
            relativeIncrease += increaseAmount;
        }
        else {
            /* Force a step and reset the tracking */
            Debug.Log("REDIRECT");
            currentHorizontalStride = maxHorizontalStride;
            pastDirections.Clear();
        }
        //Debug.Log(relativeIncrease);
        //Note there is a condition where the avg becomes 0/the player stops moving.
        //This is difference than having the step angle reach 180. 
        //when they stop, force a step to occur and maybe reset the previous tracked steps.




        //Idea: if the player fully reaches around/past maxAmount, play a footstep and change 
        //the entire past tracked values to the given new one since 
        //the player took a quick step to fully change their momentum


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

    public float AverageStepDistance() {
        /*
    	 * Get the average step distance using the magnitude of each pastDirections vector.
         * If the pastDirections array is empty, just return 0.
    	 */
        float avgDistance = 0;

        /* Add the magnitude of all tracked directions */
        foreach(Vector3 dir in pastDirections) {
            avgDistance += dir.magnitude;
        }

        /* Get the average of the magnitudes */
        if(pastDirections.Count > 0) {
            avgDistance /= pastDirections.Count;
        }

        return avgDistance;
    }
}