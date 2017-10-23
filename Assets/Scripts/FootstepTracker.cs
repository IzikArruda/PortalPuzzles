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
//add a reference to player controler

    /* --- Stride Values ------------------- */
    /* The current distance the player has moved since the last footstep effect was played */
    private float currentHorizontalStride;
    private float currentVerticalStride;
    /* How far the player can move before a footstep sound should play. */
    public float maxHorizontalStride;
    public float maxVerticalStride;
    /* Used to calculate the maxStride values. Relative to the player's movementSpeed and leg length*/
    public float horiStrideMod;
    public float vertStrideMod;


    /* --- Footstep Tracker ------------------- */
    /* How many past directions are tracked */
    public float lookbackCount;
    /* A list of past movement directions to calculate the average */
    private List<Vector3> pastDirections = new List<Vector3>();
    /* The time between the last footstep effect for a given foot */
    public float[] timeSinceStep;
	

    /* ----------- Set-up Functions ------------------------------------------------------------- */

	public void Start(){
		
		/* Setup the step times array to track each foot's timing */
		timeSinceStep = new float[2];
		for(int i = 0; i < timeSinceStep.Length; i++){
			timeSinceStep[i] = 0.0f;
		}
	}


    public void CalculateStrideDistances(float playerMovementSpeed, float playerLegLength) {
        /*
         * Calculate the max stride distances, which represents how far 
         * a player will travel before a footstep effect will play.  
         */
        //40 is a good value if the person is moving forward forever
        horiStrideMod = 40;
        vertStrideMod = 0.9f;

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
		for(int i = 0; i < timeSinceStep.Length; i++){
			timeSinceStep[i] += Time.deltaTime;
		}

        /* Check whether a footstep sound effect should be played by comparing the current stride progress */
        //if(Mathf.Abs(currentHorizontalStride) >= maxHorizontalStride) {
        if(Mathf.Abs(currentVerticalStride) >= maxVerticalStride || Mathf.Abs(currentHorizontalStride) >= maxHorizontalStride) {
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
        /* The player is immobile, reset the tracking values */
        else {
            currentHorizontalStride = 0;
            currentVerticalStride = 0;
            pastDirections.Clear();
            ResetFootTiming();
        }


        /* If the player is speeding up/slowing down compared to their average distance, increase the stepValue */
        //These values (0.04 and 5) need accees to the playerCobtroller script for values
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

        /* If the given input is small (relative to the player's expected speed), do not add to the value */
        if(horizontalInputDirection.magnitude < 0.04f/2f) {
            stepValue = 0;
        }



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
            /* Force a step and reset the tracking and foot timing*/
            Debug.Log("REDIRECT");
            currentHorizontalStride = maxHorizontalStride;
            pastDirections.Clear();
            ResetFootTiming();
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
    	 * SFX will be applied to the step depending on current variables.
    	 */
		
		//print the foot times
		Debug.Log("---");
		for(int i = 0; i < timeSinceStep.Length; i++){
			Debug.Log(timeSinceStep[i]);
		}
		Debug.Log("---");

        
        /* Get the amount of time since the next foot played a footstep effect */
        float timing = ResetFootTiming(GetNextFoot());

        /* Play a footstep sound */
        playerSoundsScript.PlayFootstep();

        /* Reset the current stride distances */
        currentHorizontalStride = 0;
        currentVerticalStride = 0;
    }
    
    
    

    public void Landing() {
        /*
         * Runs when the player lands from a fall. This will play a specific footstep
         * effect used to landing and it will reset the current momentum.
         */

        //Play the landing effect

        /* Reset the player momentum (current stride and pastDirections) */
        currentHorizontalStride = 0;
        currentVerticalStride = 0;
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
    
    public int GetNextFoot(){
    	/*
    	 * Search the foot timing variables and return the index
    	 * to the next foot that will hit the ground (longest time since step)
    	 */
    	int nextFootRef = 0;
    
    	/* Search the array for the "oldest" foot */
    	for(int i = 1; i < timeSinceStep.Length; i++){
    		if(timeSinceStep[i] > timeSinceStep[nextFootRef]) {
    			nextFootRef = i;
    		}
    	}
    	
    	return nextFootRef;
    }
    
    public float ResetFootTiming(int footIndex){
    	/* 
    	 * With the given reference to a foot's last play time,
    	 * reset it's timing back to 0 and return what it's final time was.
    	 */
    	float footFinalTime = timeSinceStep[footIndex];

        timeSinceStep[footIndex] = 0;

        return footFinalTime;
    }
    
    public void ResetFootTiming(){
    	/*
    	 * Reset the timings of each footstep.
    	 * Runs when the player is immobile or lands.
    	 */
    	
    	for(int i = 0; i < timeSinceStep.Length; i++){
    		timeSinceStep[i] = 0;
    	}
    }
}