using UnityEngine;
using System.Collections;


test to see if audio source position mayters
assignment must submt





/*
 * Contains all the sounds that will be made by the player.
 * May want to have a sepperate transform for this script. a transformSounds.
 */
public class PlayerSounds : MonoBehaviour {

	/* The audioSource that will play all the player's sounds */
	public AudioSource audioSource;

	/* The sound effects that will be used for footsteps */
	public AudioClip[] footstepClips;   
	/* The audioSources that will play the footstep sound effects */
	private AudioSource[] footstepSources; 
	/* Only allow up to footstepCount footstep effects from playing at once. */
	public int footstepCount;
	/* The index of the previously played footstep effect */
	public int previousFootstepEffectIndex;
	
	
    /* ----------- Built-in Unity Functions ------------------------------------------------------------- */
	
	void Start(){
		/*
		 * Initialize some variables to their default
		 */
		
		previousFootstepEffectIndex = footsteps.Length;
		
		/* Create the appropriate amount of audioSources for the footstep effects */
		footstepSources = new AudioSource[footstepCount];
		for(int i = 0; i < footstepCount; i++){
			footstepSources[i] = new AudioSource();
		}
	}
	
	
    /* ----------- Play Sound Functions ------------------------------------------------------------- */
	
	public PlayFootstep(){
		/*
		 * Play a sound effect of a footstep. If there are more than 1 potential
		 * footstep sound effects that can be played, do not play the same previous effect.
		 *
		 * should be generalized. give a function a file and a source arrays and it will spit out an index.
		 */
		int footstepEffect = 0;
		AudioSource source = null;
	
		/* Pick a random footstep effect if theres more than 1 to choose from */
		if(footsteps.Length > 1){
		
			/* Get a random integer between 0 and X-1 where X is the amount of unique footstep sounds */
			footstepEffect = 1;
		
			/* If the effect's index is equal or above the previous played effect, increase it by 1 */
			if(footstepEffect >= previousFootstepEffectIndex){
				footstepEffect++;
			}
		}
		
		/* Search the footstep audioSources for one not currently in use */
		for(int i = 0; (i < footstepSources.Length && source == null); i++){
			if(footstepSources[i].isPlaying == false){
				source = footstepSources[i];
			}
		}
		
		
		
		if(source == null){
			Debug.Log("Footstep effect cannot play - no available audio sources");
		}
		
		/* Play the footstep effect using the found audio source */
		else{
			source.clip = footsteps[footstepEffect];
			source.Play();
		}
	}
}
