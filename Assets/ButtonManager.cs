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
    public Button[] buttons; // Assign your buttons in the inspector
    public Button activeButton = null; // Keep track of the active button
    public delegate void ButtonClickedHandler(Button clickedButton);
    public static event ButtonClickedHandler OnButtonClicked;


    private void Start()
    {
        foreach (Button btn in buttons)
        {
            btn.onClick.AddListener(() => OnButtonClickedInternal(btn));
        }
    }

    private void OnButtonClickedInternal(Button clickedButton)
    {
        // If there is an active button, reset its color to white
        if (activeButton != null)
        {
            activeButton.GetComponent<Image>().color = Color.white;
        }

        if (clickedButton == activeButton)
        {
            activeButton = null;
        } else
        {
            // Set the clicked button to blue
            clickedButton.GetComponent<Image>().color = Color.blue;

            // Update the active button to be the one that was just clicked
            activeButton = clickedButton;
        }

        OnButtonClicked?.Invoke(clickedButton);
    }
}
