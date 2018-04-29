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
    private bool isHover = false;
    private float currentHoverTime = 0;
    private float maxHoverTime = 0.8f;
    private bool pressedStartButton = false;
    private float pressedStartCurrentTime = 0;
    private float pressedStartMaxTime = 1f;


    /* Previous resolutions of the window */
    public float screenWidth;
    public float screenHeight;


    /* ----------- Built-in Functions ------------------------------------------------------------- */

    void Update() {
        /*
         * Run checks that will be done on each frame, such as window resizing and button hovering
         */

        /* Reposition the buttons every frame */
        Reposition();

        /* Run the main update function for each button */
        UpdateStartButton();
    }


    /* ----------- Set-up Functions ------------------------------------------------------------- */

    public void InitializeMenu(CustomPlayerController controller) {
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

        /* Set size relative values for the text */
        text.alignment = TextAnchor.MiddleRight;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 1;
        text.resizeTextMaxSize = 300;

    }

    public void SetupButtonEvents(Button button, UnityAction mouseEnter, UnityAction mouseExit) {
        /*
         * Attach the hover events to the given button
         */
        EventTrigger startButtonTrigger = startButton.gameObject.AddComponent<EventTrigger>();
        EventTrigger.Entry startButtonEnter = new EventTrigger.Entry();
        startButtonEnter.eventID = EventTriggerType.PointerEnter;
        startButtonEnter.callback.AddListener((data) => { mouseEnter(); });
        startButtonTrigger.triggers.Add(startButtonEnter);
        EventTrigger.Entry startButtonExit = new EventTrigger.Entry();
        startButtonExit.eventID = EventTriggerType.PointerExit;
        startButtonExit.callback.AddListener((data) => { mouseExit(); });
        startButtonTrigger.triggers.Add(startButtonExit);
    }

    public void SetupStartButton() {
        /*
         * Set the values and variables needed for the start button
         */

        /* Get the components used on the button */
        startButton.onClick.AddListener(StartButtonClick);
        startButtonRect = startButton.GetComponent<RectTransform>();

        /* Set the sizes and content of the button */
        SetupText(startButton.GetComponentInChildren<Text>());
        startButton.GetComponentInChildren<Text>().text = "START";
        startButton.GetComponentInChildren<Text>().fontSize = 100;

        /* Add an event trigger for when the mosue hovers over the button */
        SetupButtonEvents(startButton, StartButtonMouseEnter, StartButtonMouseExit);
        
        /* Run the first update call for the button */
        UpdateStartButton();
    }
    

    /* ----------- Update Functions ------------------------------------------------------------- */

    void UpdateStartButton() {
        /*
         * Update the start button. Includes updating values which will control visual elements of the button.
         */

        /* Increase or decrease specific per-frame updated values */
        if(pressedStartButton == true) {
            pressedStartCurrentTime += Time.deltaTime;
            if(pressedStartCurrentTime > pressedStartMaxTime) { pressedStartCurrentTime = pressedStartMaxTime; }
        }

        if(isHover) { currentHoverTime += Time.deltaTime; }
        else { currentHoverTime -= Time.deltaTime; }
        if(currentHoverTime < 0) { currentHoverTime = 0; }
        else if(currentHoverTime > maxHoverTime) { currentHoverTime = maxHoverTime; }

        /* Update the visuals of the button */
        UpdateStartButtonVisuals();
    }
    
    public void UpdateStartButtonVisuals() {
        /*
         * Update the visuals of the start button. This includes the outlines used. Adjust the visuals
         * depending on whether animatingStartButton is true or not and how much startButtonRemainingTime is left.
         */
        Outline[] outlines = startButton.GetComponentInChildren<Text>().gameObject.GetComponents<Outline>();
        float clickFade = pressedStartCurrentTime/pressedStartMaxTime;
        float hoverFade = currentHoverTime/maxHoverTime;

        /* Clicking the button controls the outline distance and overall opacity */
        startButton.GetComponentInChildren<Text>().color = new Color(1, 1, 1, 1 - clickFade);
        outlines[0].effectColor = new Color(0, 0, 0, 0.5f - 0.75f*clickFade);
        outlines[1].effectColor = new Color(0, 0, 0, 0.5f - 0.5f*clickFade);
        outlines[0].effectDistance = new Vector2(0.5f + 45f*clickFade, 0.5f + 45f*clickFade);
        outlines[1].effectDistance = new Vector2(0.5f + 30f*clickFade, 0.5f + 30f*clickFade);

        /* Hovering over the button controls it's color */
        float hovCol = 1f - 0.25f*hoverFade;
        startButton.GetComponentInChildren<Text>().color = new Color(hovCol, hovCol, hovCol, startButton.GetComponentInChildren<Text>().color.a);
    }
    

    /* ----------- Screen Position Functions ------------------------------------------------------------- */

    public void Reposition() {
        /*
         * Reposition the components of the menu relative to the screen size. The size of the text
         * is relative to the screen size. The height of the text also has a min and max limit.
         * Depending on the current hover value of the buttons, add horizontal size to the button.
         * 
         * Note: The width of the buttons are completely relative to the text and font used. 
         * Each button's width will be set through trial and error of the min and max sizes.
         */
        float minHeight = 25;
        float maxHeight = 200;
        float startWidthRatio = 3;
        float startHoverRatio = (Mathf.Sin(Mathf.PI*currentHoverTime/maxHoverTime - 0.5f*Mathf.PI)+1)/2f;

        /* Set the height of the buttons and adjust the extra width values */
        float buttonHeight = Screen.height*0.2f;
        if(buttonHeight < minHeight) { buttonHeight = minHeight; }
        else if(buttonHeight > maxHeight) { buttonHeight = maxHeight; }
        float startExtraWidth = startHoverRatio*buttonHeight*0.5f;
        
        /* Place the start button near the center left of the screen. Be 50% larger than other buttons. */
        startButtonRect.sizeDelta = 1.5f*new Vector2(buttonHeight*startWidthRatio + startExtraWidth, buttonHeight);
        startButtonRect.position = new Vector3(startButtonRect.sizeDelta.x/2f, canvasRect.position.y + buttonHeight/2f, 0);
    }


    /* ----------- Listener Functions ------------------------------------------------------------- */

    public void StartButtonClick() {
        /*
         * Runs when the user presses the start button
         */

        if(pressedStartButton == false) {
            pressedStartButton = true;
            pressedStartCurrentTime = 0;
        }

        playerController.StartButtonPressed();
    }

    public void StartButtonMouseEnter() {
        /*
         * The mouse entered the startButton's clickable area
         */

        isHover = true;
    }

    public void StartButtonMouseExit() {
        /*
         * The mouse entered the startButton's clickable area
         */

        Debug.Log("unhovered");
        isHover = false;
    }
}
