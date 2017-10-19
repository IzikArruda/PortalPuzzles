using UnityEngine;
using System.Collections;

/*
 * Contains all the sounds that will be made by the player.
 * May want to have a sepperate transform for this script. a transformSounds.
 */
public class PlayerSounds : MonoBehaviour {

    /* The main gameObject that will hold all audioSources.
     * This will change if the position of the audioSource effects it's sound */
    public GameObject mainSourceContainer;
    
    /* --- Clips */
    
    /* Sources */
    
    
    /* Indexes */
    
    
    /* Misc */
    
    
    /* --- Volume Variables ------------------- */
    /* How loud the volume of the audio is at max */
    public float maxVolume;

    /* How fast the audio fades universally. Rate is relative to maxVolume and ___Fade (+-x) */
    [Range(1, 0)]
    public float fadeRate;
    /* 0: Nothing. -x: Fade out the music. +x: Fade in the music */
	private int musicFade = 0;
	private int fallingFade = 0;


	/* --- Footstep Variables ------------------- */
	/* The sound clips and audio sources that will be used for footsteps */
	public AudioClip[] stepClips;
    private AudioSource[] stepSources; 
	/* limit the amout of footstep effects from playing at once by limiting stepSources size */
	public int maxSimultaniousStepEffects;
    /* The index of the last played footstep effect */
    private int lastStepClipIndex;
	
	
	/* --- Ambient Music Variables --------------------------------- */
	/* The music clips and the source that will play them */
	public AudioClip[] musicClips;
    private AudioSource musicSource;
	/* The index of the last music clip that played musicClip to prevent repetition */
	private int lastMusicClipIndex;
	
	
	/* --- Falling Audio Variables --------------------------------- */
	/* The clip and source for the audio that plays when fast falling */
	//fastfall audio : bus and jet engine?
	public AudioClip fallingClip;
	private AudioSource fallingSource;
    

	
	
	/* --- Falling Audio Variables --------------------------------- */
	/* The clip and source for the audio of the player landing from a fast fall */
	//just use the whitney houston bang
	public AudioClip landingClip;
	private AudioSource landingSource;
	

    /* ----------- Built-in Unity Functions ------------------------------------------------------------- */

    void Start(){
        /*
		 * Initialize some variables to their default
		 */
         
        /* Set the clip index trackers. No matter what value they are, a clip will always get culled */
        lastStepClipIndex = stepClips.Length;
        lastMusicClipIndex = musicClips.Length;

        /* Create the appropriate amount of audioSources */
        stepSources = new AudioSource[maxSimultaniousStepEffects];
        for(int i = 0; i < stepSources.Length; i++){
        	stepSources[i] = mainSourceContainer.AddComponent<AudioSource>();
        }
		musicSource = mainSourceContainer.AddComponent<AudioSource>();
        fallingSource = mainSourceContainer.AddComponent<AudioSource>();
        landingSource = mainSourceContainer.AddComponent<AudioSource>();
        
        /* Apply the maxVolume to each audioSource */
        ApplyMaxVolume();
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
		
		for(int i = 0; i < stepSources.Length; i++){
			Destroy(stepSources[i]);
		}
        stepSources = null;
		
		Destroy(musicSource);
		Destroy(fallingSource);
		Destroy(landingSource);
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
		stepEffectIndex = RandomClip(stepClips, lastStepClipIndex);
		
		/* Search the footstep audioSources for one not currently in use */
		source = UnusedSoundSource(stepSources);
		
		
		if(source == null){
			Debug.Log("Footstep effect cannot play - no available audio sources");
		}
		
		/* Play the footstep effec't using the found audio source */
		else{
			source.clip = stepClips[stepEffectIndex];
			source.Play();
			lastStepClipIndex = stepEffectIndex;
		}
	}
	
	
	/* ----------- Play Music Functions ------------------------------------------------------------- */
	
	public void PlayMusic(){
		/*
		 * Take a random song clip and play it for the game's music
		 */
		int songIndex;
		
		/* Get the index of a new song */
		songIndex = RandomClip(musicClips, lastMusicClipIndex);

        /* Update the musicSource with the new song */
		musicSource.clip = musicClips[songIndex];
        musicSource.Play();
		lastMusicClipIndex = songIndex;
	}
	
	public void EnterFastFall(){
		/* 
		 * As the player enters the fastfalling state, the game music should switch over to
		 * the "sound of falling". This is done by fading out the music and in the falling audio.
		 */
		
		/* fade out the music */
		musicFade = -1;
		
		/* Start and fade in the FastFalling state audio */
		fallingSource.clip = fallingClip;
		fallingSource.volume = 0;
		fallingFade = 1;
        fallingSource.Play();
	}
	
	public void PlayLanding(){
		/*
		 * When landing from the FastFall state, play a landing sound and start the music again.
		 */
		
		/* Fade in the music */
		PlayMusic();
		musicFade = 1;
		
		/* Stop the falling audio */
		fallingSource.Stop();
		
		/* Play the landing audio */
		landingSource.clip = landingClip;
		landingSource.Play();
	}
	
	
	/* ----------- Audio Mixing Functions ------------------------------------------------------------- */
	
	void ApplyMaxVolume(){
		/*
		 * Apply the max volume to all audio sources.
		 * Should only ever be run at startup.
		 */
		
		for(int i = 0; i < stepSources.Length; i++){
            stepSources[i].volume = maxVolume;
		}
		musicSource.volume = maxVolume;
		fallingSource.volume = maxVolume;
		landingSource.volume = maxVolume;
	}
	
	void UpdateSourceVolume(){
		/*
		 * Change the volume of an audio source, simulating a fade effect on the clip.
         * Use a generalized UpdateSourceVolume function for each potential fade source.
		 */
		
		UpdateSourceVolume(ref musicFade, musicSource);
		UpdateSourceVolume(ref fallingFade, fallingSource);
	}

    void UpdateSourceVolume(ref int fade, AudioSource source) {
        /*
         * A generalized function that will either fade in or fade out 
         * the given source using the given fade value.
         */

		if(fade != 0){
			source.volume = source.volume + maxVolume*fadeRate*fade;
			
			/* Stop the fading if the sources reaches max volume or is fully muted */
			if(source.volume <= 0){
				source.Stop();
				source.volume = 0;
				fade = 0;
			}
			else if(source.volume >= maxVolume){
				source.volume = maxVolume;
				fade = 0;
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
