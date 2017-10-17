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
            cameraVignette.enabled = false;

            cameraChromaticAberration = playerCamera.GetComponent<PostProcessingBehaviour>().profile.chromaticAberration;
            cameraChromaticAberration.enabled = false;
        }
        else {
            Debug.Log("WARNING: playerCamera is missing missing a PostProcessingBehaviour");
        }
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

        /* The vignette is effected by the speed of a fastFall and duration of a landing */
        if(cameraVignette.enabled) {
            UpdateEffectVignette(playerControllerScript.state, playerControllerScript.stateTime, playerControllerScript.GetYVelocityFastFallRatio());
        }

        /* Chromatic Aberration last during the landing animation the fades away */
        if(cameraChromaticAberration.enabled) {
            UpdateEffectChromaticAberration(playerControllerScript.state, playerControllerScript.GetYVelocityFastFallRatio());
        }
    }

    public void UpdateEffectVignette(int playerState, float playerStateTime, float velocityRatio) {
        /*
    	 * The vignette effect adds a border around the camera's view.
    	 * When in fastfall, the player speed directly effects the intensity.
    	 * The start frames of landing will have a large amount of intensity.
    	 * When outside the previously mentionned states, a duration value
    	 * will count down as the vignetting dissipates.
    	 */
        float intensity = 0;
        float minTime = 0.1f;

        /* Set the intensity relative to the player speed */
        if(playerState == (int) PlayerStates.FastFalling) {
            intensity = 0.15f*velocityRatio;
        }

        /* Set the intensity depending on the stateTime */
        else if(playerState == (int) PlayerStates.Landing && playerStateTime < minTime) {
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

    public void UpdateEffectChromaticAberration(int playerState, float velocityRatio) {
        /*
    	 * The intensity of this effect will remain at max when in the landing state.
    	 * Any other state will cause a reduction in it's intensity.
    	 */
        float intensity = 0f;

        /* Set the intensity relative to the player speed */
        if(playerState == (int) PlayerStates.FastFalling) {
            intensity = 3*velocityRatio;
        }

        /* Keep the intensity at a set value for the duration of the landing state */
        else if(playerState == (int) PlayerStates.Landing) {
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
}
