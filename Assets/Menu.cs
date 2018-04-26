using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/*
 * The menu used by the player during the game. It expected each component to be 
 * already created and assigned in a canvas.
 */
public class Menu : MonoBehaviour {

    /* A link to the player's controller */
    public CustomPlayerController playerController;

    /* The canvas that holds all the UI elements */
    public Canvas canvas;

    /* The main "Start" button */
    public Button startButton;


    /* ----------- Set-up Functions ------------------------------------------------------------- */

    public void Initialize(CustomPlayerController controller) {
        /*
         * Sets up the main menu. Requires a link to the playerController to add functionallity to the buttons
         */

        playerController = controller;
        startButton.onClick.AddListener(StartButtonClick);
        //startButton.GetComponent<GUIText>() = "start";
    }


    /* ----------- Listener Functions ------------------------------------------------------------- */

    public void StartButtonClick() {
        /*
         * Runs when the user presses the start button
         */

        playerController.StartButtonPressed();
    }
}
