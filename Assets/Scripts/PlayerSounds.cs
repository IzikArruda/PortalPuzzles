using UnityEngine;
using System.Collections;

/*
 * Controls how all sounds are played for the player. 
 * This inludes sound effects and music. The playerController
 * will call functions from this script when requesting to play a sound.
 * Each sound effect will have it's own audioSource and gameObject.
 * To properly play a song, we need a clip to have two versions:
 * a muted and an upgraded version.
 */
public class PlayerSounds : MonoBehaviour {


    /* --- Source Containers ------------------- */
    private GameObject[] upperStepContainers;
    private GameObject[] lowerStepContainers;
	private GameObject[] landingContainers;
    private GameObject musicContainerMuted;
    private GameObject musicContainerUpgraded;
    private GameObject fallingContainer;
	/* The main soundEffect container that contains all the source's containers */
	public GameObject sourceContainer;


    /* --- Audio Sources ------------------- */
    private AudioSource[] upperStepSources;
    private AudioSource[] lowerStepSources;
	private AudioSource[] landingSources;
    private AudioSource musicSourceMuted;
    private AudioSource musicSourceUpgraded;
    private AudioSource fallingSource;
	
	
	/* --- Audio Filters ------------------- */
    private AudioHighPassFilter[] stepHighPass;
    private AudioLowPassFilter[] stepLowPass;


    /* --- Audio Clips ------------------- */
    public AudioClip[] stepClips;
    public AudioClip[] landingClips;
    public AudioClip[] musicClipsMuted;
    public AudioClip[] musicClipsUpgraded;
	public AudioClip hardLandingClip;
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
    private int lastStepClipIndex = -1;
    private int lastMusicClipIndex = -1;
    /* 0: Nothing. -x: Fade out the clip. +x: Fade in the clip. Relative to fadeRate. */
    private float fallingFade = 0;
    private float musicFadeMuted;
    private float musicFadeUpgraded;
    /* The stepFade is set by the script as it varies between sources */
    private float[] stepFade;
    /* How many samples into the clip a footstep effect needs to be before the fade begins */
	private int[] stepFadeDelay;
	

    /* ----------- Built-in Unity Functions ------------------------------------------------------------- */

    void Start(){
        /*
		 * Initialize the audio objects and assign variables to their default
		 */

        /* Create the appropriate amount of audioSources and put them in their corresponding containers*/
        InitializeStepArray(ref upperStepSources, ref lowerStepSources, ref upperStepContainers, ref lowerStepContainers,
                    ref stepHighPass, ref stepLowPass, maxSimultaniousStepEffects);
        InitializeAudioObject(ref musicSourceMuted, ref musicContainerMuted, "Music(muted)");
        InitializeAudioObject(ref musicSourceUpgraded, ref musicContainerUpgraded, "Music(upgraded)");
        InitializeAudioObject(ref fallingSource, ref fallingContainer, "Falling");
		InitializeAudioArray(ref landingSources, ref landingContainers, 3, "Landing");
		
		/* Initialize the fade values for the steps */
		stepFade = new float[maxSimultaniousStepEffects];
		stepFadeDelay = new int[maxSimultaniousStepEffects];
		for(int i = 0; i < maxSimultaniousStepEffects; i++){
			stepFade[i] = 0f;
			stepFadeDelay[i] = 0;
		}

        /* Initialize the fade values for the music */
        musicFadeMuted = 0;
        musicFadeUpgraded = 0;

        /* Apply the maxVolume to each audioSource */
        for(int i = 0; i < maxSimultaniousStepEffects; i++){
            upperStepSources[i].volume = maxVolume;
            lowerStepSources[i].volume = maxVolume;

        }
        for(int i = 0; i < landingSources.Length; i++){
        	landingSources[i].volume = maxVolume;
        }
        musicSourceMuted.volume = maxVolume;
        musicSourceUpgraded.volume = maxVolume;
		fallingSource.volume = maxVolume;

        //Play an upgraded song at the start
        PlayMusic(1);
	}
	
	void Update(){
		
		/* Adjust the volume of audio sources if needed */
		/* Apply any fade effects for the frame */
		ApplyFade();
        

    }


    /* ----------- Play Sounds Functions ------------------------------------------------------------- */

    public void PlayFootstep(float lastStepTime, float stepHeight) {
        /*
         * Use the given values to derive the required parameters 
         * to properly send a request to play a footstep.
         *
		 * lastStepTime is the amount of time between the steps. This controls
		 * the volume and the length of the footstep effect. A longer step will 
		 * be louder and spend more time playing an echo.
		 * 
		.* stepHeight controls the tone of the step. This is done by having
		 * each footstep sound effect split the highs and lows into sepperate clips.
		 * By controlling the volume of each part, we can systematically control the tone.
		 */
        int sourceIndex = UnusedSoundSource(upperStepSources);
        int stepEffectIndex = RandomClip(stepClips, lastStepClipIndex);
		float volumeRatio;
		float footstepToneRatio;
		float sampleRatio;

		/* VolumePowerRatio meassures how loud the footstep effect will be */
		float minTime = 0.4f;
        float maxTime = 1.1f;
        float maxVolumeLoss = 0.7f;
        volumeRatio = 1 - maxVolumeLoss;
        if(lastStepTime < minTime) {
            volumeRatio += 0;
        }
        else if(lastStepTime < maxTime) {
            volumeRatio += maxVolumeLoss*CustomPlayerController.RatioWithinRange(minTime, maxTime, lastStepTime);
        }
        else {
            volumeRatio += maxVolumeLoss;
        }
        
        /* footstepToneRatio controls the pitch of the step. Higher value = higher tone */
        float minHeight = 0.1f;
        float maxHeight = 0.5f;
        float maxRatio = 0.25f;
        footstepToneRatio = 0.5f;
        if(Mathf.Abs(stepHeight) < minHeight) {
        	/* Keep the ratio on it's default */
        }
        else if(Mathf.Abs(stepHeight) < maxHeight) {
            /* Increase the ratio as the steHeight Increases */
            footstepToneRatio += maxRatio*CustomPlayerController.RatioWithinRange(minHeight, maxHeight, Mathf.Abs(stepHeight))*Mathf.Sign(stepHeight);
        }
        else {
        	/* Ratio is at it's max difference */
            footstepToneRatio += maxRatio*Mathf.Sign(stepHeight);
        }
        
	 	/* sampleRatio inidactes how far into the clip the fade effect will begin */
        float fadeMin = 0.6f;
        float fadeMax = 0.9f;
        sampleRatio = 1;
        if(lastStepTime < fadeMin) {
            sampleRatio = 1f/8f;
        }
        else if(lastStepTime < fadeMax) {
            sampleRatio = 1f/4f;
        }
        else {
            sampleRatio = 1;
        }
        
        /* Send a request to play a footstep with the calculated parameters */
        PlayFootstep(volumeRatio, footstepToneRatio, sampleRatio, 0);
        Debug.Log(lastStepTime);
    }

    public void PlayFootstep(float volumeRatio, float toneRatio, float fadeDelayRatio, float playDelay) {
        /*
         * Play a sound effect of a footstep. The given parameters will apply 
		 * effects to the clip, such as pitch shifting, volume and delay.
         */
         
        /* Get the index of a free audioSource to use */
        int sourceIndex = UnusedSoundSource(upperStepSources);
        int clipIndex = RandomClip(stepClips, lastStepClipIndex);

        /* Play the footstep effect using the found audio source and clip */
        if(sourceIndex != -1){
        	/* Get both sources that have both upper and lower tones of the step */
        	AudioSource upperSource = upperStepSources[sourceIndex];
            AudioSource lowerSource = lowerStepSources[sourceIndex];
            
            /* Set the volume of the footstep to be relative to the maxVolume */
            upperSource.volume = maxVolume*volumeRatio;
            lowerSource.volume = maxVolume*volumeRatio;

			/* Set the tone of the overall footstep sound by having different volumes */
			upperSource.volume *= toneRatio;
            lowerSource.volume *= (1 - toneRatio);

            /* Set the fade's starting point on the step*/
            stepFadeDelay[sourceIndex] = Mathf.FloorToInt(stepClips[clipIndex].samples*fadeDelayRatio);
            stepFade[sourceIndex] = 0;

            /* Set the proper clips for the sources */
            upperSource.clip = stepClips[clipIndex];
            lowerSource.clip = stepClips[clipIndex];

            /* Play the full footstep sound clip with the given delay */
            upperSource.PlayDelayed(playDelay);
            lowerSource.PlayDelayed(playDelay);
            
            /* Remember the index of the clip used to avoid it next step */
            lastStepClipIndex = clipIndex;
        }
        else{
        	Debug.Log("Footstep effect cannot play - no available audio sources");
        }
    }

    public void PlayMusic(int upgraded){
		/*
         * Get a random song clip and play it's muted and upgraded versions using both musicSources.
         * The given boolean controls the volumes of both sources.
         * -1 = only hear the non-upgraded song
         * 0 = set both volumes to 0
         * 1 = only hear the upgraded song
         */
		int songIndex;
        
        /* Get the index of a new clip */
        songIndex = RandomClip(musicClipsMuted, lastMusicClipIndex);

        /* Play the clip and it's upgraded version using the two musicSources*/
        musicSourceMuted.clip = musicClipsMuted[songIndex];
        musicSourceUpgraded.clip = musicClipsUpgraded[songIndex];

        /* Set the volume of the clips */
        musicSourceMuted.volume = -upgraded*maxVolume;
        musicSourceUpgraded.volume = upgraded*maxVolume;

        /* play the clips and track the clip's index */
        musicSourceMuted.Play();
        musicSourceUpgraded.Play();
        lastMusicClipIndex = songIndex;
	}
	
	public void PlayFastFall(){
        /* 
		 * As the player enters the fastfalling state, the game music should switch over to
		 * the "sound of falling". This is done by fading out the music and in the falling audio.
		 */

        /* fade out the music */
        musicFadeMuted = -0.25f;
        musicFadeUpgraded = -0.25f;

        /* Start and fade in the FastFalling state audio */
        fallingSource.clip = fallingClip;
		fallingSource.volume = 0;
        fallingSource.loop = true;
        fallingFade = 0.025f;
        fallingSource.Play();
	}
	
    public void PlayHardLanding() {
        /*
		 * When landing from the FastFall state, play the hardLanding clip and start the music again.
		 */
		int sourceIndex = UnusedSoundSource(landingSources);
		AudioSource landingSource;
		
		/* Use a landing audioSource to play the hardLanding sound */
		if(sourceIndex != -1){
			landingSource = landingSources[sourceIndex];
			landingSource.volume = maxVolume;
			landingSource.clip = hardLandingClip;
			landingSource.Play();
		}
		else{
			Debug.Log("Landing effect cannot play - no available audio sources");
		}
		
		/* Stop playing the fastFalling audio and fade in the music */
        fallingFade = -2.5f;
        PlayMusic(0);
        /* Fade in the non-upgraded music */
        musicFadeMuted = 0.1f;
    }


    /* ----------- Audio Mixing Functions ------------------------------------------------------------- */

    void ApplyFade(){
		/*
		 * Change the volume of an audio source, simulating a fade effect on the clip.
         * Use a generalized UpdateSourceVolume function for each potential fade source.
		 */
		
        /* Apply the fade effect to the fallingSources */
		ApplyFade(ref fallingFade, fallingSource);
        

        /* Apply the fade effect to the two musicSources */
        ApplyFade(ref musicFadeMuted, musicSourceMuted);
        ApplyFade(ref musicFadeUpgraded , musicSourceUpgraded);


        /* Check if a fade effect needs to be applied to any of the footstep sources */
        for(int i = 0; i < maxSimultaniousStepEffects; i++){
            if(upperStepSources[i].isPlaying) {

                /* Check if the playing source is past the stepDelay */
                if(upperStepSources[i].timeSamples >= stepFadeDelay[i]) {
                    stepFade[i] = -10;
                }


                float upperFade = stepFade[i];
                float lowerFade = stepFade[i];
                ApplyFade(ref upperFade, upperStepSources[i]);
                ApplyFade(ref lowerFade, lowerStepSources[i]);
                /* Only change the real fade value if both values are identical.
                 * This will prevent one clip from ending earlier and forcing the other to stop too. */
                if(upperFade == lowerFade) {
                    stepFade[i] = upperFade;
                }
            }
		}
	}

    void ApplyFade(ref float fade, AudioSource source) {
        /*
         * A generalized function that will either fade in or fade out 
         * the given source using the given fade value.
         */
         //maybe ad an option to take in two sources

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

    void SetMusicFade(int fade) {
        /*
         * Set the fade value of both music sources. This will allow the game to swap
         * between the upgraded and normal versions of a song.
         * -1 = fade out the upgraded version, fade in the normal version
         * 1 = fade out the normal version, fade in the upgraded version
         */

        musicFadeMuted = -fade;
        musicFadeUpgraded = fade;
    }

    /* ----------- Helper Functions ------------------------------------------------------------- */

    public int RandomClip(AudioClip[] clips, int previousClipIndex){
		/*
		 * Using the given array of clips, pick the index of a random clip.
		 * Do not include the clip given by the given int previousClip.
		 * 
		 * If a previousClipIndex given is -1, do not cull a clip 
		 */
		int randomIndex = 0;
		
		/* Pick a random index if there exists more than 1 clip to choose from */
		if(clips.Length > 1 && previousClipIndex != -1){
		
			/* Get a random integer between 0 and X-1 where X is the amount of unique footstep sounds */
			randomIndex = Random.Range(0, clips.Length-2);
		
			/* If the effect's index is equal or above the previous played effect, increase it by 1 */
			if(randomIndex >= previousClipIndex){
				randomIndex++;
			}
		}
		 /* Choose a truly random clip */
		else if(previousClipIndex == -1){
			randomIndex = Random.Range(0, clips.Length-1);
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
            ref GameObject[] upperContainerArray, ref GameObject[] lowerContainerArray,
            ref AudioHighPassFilter[] highPassFilter, ref AudioLowPassFilter[] lowerPassFilter, int sourceSize) {
        /*
         * Create the audio sources that play the footsteps, the containers that hold them
         * and the low and high pass filters that control the tone of the footstep.
         */

        upperSource = new AudioSource[sourceSize];
        lowerSource = new AudioSource[sourceSize];

        upperContainerArray = new GameObject[sourceSize];
        lowerContainerArray = new GameObject[sourceSize];

        highPassFilter = new AudioHighPassFilter[sourceSize];
        lowerPassFilter = new AudioLowPassFilter[sourceSize];

        for(int i = 0; i < sourceSize; i++) {
            InitializeAudioObject(ref upperSource[i], ref upperContainerArray[i], "Footstep(high) " + i);
            InitializeAudioObject(ref lowerSource[i], ref lowerContainerArray[i], "Footstep(low) " + i);

            highPassFilter[i] = upperContainerArray[i].AddComponent<AudioHighPassFilter>();
            lowerPassFilter[i] = lowerContainerArray[i].AddComponent<AudioLowPassFilter>();

            highPassFilter[i].cutoffFrequency = 1000;
            lowerPassFilter[i].cutoffFrequency = 1000;
        }
    }


    void InitializeAudioArray(ref AudioSource[] sourceArray, ref GameObject[] containerArray, int sourceSize, string name){
		/*
		 * Initialize the given arrays with audioSources and their containers
		 */
		
		sourceArray = new AudioSource[sourceSize];
        containerArray = new GameObject[sourceSize];
        for(int i = 0; i < sourceArray.Length; i++){
        	InitializeAudioObject(ref sourceArray[i], ref containerArray[i], name + " " + i);
        }
	}
	
	void InitializeAudioObject(ref AudioSource source, ref GameObject container, string name) {
		/*
		 * Initialize the given container and add the given source
         * Create the given container, place it in the main audio container and add it's audioSource
		 */
		
		container = new GameObject();
        container.transform.parent = sourceContainer.transform;
        container.name = name;
        source = container.AddComponent<AudioSource>();
    }
}
