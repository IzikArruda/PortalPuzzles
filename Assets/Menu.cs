using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
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
    private bool animatingStartButton = false;
    private float startButtonRemainingTime;


    /* Previous resolutions of the window */
    public float screenWidth;
    public float screenHeight;


    /* ----------- Built-in Functions ------------------------------------------------------------- */

    void Update() {
        /*
         * Check if there was a change in the current window size
         */

        if(screenWidth != Screen.width || screenHeight != Screen.height) {

            /* Update the new sizes and update the menu's positions */
            screenWidth = Screen.width;
            screenHeight = Screen.height;
            UpdatePositions();
        }

        /* Update the start button's visuals */
        if(animatingStartButton == true) {
            startButtonRemainingTime -= Time.deltaTime;
            UpdateStartButtonVisuals();
        }
    }


    /* ----------- Set-up Functions ------------------------------------------------------------- */


    public void Initialize(CustomPlayerController controller) {
        /*
         * Sets up the main menu. Requires a link to the playerController to add functionallity to the buttons
         */

        /* Link the global variables of the script */
        playerController = controller;
        canvasRect = canvas.GetComponent<RectTransform>();

        /* Run the initialSetup functions for the buttons */
        SetupStartButton();
    }

    public void SetupText(Text text) {
        /*
         * Set the properties of the text object. This is to keep all text objects consistent
         */

        /* Set the font */
        text.font = usedFont;
        text.fontStyle = FontStyle.Normal;

        /* Set the outlines for the text. There will be two outlines used for each text. */
        if(text.gameObject.GetComponent<Outline>() == null) { text.gameObject.AddComponent<Outline>(); text.gameObject.AddComponent<Outline>(); }
        Outline[] outlines = text.gameObject.GetComponents<Outline>();
    }

    public void SetupStartButton() {
        /*
         * Set the values and variables needed for the start button
         */

        /* Get the components used on the button */
        startButton.onClick.AddListener(StartButtonClick);
        startButtonRect = startButton.GetComponent<RectTransform>();

        /* Add an event trigger for when the mosue hovers over */
        EventTrigger startButtonTrigger = startButton.gameObject.AddComponent<EventTrigger>();
        EventTrigger.Entry startButtonEntry = new EventTrigger.Entry();
        startButtonEntry.eventID = EventTriggerType.PointerEnter;
        startButtonEntry.callback.AddListener((data) => { StartButtonMouseEnter(); });
        startButtonTrigger.triggers.Add(startButtonEntry);

        /* Set the sizes and content of the button */
        SetupText(startButton.GetComponentInChildren<Text>());
        startButtonRect.sizeDelta = new Vector2(400, 125);
        startButton.GetComponentInChildren<Text>().text = "START";
        startButton.GetComponentInChildren<Text>().fontSize = 100;
        
        /* Update the visuals of the button */
        UpdateStartButtonVisuals();
    }


    /* ----------- Visual Functions ------------------------------------------------------------- */

    public void UpdateStartButtonVisuals() {
        /*
         * Update the visuals of the start button. This includes the outlines used. Adjust the visuals
         * depending on whether animatingStartButton is true or not and how much startButtonRemainingTime is left.
         */

        Outline[] outlines = startButton.GetComponentInChildren<Text>().gameObject.GetComponents<Outline>();

        /* Leave the button as normal */
        if(animatingStartButton == false) {
            startButton.GetComponentInChildren<Text>().color = new Color(1, 1, 1, 1);
            outlines[0].effectColor = new Color(0, 0, 0, 0.3f);
            outlines[0].effectDistance = new Vector2(1.5f, 1.5f);
            outlines[1].effectColor = new Color(0, 0, 0, 0.3f);
            outlines[1].effectDistance = new Vector2(1.5f, 1.5f);
        }

        /* Animate the button fading away */
        else {
            float ratio = startButtonRemainingTime/5f;
            if(ratio < 0) { ratio = 0; }
            Debug.Log(ratio);

            startButton.GetComponentInChildren<Text>().color = new Color(1, 1, 1, ratio*1);
            outlines[0].effectColor = new Color(0, 0, 0, ratio*0.3f);
            outlines[0].effectDistance = new Vector2(1.5f, 1.5f);
            outlines[1].effectColor = new Color(0, 0, 0, ratio*0.3f);
            outlines[1].effectDistance = new Vector2(1.5f, 1.5f);
        }
    }
    

    /* ----------- Screen Position Functions ------------------------------------------------------------- */

    public void UpdatePositions() {
        /*
         * Reposition the components of the menu relative to the screen size
         */

        startButtonRect.position = new Vector3(startButtonRect.sizeDelta.x/2f, canvasRect.position.y, 0);
    }


    /* ----------- Listener Functions ------------------------------------------------------------- */

    public void StartButtonClick() {
        /*
         * Runs when the user presses the start button
         */

        if(animatingStartButton == false) {
            animatingStartButton = true;
            startButtonRemainingTime = 5;
        }

        playerController.StartButtonPressed();
    }

    public void StartButtonMouseEnter() {
        /*
         * The mouse entered the startButton's clickable area
         */

        Debug.Log("Mouse enterd area");
    }
}
