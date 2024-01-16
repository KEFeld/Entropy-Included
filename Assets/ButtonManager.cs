using UnityEngine;
using UnityEngine.UI;

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
    public delegate void BuildButtonClickedHandler(Button clickedButton);
    public static event BuildButtonClickedHandler OnBuildClicked;
    public Material[] materials;



    private void Start()
    {
        foreach (Button btn in overlays)
        {
            btn.onClick.AddListener(() => OnOverlayClickedInternal(btn));
        }
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
        }
        foreach (Button btn in buildButtons)
        {
            btn.onClick.AddListener(() => OnBuildClickedInternal(btn));
        }
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


    

