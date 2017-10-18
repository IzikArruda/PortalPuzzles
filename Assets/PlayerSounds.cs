using UnityEngine;
using System.Collections;

/*
 * Contains all the sounds that will be made by the player.
 * May want to have a sepperate transform for this script. a transformSounds.
 */
public class PlayerSounds : MonoBehaviour {

    /* The main gameObject that will hold all audioSources.
     * This will change if the position of the audioSource effects it's sound */
    public GameObject mainSourceHolder;

	/* --- Footstep Variables ------------------- */
	/* The sound effects that will be used for footsteps */
	public AudioClip[] stepClips;
    /* The audioSources that will play the footstep sound effects */
    private AudioSource[] stepSources; 
    /* The gameObject that will contain the footstep audio sources */
    //public GameObject stepObject;
	/* Only allow up to stepSourceCount footstep effects from playing at once. */
	public int stepSourceCount;
    /* The index of the last played footstep effect */
    private int previousStepEffectIndex;
	
	
	/* --- Ambient Music Variables --------------------------------- */
	/* The music files that will be used as background music */
	public AudioClip[] musicClips;
    /* The audioSource that plays the game's music */
    public AudioSource musicSource;
    /* The gameObject that contains the music sources */
    //public GameObject musicObject;
	/* The index of the currently playing musicClip to prevent repetition */
	private int currentMusicClipIndex;
	/* 0: Nothing. -1: Fade out the music. 1: Fade in the music */
	private int musicFade = 0;
	
	
	/* --- Landing/Falling Audio Variables --------------------------------- */
	/* The music that will be used while fastfalling */
	//fastfall audio : bus and jet engine?
	public AudioClip fastFallingClip;
	/* The audioSource to play the fallig sounds */
	public AudioSource fastFallingSource;
	/* The gameObject that holds the falling audio source */
	//public GameObject fastFallingObject;
    /* The audio clip that plays when the player lands from a fastfall */
    //just use the whitney houston bang
    public AudioClip fastFallLandingClip;


    /* ----------- Built-in Unity Functions ------------------------------------------------------------- */

    void Start(){
        /*
		 * Initialize some variables to their default
		 */
         
        /* Set the clip index trackers. No matter what value they are, a clip will always get culled */
		previousStepEffectIndex = stepClips.Length;
        currentMusicClipIndex = musicClips.Length;

        /* If no gameObject is given to house the AudioSources, use the one linked to this script. */
        /*if(stepObject == null){
			stepObject = gameObject;
		}
		if(musicObject == null){
			musicObject = gameObject;
		}
		if(fastFallingObject == null){
			fastFallingObject = gameObject;
		}*/

        /* Create the appropriate amount of audioSources for the footstep effects */
        stepSources = new AudioSource[stepSourceCount];
		for(int i = 0; i < stepSourceCount; i++){
            stepSources[i] = mainSourceHolder.AddComponent<AudioSource>();
		}
		
		/* Create the audioSource for the game music */
		musicSource = mainSourceHolder.AddComponent<AudioSource>();
        Debug.Log(musicSource);
        
        /* Create the audioSource for the fastfall audio */
        fastFallingSource = mainSourceHolder.AddComponent<AudioSource>();
	}
	
	void Update(){
		
		/* Adjust the volume of audio sources if needed */
		UpdateSourceVolume();
	}
	
	void OnDisable(){
		/*
		 * To prevent any unnecessairy extra audioSources, 
		 * Delete all audioSources when this script is disabled.
		 * This should run on program End instead, as this is just
		 * used to prevent constant audioSources from poping up in the editor.
		 */
		
		for(int i = 0; i < stepSourceCount; i++){
			Destroy(stepSources[i]);
		}
        stepSources = null;
		
		Destroy(musicSource);
		Destroy(fastFallingSource);
	}
	
	
    /* ----------- Play Sounds Functions ------------------------------------------------------------- */
	
	public void PlayFootstep(){
		/*
		 * Play a sound effect of a footstep. If there are more than 1 potential
		 * footstep sound effects that can be played, do not play the same previous effect.
		 *
		 * should be generalized. give a function a file and a source arrays and it will spit out an index.
		 */
		int stepEffectIndex = 0;
		AudioSource source = null;
	
		/* Pick a random footstep effect if theres more than 1 to choose from */
		stepEffectIndex = RandomClip(stepClips, previousStepEffectIndex);
		
		/* Search the footstep audioSources for one not currently in use */
		source = UnusedSoundSource(stepSources);
		
		
		if(source == null){
			Debug.Log("Footstep effect cannot play - no available audio sources");
		}
		
		/* Play the footstep effec't using the found audio source */
		else{
			source.clip = stepClips[stepEffectIndex];
			source.Play();
			previousStepEffectIndex = stepEffectIndex;
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
        Debug.Log(songIndex);
		musicSource.clip = musicClips[songIndex];
        musicSource.Play();
		currentMusicClipIndex = songIndex;
	}
	
	public void EnterFastFall(){
		/* 
		 * As the player enters the fastfalling state, the game music should switch over to
		 * the "sound of falling". This is done by fading out the music and in the falling audio.
		 */
		
		/* fade out the music */
		musicFade = -1;
		
		/* Start and fade in the fastFalling sound */
		fastFallingSource.clip = fastFallingClip;
		fastFallingSource.volume = 1;
        fastFallingSource.Play();
	}
	
	public void FastFallLanding(){
		/*
		 * When landing from the FastFall state, play a landing sound and start the music again.
		 */
		
		/* Fade in the music */
		PlayMusic();
		musicFade = 1;
		
		/* Stop the falling audio and start the landing audio */
		fastFallingSource.Stop();
		fastFallingSource.clip = fastFallLandingClip;
		fastFallingSource.Play();
	}
	
	
	/* ----------- Audio Mixing Functions ------------------------------------------------------------- */
	
	void UpdateSourceVolume(){
		/*
		 * Change the volume of an audio source, simulating a fade effect on the clip
		 */
		
		if(musicFade == -1){
			
			/* Stop the fade out and the audio source once it's fully muted */
			if(musicSource.volume <= 0){
				musicSource.Stop();
				musicFade = 0;
			}else{
				musicSource.volume = musicSource.volume - 0.01f;
			}
		}
		else if(musicFade == 1){
			
			/* Stop the fade in once the audio sources is max volume (1.0f) */
			if(musicSource.volume >= 1.0f){
				musicSource.volume = 1.0f;
			}else{
				musicSource.volume = musicSource.volume + 0.01f;
			}
		} 
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
			randomIndex = Random.Range(0, clips.Length-2);
		
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
