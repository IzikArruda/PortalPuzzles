using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using System.Collections;

/*  
 * The potential states the menu can be in.
 */
public enum MenuStates {
    Empty,
    EmptyToMain,
    Main,
};

/*
 * The menu used by the player during the game. It expected each component to be 
 * already created and assigned in a canvas.
 */
public class Menu : MonoBehaviour {
    private MenuStates state;

    /* Button height to width ratios. Set manually and is unique for each font + text content. */
    private float startWidthRatio = 3;

    /* Global values used for sizes of UI elements */
    private float minHeight = 25;
    private float maxHeight = 200;
    private float buttonHeight;

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

    void Start() {
        /*  
         * Start the menu in the empty state
         */
        state = MenuStates.Main;
    }

    void Update() {
        /*
         * Check if the screen has been resized and run any per-frame update calls for any UI elements
         */

        /* Check if the screen has been resized */
        if(Screen.width != screenWidth || Screen.height != screenHeight) {
            Resize();
        }
        
        /* Start button */
        if(IsStartVisible()) {
            UStartButtonValues();
            switch(state) {
                case MenuStates.EmptyToMain:
                    UStartButtonEmptyToMain();
                    break;
                case MenuStates.Main:
                    UStartButtonMain();
                    break;
                default:
                    Debug.Log("ERROR: Menu item does not handle current state");
                    break;
            }
        }
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
    }

    public void Resize() {
        /*
         * Update the sizes of the ui to reflect the current screen size.
         */
        screenWidth = Screen.width;
        screenHeight = Screen.height;

        /* Update the buttonHeight value used by all buttons */
        buttonHeight = Mathf.Clamp(screenHeight*0.2f, minHeight, maxHeight);
    }
    

    /* ----------- Update Functions ------------------------------------------------------------- */

    #region Start Button Updates
    void UStartButtonValues() {
        /*
         * Update values used by the start button.
         */
         
        /* Update the value when the user has pressed the start button. Will be removed with a new state. */
        if(pressedStartButton == true) {
            pressedStartCurrentTime += Time.deltaTime;
            if(pressedStartCurrentTime > pressedStartMaxTime) { pressedStartCurrentTime = pressedStartMaxTime; }
        }

        /* Update the hover value when the mouse is/isin't over the button */
        if(isHover) { currentHoverTime += Time.deltaTime; }
        else { currentHoverTime -= Time.deltaTime; }
        if(currentHoverTime < 0) { currentHoverTime = 0; }
        else if(currentHoverTime > maxHoverTime) { currentHoverTime = maxHoverTime; }
    }

    void UStartButtonEmptyToMain() {
        /*
         * Update the start button while in the EmptyToMain state. 
         */

        /* For now, do the same as the normal Menu state */
        UStartButtonMain();
    }

    void UStartButtonMain() {
        /*
         * Update the start button while in the Main state.
         */
        Outline[] outlines = startButton.GetComponentInChildren<Text>().gameObject.GetComponents<Outline>();
        float hoverRatio = (Mathf.Sin(Mathf.PI*currentHoverTime/maxHoverTime - 0.5f*Mathf.PI)+1)/2f;
        float extraHoverWidth = hoverRatio*buttonHeight*0.5f;

        /* The position is effected by the current hover value */
        startButtonRect.sizeDelta = 1.5f*new Vector2(buttonHeight*startWidthRatio + extraHoverWidth, buttonHeight);
        startButtonRect.position = new Vector3(startButtonRect.sizeDelta.x/2f, canvasRect.position.y + buttonHeight/2f, 0);

        /* The color is controlled by the current hover value AND click value(will be removed later) */
        float clickFade = pressedStartCurrentTime/pressedStartMaxTime;
        startButton.GetComponentInChildren<Text>().color = new Color(1, 1, 1, 1 - clickFade);
        outlines[0].effectColor = new Color(0, 0, 0, 0.5f - 0.75f*clickFade);
        outlines[1].effectColor = new Color(0, 0, 0, 0.5f - 0.5f*clickFade);
        outlines[0].effectDistance = new Vector2(0.5f + 45f*clickFade, 0.5f + 45f*clickFade);
        outlines[1].effectDistance = new Vector2(0.5f + 30f*clickFade, 0.5f + 30f*clickFade);
        float hovCol = 1f - 0.25f*hoverRatio;
        startButton.GetComponentInChildren<Text>().color = new Color(hovCol, hovCol, hovCol, startButton.GetComponentInChildren<Text>().color.a);
    }
    #endregion

    
    /* ----------- Event/Listener Functions ------------------------------------------------------------- */

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


    /* ----------- Helper Functions ------------------------------------------------------------- */

    bool IsStartVisible() {
        /*
         * Return true if the current state shows the start button
         */
        bool visible = false;

        if(state == MenuStates.EmptyToMain || state == MenuStates.Main) {
            visible = true;
        }

        return visible;
    }
}
