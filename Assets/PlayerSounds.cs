using UnityEngine;
using System.Collections;


//////////TEST IF THE POSITION OF THIS SCRIPT IN THE WORLD EFFECTS THE AUDIO


/*
 * Contains all the sounds that will be made by the player.
 * May want to have a sepperate transform for this script. a transformSounds.
 */
public class PlayerSounds : MonoBehaviour {

	/* --- Footstep Variables ------------------- */
	/* The sound effects that will be used for footsteps */
	public AudioClip[] footstepClips;
    /* The audioSources that will play the footstep sound effects */
    public AudioSource[] footstepSources; 
	/* Only allow up to footstepCount footstep effects from playing at once. */
	public int footstepCount;
    /* The index of the last played footstep effect */
    public int previousFootstepEffectIndex;
	
	
	/* --- Ambient Music Variables --------------------------------- */
	/* The music files that will be used as background music */
	public AudioClip[] musicClips;
    /* The audioSource that plays the game's music */
    public AudioSource musicSource;
	/* The index of the currently playing musicClip to prevent repetition */
	private int currentMusicClipIndex;
	
	
    /* ----------- Built-in Unity Functions ------------------------------------------------------------- */
	
	void Start(){
        /*
		 * Initialize some variables to their default
		 */
         
		previousFootstepEffectIndex = footstepClips.Length;
        currentMusicClipIndex = musicClips.Length;
		
		/* Create the appropriate amount of audioSources for the footstep effects */
		footstepSources = new AudioSource[footstepCount];
		for(int i = 0; i < footstepCount; i++){
            //AudioSource testS = new AudioSource();
            //footstepSources[i] = testS;
            gameObject.AddComponent<AudioSource>();
		}
		
		/* Create the audioSource for the game music */
		musicSource = new AudioSource();
        Debug.Log(musicSource);
	}
	
	void OnDisable(){
		/*
		 * To prevent any unnecessairy extra audioSources, 
		 * Delete all audioSources when this script is disabled.
		 * This should run on program End instead, as this is just
		 * used to prevent constant audioSources from poping up in the editor.
		 */
		
		for(int i = 0; i < footstepCount; i++){
			Destroy(footstepSources[i]);
		}
        footstepSources = null;
		
		Destroy(musicSource);
	}
	
	
    /* ----------- Play Sounds Functions ------------------------------------------------------------- */
	
	public void PlayFootstep(){
		/*
		 * Play a sound effect of a footstep. If there are more than 1 potential
		 * footstep sound effects that can be played, do not play the same previous effect.
		 *
		 * should be generalized. give a function a file and a source arrays and it will spit out an index.
		 */
		int footstepEffectIndex = 0;
		AudioSource source = null;
	
		/* Pick a random footstep effect if theres more than 1 to choose from */
		footstepEffectIndex = RandomClip(footstepClips, previousFootstepEffectIndex);
		
		/* Search the footstep audioSources for one not currently in use */
		source = UnusedSoundSource(footstepSources);
		
		
		if(source == null){
			Debug.Log("Footstep effect cannot play - no available audio sources");
		}
		
		/* Play the footstep effect using the found audio source */
		else{
			source.clip = footstepClips[footstepEffectIndex];
			source.Play();
			previousFootstepEffectIndex = footstepEffectIndex;
		}
	}
	
	
	/* ----------- Play Music Functions ------------------------------------------------------------- */
	
	public void PlayMusic(){
		/*
		 * Take a random song clip and play it for the game's music
		 */
		int songIndex;
		
		/* Get the index of a new song */
		songIndex = RandomClip(musicClips, currentMusicClipIndex);
		
		/* Update the musicSource with the new song */
		musicSource.clip = musicClips[songIndex];
        musicSource.Play();
		currentMusicClipIndex = songIndex;
	}
	
	
	/* ----------- Helper Functions ------------------------------------------------------------- */
	
	public int RandomClip(AudioClip[] clips, int previousClipIndex){
		/*
		 * Using the given array of clips, pick the index of a random clip.
		 * Do not include the clip given by the given int previousClip.
		 */
		int randomIndex = 0;
		
		/* Pick a random index if there exists more than 1 clip to choose from */
		if(clips.Length > 1){
		
			/* Get a random integer between 0 and X-1 where X is the amount of unique footstep sounds */
			randomIndex = 1;
		
			/* If the effect's index is equal or above the previous played effect, increase it by 1 */
			if(randomIndex >= previousClipIndex){
				randomIndex++;
			}
		}
		
		return randomIndex;
	}
	
	public AudioSource UnusedSoundSource(AudioSource[] sourceArray){
        /*
		 * Search the given array of soundSources and return the first one
		 * that is not currently playing a sound. Return null if none are free.
		 */
        AudioSource freeSource = null;
		
		for(int i = 0; (i < sourceArray.Length && freeSource == null); i++){
			if(sourceArray[i].isPlaying == false){
				freeSource = sourceArray[i];
			}
		}
		
		return freeSource;
	}
}
