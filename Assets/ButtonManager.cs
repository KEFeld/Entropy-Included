using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public enum ButtonState
{
    Button1,
    Button2,
    Button3,
    Button4
    // Add more states if you have more buttons
}

public class ButtonController : MonoBehaviour
{
    public GameObject buttonPrefab;
    public Canvas canvas;

    public Button[] overlays; // Assign your buttons in the inspector
    public Button activeOverlay = null; // Keep track of the active button

    public delegate void ButtonClickedHandler(Button clickedButton);
    public static event ButtonClickedHandler OnOverlayClicked;

    public Button[] buildButtons; // Assign your buttons in the inspector
    public Button activeBuild = null; // Keep track of the active button
    public GameObject tooltipPrefab;
    public delegate void BuildButtonClickedHandler(Button clickedButton);
    public static event BuildButtonClickedHandler OnBuildClicked;
    public Material[] materials;



    private void Start()
    {
        foreach (Button btn in overlays)
        {
            btn.onClick.AddListener(() => OnOverlayClickedInternal(btn));
            
        }
        AddTooltipToButton(overlays[0], "Temperature overlay");
        AddTooltipToButton(overlays[1], "Pressure overlay");
        AddTooltipToButton(overlays[2], "Wind overlay");
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (activeOverlay != null)
            {
                activeOverlay.GetComponent<Image>().color = Color.white;
                activeOverlay = null;
            } 
            else if (activeBuild != null)
            {
                activeBuild.GetComponent<Image>().color = Color.white;
                activeBuild = null;
            }
        }
    }
    private void OnOverlayClickedInternal(Button clickedButton)
    {
        // If there is an active button, reset its color to white
        if (activeOverlay != null)
        {
            activeOverlay.GetComponent<Image>().color = Color.white;
        }

        if (clickedButton == activeOverlay)
        {
            activeOverlay = null;
        } else
        {
            // Set the clicked button to blue
            clickedButton.GetComponent<Image>().color = Color.blue;

            // Update the active button to be the one that was just clicked
            activeOverlay = clickedButton;
        }

        OnOverlayClicked?.Invoke(clickedButton);
    }

    private void OnBuildClickedInternal(Button clickedButton)
    {
        // If there is an active button, reset its color to white
        if (activeBuild != null)
        {
            activeBuild.GetComponent<Image>().color = Color.white;
        }

        if (clickedButton == activeBuild)
        {
            activeBuild = null;
        }
        else
        {
            // Set the clicked button to blue
            clickedButton.GetComponent<Image>().color = Color.blue;

            // Update the active button to be the one that was just clicked
            activeBuild = clickedButton;
        }

        OnBuildClicked?.Invoke(clickedButton);
    }

    public void CreateBuildButtons(Material[] materials)
    {
     
        float buttonSize = 30; // Example size, you can adjust this
        float spacing = 10; // Spacing between buttons
        float startX = spacing;
        float yOffset = buttonSize + spacing; // Adjusting Y offset to move buttons up

        buildButtons = new Button[materials.Length];

        for (int i = 0; i < materials.Length; i++)
        {
            GameObject buttonObj = Instantiate(buttonPrefab, canvas.transform);
            buttonObj.transform.localScale = Vector3.one;

   


            // Set button position and size
            RectTransform rectTransform = buttonObj.GetComponent<RectTransform>();
            rectTransform.anchoredPosition = new Vector2(startX, yOffset);
            rectTransform.sizeDelta = new Vector2(buttonSize, buttonSize);

            // Set the image of the button
            Image buttonImage = buttonObj.GetComponent<Image>();
            buttonImage.sprite = materials[i].sprite;

            buildButtons[i] = buttonObj.GetComponent<Button>();

            startX += buttonSize + spacing;

            // Tooltip functionality
            AddTooltipToButton(buildButtons[i], "Place " + materials[i].name + " tiles.");
        }
        foreach (Button btn in buildButtons)
        {
            btn.onClick.AddListener(() => OnBuildClickedInternal(btn));
        }
    }

    private void AddTooltipToButton(Button button, string tooltipText)
    {
        // Assign event listeners for mouse hover
        EventTrigger trigger = button.gameObject.AddComponent<EventTrigger>();

        EventTrigger.Entry entryEnter = new EventTrigger.Entry();
        entryEnter.eventID = EventTriggerType.PointerEnter;
        entryEnter.callback.AddListener((data) => { OnHoverEnter((PointerEventData)data, tooltipText); });
        trigger.triggers.Add(entryEnter);

        EventTrigger.Entry entryExit = new EventTrigger.Entry();
        entryExit.eventID = EventTriggerType.PointerExit;
        entryExit.callback.AddListener((data) => { OnHoverExit((PointerEventData)data); });
        trigger.triggers.Add(entryExit);
    }

    private void OnHoverEnter(PointerEventData eventData, string tooltipText)
    {
        // Show tooltip
        tooltipPrefab.SetActive(true);
        tooltipPrefab.GetComponentInChildren<TextMeshProUGUI>().text = tooltipText;
        Vector2 textSize = new Vector2(tooltipPrefab.GetComponentInChildren<TextMeshProUGUI>().preferredWidth, tooltipPrefab.GetComponentInChildren<TextMeshProUGUI>().preferredHeight);
        tooltipPrefab.GetComponent<RectTransform>().sizeDelta = textSize + new Vector2(10,10);

        // Adjust position of the tooltip based on mouse position
        tooltipPrefab.transform.position = eventData.position + ((eventData.position.y > 300) ? new Vector2(-50, -50) : new Vector2(0, 25));
    }

    private void OnHoverExit(PointerEventData eventData)
    {
        // Hide tooltip
        tooltipPrefab.SetActive(false);
    }

    public int? GetActiveBuildButtonIndex()
    {
        if (buildButtons == null)
        {
            return null;
        }

        for (int i = 0; i < buildButtons.Length; i++)
        {
            if (buildButtons[i] == activeBuild)
            {
                return i;
            }
        }

        return null;
    }
}


    

