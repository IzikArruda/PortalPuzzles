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

    /* The font used for the text of the game */
    public Font usedFont;

    /* The canvas that holds all the UI elements */
    public Canvas canvas;
    public RectTransform canvasRect;

    /* The main "Start" button */
    public Button startButton;
    public RectTransform startButtonRect;


    /* ----------- Set-up Functions ------------------------------------------------------------- */

    public void Initialize(CustomPlayerController controller) {
        /*
         * Sets up the main menu. Requires a link to the playerController to add functionallity to the buttons
         */
        playerController = controller;

        /* Setup the canvas */
        canvasRect = canvas.GetComponent<RectTransform>();

        /* Setup the start button */
        startButton.onClick.AddListener(StartButtonClick);
        startButtonRect = startButton.GetComponent<RectTransform>();
        startButtonRect.sizeDelta = new Vector2(300, 100);
        startButtonRect.position = new Vector3(1.1f*startButtonRect.sizeDelta.x/2f, canvasRect.position.y, 0);
        startButton.GetComponentInChildren<Text>().text = "START";
        startButton.GetComponentInChildren<Text>().fontSize = 60;
        SetupText(startButton.GetComponentInChildren<Text>());
    }

    public void SetupText(Text text) {
        /*
         * Set the properties of the text object. This is to keep all text objects consistent
         */

        /* Set the font */
        text.font = usedFont;
        text.fontStyle = FontStyle.Normal;

        /* Set the outline */
        if(text.GetComponent<Outline>() == null) { text.gameObject.AddComponent<Outline>(); }
        text.GetComponent<Outline>().effectColor = new Color(0, 0, 0, 170);
        text.GetComponent<Outline>().effectDistance = new Vector2(2, 2);
    }

    /* ----------- Listener Functions ------------------------------------------------------------- */

    public void StartButtonClick() {
        /*
         * Runs when the user presses the start button
         */

        playerController.StartButtonPressed();
    }
}
