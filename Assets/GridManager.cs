using UnityEngine;
using UnityEngine.EventSystems; // Required for checking UI elements


public class GridManager : MonoBehaviour
{
    public GameObject blockPrefab; // Prefab for the block
    public int width = 10; // Width of the grid
    public int height = 10; // Height of the grid

    private GameObject[,] grid; // 2D array to store block states

    private bool isDragging = false; // To track if dragging started outside of UI elements

    void Start()
    {
        CreateGrid();
    }

    void Update()
    {
        // Check for the initial mouse down event
        if (Input.GetMouseButtonDown(0) && !IsPointerOverUIObject())
        {
            isDragging = true; // Start dragging if the click was not on a UI element
        }

        // If dragging and the left mouse button is held down, change block color
        if (isDragging && Input.GetMouseButton(0))
        {
            ChangeBlockColorUnderMouse();
        }

        // Reset dragging state when mouse button is released
        if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
        }
    }

    void CreateGrid()
    {
        grid = new GameObject[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                GameObject newBlock = Instantiate(blockPrefab, new Vector2(x, y), Quaternion.identity);
                newBlock.transform.parent = this.transform; // Set the grid as the parent
                grid[x, y] = newBlock;
            }
        }
    }

    void ChangeBlockColorUnderMouse()
    {
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);

        if (hit.collider != null)
        {
            GameObject hoveredBlock = hit.collider.gameObject;
            hoveredBlock.GetComponent<SpriteRenderer>().color = Color.black; // Change color to black
        }
    }

    private bool IsPointerOverUIObject()
    {
        // Check if the pointer is over a UI element
        return EventSystem.current.IsPointerOverGameObject();
    }
}