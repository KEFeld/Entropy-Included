using UnityEngine;
using UnityEngine.UI;

public class ButtonController : MonoBehaviour
{
    public Button[] buttons; // Assign your buttons in the inspector
    private Button activeButton = null; // Keep track of the active button

    private void Start()
    {
        foreach (Button btn in buttons)
        {
            btn.onClick.AddListener(() => OnButtonClicked(btn));
        }
    }

    private void OnButtonClicked(Button clickedButton)
    {
        // If there is an active button, reset its color to white
        if (activeButton != null)
        {
            activeButton.GetComponent<Image>().color = Color.white;
        }

        // Set the clicked button to blue
        clickedButton.GetComponent<Image>().color = Color.blue;

        // Update the active button to be the one that was just clicked
        activeButton = clickedButton;
    }
}
