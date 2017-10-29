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
    private AudioSource[] upperStepSource;
    private AudioSource[] lowerStepSource;
    private AudioSource musicSource;
	private AudioSource landingSource;
	private AudioSource fallingSource;
	
	
	/* --- Audio Filters ------------------- */
	private AudioReverbFilter[] stepFilters;    
	private AudioReverbFilter musicFilter;
	private AudioReverbFilter landingFilter;     
	private AudioReverbFilter fallingFilter;


    /* --- Audio Clips ------------------- */
    public AudioClip[] upperStepClips;
    public AudioClip[] lowerStepClips;
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

        int stepArrayLength = Mathf.Min(upperStepClips.Length, lowerStepClips.Length);
        if(upperStepClips.Length != lowerStepClips.Length) {
            Debug.Log("Warning: Footstep audio clip count does not match");
        }
         
        /* Set the clip index trackers. No matter what value they are, the final clip will always get culled at first */
        lastStepClipIndex = upperStepClips.Length;
        lastMusicClipIndex = musicClips.Length;

        /* Create the appropriate amount of audioSources and put them in their corresponding containers*/
        InitializeStepArray(ref upperStepSource, ref lowerStepSource, ref stepContainers, ref stepFilters, upperStepClips.Length);
		InitializeAudioObject(ref musicSource, ref musicContainer, ref musicFilter, "Music");
		InitializeAudioObject(ref fallingSource, ref fallingContainer, ref fallingFilter, "Falling");
		InitializeAudioObject(ref landingSource, ref landingContainer, ref landingFilter, "Landing");
		
		/* Initialize the fade values for the steps */
		stepFade = new float[stepArrayLength];
		stepFadeDelay = new int[stepArrayLength];
		for(int i = 0; i < stepArrayLength; i++){
			stepFade[i] = 0f;
			stepFadeDelay[i] = 0;
		}
		
        /* Apply the maxVolume to each audioSource */
        for(int i = 0; i < stepArrayLength; i++){
            upperStepSource[i].volume = maxVolume;
            lowerStepSource[i].volume = maxVolume;

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

    public void PlayFootstep(float lastStepTime, float stepHeight) {
        /*
		 * Play a sound effect of a footstep. The given parameters will apply 
		 * effects to the clip, such as pitch shifting or volume control.
		 *
		 * lastStepTime is the amount of time between the steps. This controls
		 * the volume and the length of the footstep effect. A longer step will 
		 * be louder and spend more time playing an echo.
		 * 
		.* stepHeight controls the tone of the step. This is done by having
		 * each footstep sound effect split the highs and lows into sepperate clips.
		 * By controlling the volume of each part, we can systematically control the tone.
		 */
        int sourceIndex = UnusedSoundSource(upperStepSource);
        int stepEffectIndex = RandomClip(upperStepClips, lastStepClipIndex);

        /* Play the footstep effect using the found audio source and clip */
        if(sourceIndex != -1) {


            /* Short steps will begin fading away much earlier than longer steps */
            float fadeMin = 0.6f;
            float fadeMax = 0.9f;
            int clipSampleSize = upperStepClips[stepEffectIndex].samples;
            if(lastStepTime < fadeMin) {
                stepFadeDelay[stepEffectIndex] = clipSampleSize/8;
            }
            else if(lastStepTime < fadeMax) {
                stepFadeDelay[stepEffectIndex] = clipSampleSize/4;
            }
            else {
                stepFadeDelay[stepEffectIndex] = clipSampleSize;
            }


            /* The time between the steps directly effects the volume of the step */
            float minTime = 0.4f;
            float maxTime = 1.1f;
            float maxVolumeLoss = 0.7f;
            float volumePower = maxVolume*(1 - maxVolumeLoss);
            if(lastStepTime < minTime) {
                volumePower += maxVolume*0;
            }
            else if(lastStepTime < maxTime) {
                volumePower += maxVolume*maxVolumeLoss*RatioWithinRange(minTime, maxTime, lastStepTime);
            }
            else {
                volumePower  += maxVolume*maxVolumeLoss;
            }


            //Splt the volue across the step's multiple sources.
            //if stepHeight is 0, the split is 50/50.Leave it at only that for jow
            float volumeRatio = 0.5f;



            AudioSource upperSource = upperStepSource[sourceIndex];
            AudioSource lowerSource = lowerStepSource[sourceIndex];
            /* Set the volume of the sources using the power and ratio */
            upperSource.volume = volumePower*volumeRatio;
            lowerSource.volume = volumePower*(1 - volumeRatio);

            /* Set the proper clips for the sources */
            upperSource.clip = upperStepClips[stepEffectIndex];
            lowerSource.clip = lowerStepClips[stepEffectIndex];

            /* Play the full footstep sound clip with the added audio effects */
            upperSource.Play();
            lowerSource.Play();
            lastStepClipIndex = stepEffectIndex;
            Debug.Log(lastStepTime);
        }

        else {
            Debug.Log("Footstep effect cannot play - no available audio sources");
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
	
	void ApplyFade(){
		/*
		 * Change the volume of an audio source, simulating a fade effect on the clip.
         * Use a generalized UpdateSourceVolume function for each potential fade source.
		 */
		
		ApplyFade(ref musicFade, musicSource);
		ApplyFade(ref fallingFade, fallingSource);
		
        /* Check if a fade effect needs to be applied to any of the footstep sources */
		for(int i = 0; i < upperStepSource.Length; i++){
            if(upperStepSource[i].isPlaying) {

                /* Check if the playing source is past the stepDelay */
                if(upperStepSource[i].timeSamples >= stepFadeDelay[i]) {
                    stepFade[i] = -10;
                }

                ApplyFade(ref stepFade[i], upperStepSource[i]);
                ApplyFade(ref stepFade[i], lowerStepSource[i]);
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
	
	public int UnusedSoundSource(AudioSource[] sourceArray){
        /*
		 * Search the given array of soundSources and return the first one
		 * that is not currently playing a sound. Return null if none are free.
		 */
        int freeSource = -1;
		
		for(int i = 0; (i < sourceArray.Length && freeSource == -1); i++){
			if(sourceArray[i].isPlaying == false){
				freeSource = i;
			}
		}
		
		return freeSource;
	}
	




    void InitializeStepArray(ref AudioSource[] upperSource, ref AudioSource[] lowerSource, 
            ref GameObject[] containerArray, ref AudioReverbFilter[] filterArray, int sourceSize) {
        /*
         * Create the audio sources that play the footsteps and the containers that hold them.
         */

        upperSource = new AudioSource[sourceSize];
        lowerSource = new AudioSource[sourceSize];
        containerArray = new GameObject[sourceSize];
        filterArray = new AudioReverbFilter[sourceSize];
        for(int i = 0; i < sourceSize; i++) {
            containerArray[i] = new GameObject();
            containerArray[i].transform.parent = sourceContainer.transform;
            containerArray[i].name = name;

            upperSource[i] = containerArray[i].AddComponent<AudioSource>();
            lowerSource[i] = containerArray[i].AddComponent<AudioSource>();
            filterArray[i] = containerArray[i].AddComponent<AudioReverbFilter>();
            filterArray[i].enabled = false;
        }
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
