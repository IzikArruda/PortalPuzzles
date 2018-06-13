using UnityEngine;
using UnityEngine.Audio;
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


    /* -- AudioMixer groups ------------------- */
    public AudioMixer audioMixer;
    public AudioMixerGroup audioMixerMaster;
    public AudioMixerGroup audioMixerMusic;
    public AudioMixerGroup audioMixerFootsteps;


    /* --- Source Container Containers ------------------- */
    public GameObject stepTransformContainer;
    public GameObject musicTransformContainer;
    public GameObject effectsTransformContainer;


    /* --- Source Containers ------------------- */
    private GameObject[] upperStepContainers;
    private GameObject[] lowerStepContainers;
	private GameObject[] landingContainers;
    private GameObject musicContainerMuted;
    private GameObject musicContainerUpgraded;
    private GameObject fallingContainer;
    private GameObject meunContainer;
	/* The main soundEffect container that contains all the source's containers */
	public GameObject sourceContainer;
    

    /* --- Audio Sources ------------------- */
    private AudioSource[] upperStepSources;
    private AudioSource[] lowerStepSources;
	private AudioSource[] landingSources;
    private AudioSource musicSourceMuted;
    private AudioSource musicSourceUpgraded;
    private AudioSource fallingSource;
    private AudioSource menuSource;
	
	
	/* --- Audio Filters ------------------- */
    private AudioHighPassFilter[] stepHighPass;
    private AudioLowPassFilter[] stepLowPass;


    /* --- Audio Clips ------------------- */
    public AudioClip[] stepClips;
    public AudioClip[] marbleStepClips;
    public AudioClip[] carpetStepClips;
    public AudioClip[] landingClips;
    public AudioClip[] musicClipsMuted;
    public AudioClip[] musicClipsUpgraded;
    public AudioClip outsideSounds;
    public AudioClip menuClickClip;
    public AudioClip startingMusic;
	public AudioClip hardLandingClip;
	public AudioClip fallingClip;


    /* --- User Input Values ------------------- */
    /* Audio volumes directly linked to the audio mixer */
    public float masterMixerVolume;
    public float musicMixerVolume;
    public float footstepsMixerVolume;
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
    private float fallingFade;
    private float musicFadeMuted;
    private float musicFadeUpgraded;
    /* The stepFade is set by the script as it varies between sources */
    private float[] stepFade;
    /* How many samples into the clip a footstep effect needs to be before the fade begins */
	private int[] stepFadeDelay;

    /* When the player is in the outside state, handle certain sounds differently, such as playing upgraded music instead of muted */
    private bool outside = false;

    /* Used to prevent the songs from constantly sending a PlayDelay every update call */
    private bool delayedPlayMuted = false;
    private bool delayedPlayUpgraded = false;

    /* Track what songs have been previously played. This will ensure the player hears each song atleast once */
    private bool[] previouslyPlayed;


    /* ----------- Built-in Unity Functions ------------------------------------------------------------- */

    void Start(){
        /*
		 * Initialize the audio objects and assign variables to their default
		 */

        /* Create the appropriate amount of audioSources and put them in their corresponding containers*/
        InitializeStepArray(ref upperStepSources, ref lowerStepSources, ref upperStepContainers, ref lowerStepContainers,
                    ref stepHighPass, ref stepLowPass, maxSimultaniousStepEffects);
        InitializeAudioObject(ref musicSourceMuted, ref musicContainerMuted, "Music(muted)", musicTransformContainer.transform, audioMixerMusic);
        InitializeAudioObject(ref musicSourceUpgraded, ref musicContainerUpgraded, "Music(upgraded)", musicTransformContainer.transform, audioMixerMusic);
        InitializeAudioObject(ref fallingSource, ref fallingContainer, "Falling", effectsTransformContainer.transform, audioMixerMusic);
		InitializeAudioArray(ref landingSources, ref landingContainers, 3, "Landing", effectsTransformContainer.transform, audioMixerFootsteps);
        InitializeAudioObject(ref menuSource, ref meunContainer, "Menu", effectsTransformContainer.transform, audioMixerFootsteps);

        /* Initialize the previously played song tracker */
        previouslyPlayed = new bool[musicClipsMuted.Length];
        for(int i = 0; i < previouslyPlayed.Length; i ++) {
            previouslyPlayed[i] = false;
        }

        /* Initialize the fade values for the steps */
        stepFade = new float[maxSimultaniousStepEffects];
		stepFadeDelay = new int[maxSimultaniousStepEffects];

        /* Set the fade values for all sources to 0 */
        for(int i = 0; i < maxSimultaniousStepEffects; i++) {
            stepFade[i] = 0f;
            stepFadeDelay[i] = 0;
        }
        fallingFade = 0;
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
        menuSource.volume = maxVolume;

        /* Set the volume for the audio mixer */
        ResetAudioMixerVolume();

    }

    void Update(){
		
		/* Apply any fade effects for the frame */
		ApplyFade();
        
        //Check if the player has pressed the P key, which will play a new song.
        if(Input.GetKeyDown("p")){
            PlayMusic();
        }

        /* If the music ever stops playing, have it start over again */
        if((!musicSourceMuted.isPlaying && musicSourceMuted.time == 0) || (!musicSourceUpgraded.isPlaying && musicSourceUpgraded.time == 0)) {
            
            /* Make sure this function was not already called */
            if(delayedPlayMuted == false || delayedPlayUpgraded == false) {
                
                /* If we are using upgraded music, select a new song */
                if(outside && delayedPlayUpgraded == false) {
                    /* Get the index of a new clip/song */
                    int songIndex = GetNewSongIndex();

                    /* Play the clip and it's upgraded version using the two musicSources*/
                    musicSourceMuted.clip = musicClipsMuted[songIndex];
                    musicSourceUpgraded.clip = musicClipsUpgraded[songIndex];
                }

                /* Set the boolean to track that we have started to play new songs on a delay */
                if(!musicSourceMuted.isPlaying) {
                    musicSourceMuted.PlayDelayed(2);
                    delayedPlayMuted = true;
                }
                if(!musicSourceUpgraded.isPlaying) {
                    musicSourceUpgraded.PlayDelayed(2);
                    delayedPlayUpgraded = true;
                }
            }
        }

        /* Reset the delayedPlay variable once the songs start up */
        if(musicSourceMuted.isPlaying) {
            delayedPlayMuted = false;
        }
        if(musicSourceUpgraded.isPlaying) {
            delayedPlayUpgraded = false;
        }
    }


    /* ----------- Play Sounds Functions ------------------------------------------------------------- */

    public void PlayFootstep(float lastStepTime, float stepHeight, int stepType) {
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
        PlayFootstep(volumeRatio, footstepToneRatio, sampleRatio, 0, stepType);
    }

    public void PlayFootstep(float volumeRatio, float toneRatio, float fadeDelayRatio, float playDelay, int stepType) {
        /*
         * Play a sound effect of a footstep. The given parameters will apply 
		 * effects to the clip, such as pitch shifting, volume and delay.
         */
        AudioClip stepClip = null;
        int clipIndex = 0;

        /* Get the index of a free audioSource to use */
        int sourceIndex = UnusedSoundSource(upperStepSources);


        /* Depending on the given stepSoundType, use the proper stepSound array */
        if(stepType == 0) {
            /* Use marble floor steps */
            clipIndex = RandomClip(marbleStepClips, lastStepClipIndex);
            stepClip = marbleStepClips[clipIndex];
        }
        else if(stepType == 1){
            /* Use carpet floot steps */
            clipIndex = RandomClip(carpetStepClips, lastStepClipIndex);
            stepClip = carpetStepClips[clipIndex];
        }
        else {
            Debug.Log("WARNING: GIVEN STEP TYPE DOES NOT HAVE A CLIP ARRAY");
        }


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
            stepFadeDelay[sourceIndex] = Mathf.FloorToInt(stepClip.samples*fadeDelayRatio);
            stepFade[sourceIndex] = 0;

            /* Set the proper clips for the sources */
            upperSource.clip = stepClip;
            lowerSource.clip = stepClip;

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
    
    public void PlayMusic(){
		/*
         * Get a random song clip and restart the music audio sources with a new clip/song.
         * Have the volume of the sources fade in from 0, with the outside state determinign whether
         * we use the upgraded or the muted version.
         */
		int songIndex;
        
        /* Get the index of a new clip/song */
        songIndex = GetNewSongIndex();

        /* Play the clip and it's upgraded version using the two musicSources*/
        musicSourceMuted.clip = musicClipsMuted[songIndex];
        musicSourceUpgraded.clip = musicClipsUpgraded[songIndex];

        /* Set the volume of the clips to reflect the music's upgraded state */
        musicSourceMuted.volume = 0;
        musicSourceUpgraded.volume = 0;
        if(outside) {
            musicFadeUpgraded = 0.5f;
        }else {
            musicFadeMuted = 0.5f;
        }

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
        musicFadeMuted = -0.75f;
        musicFadeUpgraded = -0.75f;

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

        /* Quickly fade out the fastFalling audio */
        fallingFade = -2.5f;

        /* After a hard landing, reset the music if the player is not outside */
        if(!outside) {
            PlayMusic();
        }
    }

    public void PlayMenuClick() {
        /*
         * Play the sound of a menu click
         */

        menuSource.clip = menuClickClip;
        menuSource.Play();
    }

    public void PlayStartupMusic() {
        /*
         * Start fading in the outsideSounds audio clip
         */
         
        musicSourceMuted.volume = 0;
        musicSourceUpgraded.volume = 0;
        musicSourceMuted.clip = outsideSounds;
        musicSourceUpgraded.clip = startingMusic;
        musicSourceMuted.Play();
        SetMusicFade(-0.45f);
    }

    public void PlayIntroMusic() {
        /*
         * Play the starting song by having the outside background sounds fade out and the intro song fade in.
         * Only start playing the starting song once this is called.
         */

        /* Have the intro music play after a delay */
        musicSourceUpgraded.clip = startingMusic;
        musicSourceUpgraded.PlayDelayed(4.5f);
        SetMusicFade(0.25f);
    }

    public void ForceIntroMusic() {
        /*
         * The player has skipped the intro, so force the music to update itself
         */

        musicSourceMuted.volume = 0;
        musicSourceUpgraded.volume = maxVolume;

        /* Force the intro song to play if it has not yet started */
        if(musicSourceUpgraded.time <= 0) {
            musicSourceUpgraded.Play();
        }
    }


    /* ----------- Audio Mixing Functions ------------------------------------------------------------- */

    void ApplyFade(){
		/*
		 * Change the volume of an audio source, simulating a fade effect on the clip.
         * Use a generalized UpdateSourceVolume function for each potential fade source.
		 */
		
        /* Apply the fade effect to the fallingSources */
		ApplyFade(ref fallingFade, fallingSource, true);
        

        /* Apply the fade effect to the two musicSources */
        ApplyFade(ref musicFadeMuted, musicSourceMuted, false);
        ApplyFade(ref musicFadeUpgraded , musicSourceUpgraded, false);


        /* Check if a fade effect needs to be applied to any of the footstep sources */
        for(int i = 0; i < maxSimultaniousStepEffects; i++){
            if(upperStepSources[i].isPlaying) {

                /* Check if the playing source is past the stepDelay */
                if(upperStepSources[i].timeSamples >= stepFadeDelay[i]) {
                    stepFade[i] = -10;
                }


                float upperFade = stepFade[i];
                float lowerFade = stepFade[i];
                ApplyFade(ref upperFade, upperStepSources[i], false);
                ApplyFade(ref lowerFade, lowerStepSources[i], false);
                /* Only change the real fade value if both values are identical.
                 * This will prevent one clip from ending earlier and forcing the other to stop too. */
                if(upperFade == lowerFade) {
                    stepFade[i] = upperFade;
                }
            }
		}
	}

    void ApplyFade(ref float fade, AudioSource source, bool stopOnMute) {
        /*
         * A generalized function that will either fade in or fade out 
         * the given source using the given fade value. The stopOnMute
         * indicates if the source should stop playing if fully muted.
         */

        if(fade != 0){
			source.volume = source.volume + maxVolume*fadeRate*fade;
			
			/* Stop the fading if the sources reaches max volume or is fully muted */
			if(source.volume <= 0){
                if(stopOnMute) {
                    source.Stop();
                }
				source.volume = 0;
				//fade = 0;
			}
			else if(source.volume >= maxVolume){
				source.volume = maxVolume;
				//fade = 0;
			}
		}
    }

    void SetMusicFade(float fade) {
        /*
         * Set the fade value of both music sources. This will allow the game to swap
         * between the upgraded and normal versions of a song.
         * -1 = fade out the upgraded version, fade in the muted version
         * 1 = fade out the muted version, fade in the upgraded version
         */

        musicFadeMuted = -fade;
        musicFadeUpgraded = fade;
    }


    /* ----------- Event Functions ------------------------------------------------------------- */

    public void ResetAll(bool playSong) {
        /*
         * Reset the fade values for every audio source and stop any audio from playing.
         * The playSong parameter determines if a song should be played after the reset.
         */

        /* Set the fade values for all sources to 0 */
        for(int i = 0; i < maxSimultaniousStepEffects; i++) {
            stepFade[i] = 0f;
            stepFadeDelay[i] = 0;
        }
        fallingFade = 0;
        musicFadeMuted = 0;
        musicFadeUpgraded = 0;

        /* Reset the audioMixer volumes */
        ResetAudioMixerVolume();

        /* Stop and mute all audio sources */
        StopAllAudioSources();

        /* Start playing a new song from the start */
        if(playSong) {
            PlayMusic();
        }
    }

    public void ResetAudioMixerVolume() {
        /*
         * Reset the volume of the audio mixer and it's groups
         */
        audioMixer.SetFloat("masterVolume", masterMixerVolume);
        audioMixer.SetFloat("musicVolume", musicMixerVolume);
        audioMixer.SetFloat("footstepsVolume", footstepsMixerVolume);
    }

    public void StopAllAudioSources() {
        /*
         * Stop all audio sources and set their volume to 0
         */

        for(int i = 0; i < upperStepSources.Length; i++) {
            upperStepSources[i].Stop();
            upperStepSources[i].volume = 0;
            lowerStepSources[i].Stop();
            lowerStepSources[i].volume = 0;
        }
        for(int i = 0; i < landingSources.Length; i++) {
            landingSources[i].Stop();
            landingSources[i].volume = 0;
        }
        musicSourceMuted.Stop();
        musicSourceMuted.volume = 0;
        musicSourceUpgraded.Stop();
        musicSourceUpgraded.volume = 0;
        fallingSource.Stop();
        fallingSource.volume = 0;
}

    public void TemporaryMixerAdjustment(float masterVolumeRatio) {
        /*
         * Apply a volume change to the audio mixer. This change is temporary as a call to 
         * StopAllAudioSources() will reset these changes. The given float will set the volume
         * of the master channel/group by treating it as a range (0 is near-muted, 1 is normal)
         */

        /* Set the volume of the master audio group */
        audioMixer.SetFloat("masterVolume", -50 + (50+masterMixerVolume)*masterVolumeRatio);
    }

    public void EnteringOutside() {
        /*
         * Called when the player enters the outside state, this function will set the outside
         * boolean to true, making certain sounds play differently when they occur.
         */

        outside = true;
        UpgradeMusic();
    }

    public void UpgradeMusic() {
        /*
         * Running this function will upgrade the current the current and all upcomming songs.
         */

        /* Fade out the current muted song and fade in the upgraded version */
        musicFadeMuted = -0.1f;
        musicFadeUpgraded = 0.1f;
    }

    private int GetNewSongIndex() {
        /*
         * Get the index for a new song from the musicClipsMuted and musicClipsUpgraded arrays.
         * Update the previouslyPlayed array to track the songs that have been played so far.
         */
        int newSongIndex = -1;
        int songCount = 0;
        int[] potentialSongs;

        
        /* Get how many songs can be chosen */
        for(int i = 0; i < previouslyPlayed.Length; i++) {
            if(!previouslyPlayed[i]) {
                songCount++;
                //track the last non-played song for the case of only one song not yet played
                newSongIndex = i;
            }
        }

        /* If there is only one song that has not yet been played, chose it and reset previouslyPlayed  */
        if(songCount <= 1) {
            for(int i = 0; i < previouslyPlayed.Length; i++) {
                previouslyPlayed[i] = false;
            }
        }

        /* Choose a song that has not yet been played */
        else {

            /* Populate the potentialSongs array */
            potentialSongs = new int[songCount--];
            for(int i = 0; i < previouslyPlayed.Length; i++) {
                if(!previouslyPlayed[i]) {
                    potentialSongs[songCount--] = i;
                }
            }

            /* Choose one of the potential songs */
            newSongIndex = potentialSongs[Random.Range(0, potentialSongs.Length-1)];
        }

        /* Update the previouslyPlayed array to reflect the new song that will play */
        previouslyPlayed[newSongIndex] = true;

        return newSongIndex;
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
		
			/* Get a random integer between 0 and X-1 where X is the amount of unique footstep sounds.
             * Because Range is exclusive when handling ints, add +1 to the max range. */
			randomIndex = Random.Range(0, clips.Length-1);
		
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
            InitializeAudioObject(ref upperSource[i], ref upperContainerArray[i], "Footstep(high) " + i, stepTransformContainer.transform, audioMixerFootsteps);
            InitializeAudioObject(ref lowerSource[i], ref lowerContainerArray[i], "Footstep(low) " + i, stepTransformContainer.transform, audioMixerFootsteps);

            highPassFilter[i] = upperContainerArray[i].AddComponent<AudioHighPassFilter>();
            lowerPassFilter[i] = lowerContainerArray[i].AddComponent<AudioLowPassFilter>();

            highPassFilter[i].cutoffFrequency = 1000;
            lowerPassFilter[i].cutoffFrequency = 1000;
        }
    }


    void InitializeAudioArray(ref AudioSource[] sourceArray, ref GameObject[] containerArray, int sourceSize, 
            string name, Transform parent, AudioMixerGroup mixerGroup){
		/*
		 * Initialize the given arrays with audioSources and their containers
		 */
		
		sourceArray = new AudioSource[sourceSize];
        containerArray = new GameObject[sourceSize];
        for(int i = 0; i < sourceArray.Length; i++){
        	InitializeAudioObject(ref sourceArray[i], ref containerArray[i], name + " " + i, parent, mixerGroup);
        }
	}
	
	void InitializeAudioObject(ref AudioSource source, ref GameObject container, 
            string name, Transform parent, AudioMixerGroup mixerGroup) {
		/*
		 * Initialize the given container and add the given source
         * Create the given container, place it in the main audio container and add it's audioSource
		 */
		
		container = new GameObject();
        container.transform.parent = parent;
        container.name = name;
        source = container.AddComponent<AudioSource>();
        source.outputAudioMixerGroup = mixerGroup;
    }
}
