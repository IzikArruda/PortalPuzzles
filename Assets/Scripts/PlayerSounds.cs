using UnityEngine;
using System.Collections;

/*
 * Controls how all sounds are played for the player. 
 * This inludes sound effects and music. The playerController
 * will call functions from this script when requesting to play a sound.
 * Each sound effect will have it's own audioSource and gameObject.
 */
public class PlayerSounds : MonoBehaviour {

	
	/* --- Source Containers ------------------- */
	private GameObject[] stepContainers; 
	private GameObject musicContainer; 
	private GameObject landingContainer;
	private GameObject fallingContainer;
	/* The main soundEffect container that contains all the source's containers */
	public GameObject sourceContainer;
	
	
	/* --- Audio Sources ------------------- */
	private AudioSource[] stepSources;    
	private AudioSource musicSource;
	private AudioSource landingSource;
	private AudioSource fallingSource;
	
	
	/* --- Audio Filters ------------------- */
	private AudioReverbFilter[] stepFilters;    
	private AudioReverbFilter musicFilter;
	private AudioReverbFilter landingFilter;     
	private AudioReverbFilter fallingFilter;
	
	
	/* --- Audio Clips ------------------- */
	public AudioClip[] stepClips;
	public AudioClip[] musicClips;
	public AudioClip landingClip;
	//fastfall audio : bus and jet engine?
	public AudioClip fallingClip;
	
	
	/* --- User Input Values ------------------- */
    /* How loud the volume of the audio is at max */
    public float maxVolume = 1;
    /* How fast the audio fades universally. Rate is relative to maxVolume  */
    [Range(1, 0)]
    public float fadeRate = 0.01f;
    /* limit the amout of footstep effects from playing at once by limiting stepSources size */
    public int maxSimultaniousStepEffects;


    /* --- Misc ------------------- */
    /* Track the index of the previously used clip to prevent repeated plays */
    private int lastStepClipIndex;
    private int lastMusicClipIndex;
    /* 0: Nothing. -x: Fade out the music. +x: Fade in the music. Relative to fadeRate. */
    private float musicFade = 0;
	private float fallingFade = 0;
    /* The stepFade is set by the script as it varies between sources */
    private float[] stepFade;
    /* How many samples into the clip a footstep effect needs to be before the fade begins */
	private int[] stepFadeDelay;
	

    /* ----------- Built-in Unity Functions ------------------------------------------------------------- */

    void Start(){
        /*
		 * Initialize the audio objects and assign variables to their default
		 */
         
        /* Set the clip index trackers. No matter what value they are, the final clip will always get culled at first */
        lastStepClipIndex = stepClips.Length;
        lastMusicClipIndex = musicClips.Length;

        /* Create the appropriate amount of audioSources and put them in their corresponding containers*/
		InitializeAudioArray(ref stepSources, ref stepContainers, ref stepFilters, maxSimultaniousStepEffects, "Footsteps");
		InitializeAudioObject(ref musicSource, ref musicContainer, ref musicFilter, "Music");
		InitializeAudioObject(ref fallingSource, ref fallingContainer, ref fallingFilter, "Falling");
		InitializeAudioObject(ref landingSource, ref landingContainer, ref landingFilter, "Landing");
		
		/* Initialize the fade values for the steps */
		stepFade = new float[stepSources.Length];
		stepFadeDelay = new int[stepSources.Length];
		for(int i = 0; i < stepSources.Length; i++){
			stepFade[i] = 0f;
			stepFadeDelay[i] = 0;
		}
		
        /* Apply the maxVolume to each audioSource */
        for(int i = 0; i < stepSources.Length; i++){
            stepSources[i].volume = maxVolume;
		}
		musicSource.volume = maxVolume;
		fallingSource.volume = maxVolume;
		landingSource.volume = maxVolume;
	}
	
	void Update(){
		
		/* Adjust the volume of audio sources if needed */
		/* Apply any fade effects for the frame */
		ApplyFade();
        

    }
	
	
    /* ----------- Play Sounds Functions ------------------------------------------------------------- */
	
	public void PlayFootstep(float lastStepTime, float stepHeight){
		/*
		 * Play a sound effect of a footstep. The given parameters will apply 
		 * effects to the clip, such as pitch shifting or volume control.
		 * 
		 * lastStepTime is the amount of time between the steps. This controls
		 * the duration of the clip where steps with a short time will play 
		 * quick clips that get cut off early through a fade effect. The longer
		 * steps will have an echo effect along with a higher volume.
		 *
		 * stepHeight will change the tone of the footstep. stepheight is directly
		 * proportional to the height change the player underwent after taking a step.
		 * Stepping up will play a "higher" sound while stepping down will play a "low" sound.
		 * If tone/pitch cant be implemented, we can use two clips of the same step -
		 * one high and one low. Play them at the same time and adjust their 
		 * individual volumes to control the overall sound.
		 */
		AudioSource source = null;
		int stepEffectIndex;
	
		/* Pick a random footstep effect if theres more than 1 to choose from */
		stepEffectIndex = RandomClip(stepClips, lastStepClipIndex);
		
		/* Search the footstep audioSources for one not currently in use */
		source = UnusedSoundSource(stepSources);
		
		
		if(source == null){
			Debug.Log("Footstep effect cannot play - no available audio sources");
		}
		
		/* Play the footstep effect using the found audio source and apply the effects */
		else{
		
			/* Set the volume of the clip to be relative to the maxVolume and the given ratio */
			source.volume = maxVolume*1;
            
            /* Short steps will begin fading away much earlier than longer steps */
            float fadeMin = 0.6f;
            float fadeMax = 0.9f;
            if(lastStepTime < fadeMin) {
                stepFadeDelay[stepEffectIndex] = stepClips[stepEffectIndex].samples/8;
            }
            else if(lastStepTime < fadeMax) {
                stepFadeDelay[stepEffectIndex] = stepClips[stepEffectIndex].samples/4;
            }
            else {
                stepFadeDelay[stepEffectIndex] = stepClips[stepEffectIndex].samples;
            }
            
            /* The time between the steps directly effects the volume of the step */
            float minTime = 0.4f;
            float maxTime = 1.1f;
            float minVolume = 0.3f;
            if(lastStepTime < minTime) {
                source.volume = maxVolume*minVolume;
            }
            else if(lastStepTime < maxTime) {
                source.volume = maxVolume*minVolume + maxVolume*(maxVolume - minVolume)*RatioWithinRange(minTime, maxTime, lastStepTime);
            }
            else {
                source.volume  = maxVolume*1;
            }
            
            /* Play the clip */
            source.clip = stepClips[stepEffectIndex];
			source.Play();
			lastStepClipIndex = stepEffectIndex;

            Debug.Log(lastStepTime);
		}
	}
	
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
	
	public void PlayFastFall(){
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
		
		
	}
	
	void ApplyFade(){
		/*
		 * Change the volume of an audio source, simulating a fade effect on the clip.
         * Use a generalized UpdateSourceVolume function for each potential fade source.
		 */
		
		ApplyFade(ref musicFade, musicSource);
		ApplyFade(ref fallingFade, fallingSource);
		
        /* Check if a fade effect needs to be applied to any of the footstep sources */
		for(int i = 0; i < stepSources.Length; i++){
            if(stepSources[i].isPlaying) {

                /* Check if the playing source is past the stepDelay */
                if(stepSources[i].timeSamples >= stepFadeDelay[i]) {
                    stepFade[i] = -10;
                }
                ApplyFade(ref stepFade[i], stepSources[i]);
            }
		}
	}

    void ApplyFade(ref float fade, AudioSource source) {
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
	
	void InitializeAudioArray(ref AudioSource[] sourceArray, ref GameObject[] containerArray, ref AudioReverbFilter[] filterArray, int sourceSize, string name){
		/*
		 * Initialize the given arrays with audioSources and their containers
		 */
		
		sourceArray = new AudioSource[sourceSize];
        containerArray = new GameObject[sourceSize];
		filterArray = new AudioReverbFilter[sourceSize];
        for(int i = 0; i < sourceArray.Length; i++){
        	InitializeAudioObject(ref sourceArray[i], ref containerArray[i], ref filterArray[i], name);
        }
	}
	
	void InitializeAudioObject(ref AudioSource source, ref GameObject container, ref AudioReverbFilter filter, string name) {
		/*
		 * Initialize the given audio objects together
		 */
		
		/* Create the container and place it into the main audio container */
		container = new GameObject();
        container.transform.parent = sourceContainer.transform;
        container.name = name;

        /* Add the audioSource and any required filters */
        source = container.AddComponent<AudioSource>();
        filter = container.AddComponent<AudioReverbFilter>();
        filter.enabled = false;

    }

    float RatioWithinRange(float min, float max, float value) {
        /*
         * Return the ratio of the value between min and max. Returns 0 if
         * value is equal to or less than min, 1 if value is more or equal to max.
         * 0.5 if it is equally between both min and max.
         */
        float ratio;

        if(value < min) {
            ratio = 0;
        }
        else if(value > max) {
            ratio = 1;
        }
        else {
            ratio = (value - min)/(max - min);
        }

        return ratio;
    }
}
