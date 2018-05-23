using UnityEngine;
using System.Collections;
using UnityEngine.PostProcessing;

/*
 * A script that contains the required functions to apply post-processing effects
 * to the linked player's camera. This script should be attached to the object that
 * also contains the CustomPlayerController that uses this script.
 */
public class CustomPlayerCameraEffects : MonoBehaviour {

    /* The player camera that will have post-processings effects. Set by the CustomPlayerController on it's start function. */
    public Camera playerCamera;

    /* The custom player script that contains pertinent variables about the player's state */
    public CustomPlayerController playerControllerScript;

    /*  Post-Processing Effect Pointers */
    private VignetteModel cameraVignette;
    private ChromaticAberrationModel cameraChromaticAberration;

    /* Camera effect overriding values */
    private float playerResetVignetteCurrent = -1;
    private float playerResetVignetteMax = -1;


    /* ----------- Set-up Functions ------------------------------------------------------------- */

    public void SetupPostProcessingEffects(Camera camera, CustomPlayerController playerScript) {
        /*
         * Runs on startup, used to assign starting values to the post processing effects.
         * The given camera is expected to be the player's camera.
         */
        playerCamera = camera;
        playerControllerScript = playerScript;

        if(playerCamera.GetComponent<PostProcessingBehaviour>()) {
            cameraVignette = playerCamera.GetComponent<PostProcessingBehaviour>().profile.vignette;
            cameraChromaticAberration = playerCamera.GetComponent<PostProcessingBehaviour>().profile.chromaticAberration;
            ResetCameraEffects();
        }
        else {
            Debug.Log("WARNING: playerCamera is missing missing a PostProcessingBehaviour");
        }
    }

    public void ResetCameraEffects() {
        /*
         * Reset the camera's effects and stop any effect animations it can be undergoing
         */
         
        /* Stop the playerReset animation */
        StopPlayerReset();

        /* Disable the camera effects. This is because there are no effects when standing in a legal position */
        cameraVignette.enabled = false;
        cameraChromaticAberration.enabled = false;
    }
    
    public void StartEffectVignette() {
        /*
    	 * Enable the vignetting effect on the player camera
    	 */

        cameraVignette.enabled = true;
    }

    public void StartChromaticAberration() {
        /* 
    	 * Enable chromatic aberration, an effect that discolors the edges of the camera
    	 */

        cameraChromaticAberration.enabled =  true;
    }

    public void StopEffectVignette() {
        /*
    	 * Disable the vignetting effect on the player camera
    	 */

        cameraVignette.enabled = false;
    }

    public void StopChromaticAberration() {
        /* 
    	 * Disable the chromatic aberration effect on the camera
    	 */

        cameraChromaticAberration.enabled =  false;
    }


    /* ----------- Update Functions ------------------------------------------------------------- */

    public void UpdateCameraEffects() {
        /*
    	 * Add the effects to the camera every frame.
    	 * Runs every frame no matter the state.
    	 * Current playerState controls what effects are used.
    	 */

        /* The vignette is effected by the speed of a fastFall and duration of a landing.
         * If the player is currently being reset, have the reset animation take control of the vignette */
        if(cameraVignette.enabled) {
            UpdateEffectVignette(playerControllerScript.state, playerControllerScript.stateTime, playerControllerScript.GetYVelocityFastFallRatio());
        }

        /* Chromatic Aberration last during the landing animation the fades away */
        if(cameraChromaticAberration.enabled) {
            UpdateEffectChromaticAberration(playerControllerScript.state, playerControllerScript.GetYVelocityFastFallRatio());
        }
    }

    public void UpdateEffectVignette(PlayerStates playerState, float playerStateTime, float velocityRatio) {
        /*
    	 * The vignette effect adds a border around the camera's view.
         * When the player is resetting, have a sharp, closing vignette.
    	 * When in fastfall, the player speed directly effects the intensity.
    	 * The start frames of landing will have a large amount of intensity.
    	 * When outside the previously mentionned states, a duration value
    	 * will count down as the vignetting dissipates.
    	 */
        float intensity = 0;
        float minTime = 0.1f;

        /* Set the intensity relative to the current player reset time */
        if(playerResetVignetteCurrent > -1) {

            /* Have it animate in two states */
            float currentRatio = (playerResetVignetteMax - playerResetVignetteCurrent)/playerResetVignetteMax;

            float firstHalf = 0.6f;
            if(currentRatio < firstHalf) {
                /* First half is slow */
                intensity = Mathf.Log10(20 + 500*currentRatio)-1;
            }
            else {
                /* Second half is fast */
                intensity = (Mathf.Log10(20 + 500*currentRatio)-1) + Mathf.Pow(15*(currentRatio-firstHalf), 6);
            }
        }

        /* Set the intensity relative to the player speed */
        else if(playerState == PlayerStates.FastFalling) {
            intensity = 0.15f*velocityRatio;
        }

        /* Set the intensity depending on the stateTime */
        else if(playerState == PlayerStates.Landing && playerStateTime < minTime) {
            intensity = 0.15f + 0.15f*Mathf.Sin((Mathf.PI/2f)*(playerStateTime/minTime));
        }

        /* Reduce the vignette intensity. Disable the effect once it reaches 0 */
        else {

            /* If theres intensity to be reduce, start reducing it */
            intensity = cameraVignette.settings.intensity - Time.deltaTime*60/240f;

            /* Once the intensity reaches 0, disable the vignetting */
            if(intensity <= 0) {
                StopEffectVignette();
            }
        }

        /* Apply the intensity to the camera's vignetting effect */
        VignetteModel.Settings vignetteSettings = cameraVignette.settings;
        vignetteSettings.intensity = intensity;
        cameraVignette.settings = vignetteSettings;
    }

    public void UpdateEffectChromaticAberration(PlayerStates playerState, float velocityRatio) {
        /*
    	 * The intensity of this effect will remain at max when in the landing state.
    	 * Any other state will cause a reduction in it's intensity.
    	 */
        float intensity = 0f;

        /* Set the intensity relative to the player speed */
        if(playerState == PlayerStates.FastFalling) {
            intensity = 3*velocityRatio;
        }

        /* Keep the intensity at a set value for the duration of the landing state */
        else if(playerState == PlayerStates.Landing) {
            intensity = 4;
        }

        /* Slowly reduce the intensity, disabling the effect once it reaches 0 */
        else {
            intensity = cameraChromaticAberration.settings.intensity - Time.deltaTime*60/60f;
            if(intensity <= 0) {
                StopChromaticAberration();
            }
        }

        ChromaticAberrationModel.Settings chromaticAberrationSettings = cameraChromaticAberration.settings;
        chromaticAberrationSettings.intensity = intensity;
        cameraChromaticAberration.settings = chromaticAberrationSettings;
    }


    /* ----------- Event Functions ------------------------------------------------------------- */
    
    public void StartPlayerReset(float resetTime) {
        /*
         * The player wants to reset themselves and needs a vignette effect to mask the teleport.
         * The given float is how much time is required for the reset to complete.
         */

        /* Enable the vignette effect */
        StartEffectVignette();

        /* Set the timing used to measure the intensity of the vignette */
        playerResetVignetteMax = resetTime;
        playerResetVignetteCurrent = resetTime;

        /* Set the smootheness of the vignette effect for the reset animation */
        VignetteModel.Settings vignetteSettings = cameraVignette.settings;
        vignetteSettings.smoothness = 0;
        cameraVignette.settings = vignetteSettings;
    }

    public void UpdatePlayerReset(float currentResetTime) {
        /*
         * Update the vignette intesity with the new given time
         */

        playerResetVignetteCurrent = currentResetTime;
    }

    public void StopPlayerReset() {
        /*
         * Stop the player reset animation effects for this script
         */

        playerResetVignetteCurrent = -1;

        /* Set the smoothness of the vignette back to it's default for falling */
        VignetteModel.Settings vignetteSettings = cameraVignette.settings;
        vignetteSettings.smoothness = 0;
        vignetteSettings.intensity = 0;
        cameraVignette.settings = vignetteSettings;
    }
}
