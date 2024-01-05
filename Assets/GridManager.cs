using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using System;
using UnityEngine.UI;


public class GridManager : MonoBehaviour
{
    public GameObject tilePrefab; // Prefab for the tile
    public int width = 10; // Width of the grid
    public int height = 10; // Height of the grid
    public TextMeshProUGUI hoverText; // Public reference to the UI Text element
    public ButtonController buttonController;


    private TileData[,] gridData; // 2D array to store tile states

    private bool isDragging = false; // To track if dragging started outside of UI elements

    void Start()
    {
        CreateGrid();
        hoverText.text = ""; // Initialize the text as empty
        ButtonController.OnButtonClicked += HandleButtonClicked;
        CreateGrid();
    }

    void Update()
    {
        // Check for the initial mouse down event
        if (Input.GetMouseButtonDown(0) && !IsPointerOverUIObject())
        {
            isDragging = true; // Start dragging if the click was not on a UI element
        }

        // If dragging and the left mouse button is held down, change tile color
        if (isDragging && Input.GetMouseButton(0) && buttonController.activeButton == buttonController.buttons[2])
        {
            ChangeTileColorUnderMouse();
        }

        // Reset dragging state when mouse button is released
        if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
        }
        if (!isDragging)
        {
            UpdateHoverText();
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        ButtonController.OnButtonClicked -= HandleButtonClicked;
    }

    void CreateGrid()
    {
        gridData = new TileData[width, height];
        float[,] temperature = GenerateFractalNoise(width, height, 4, 100, 2f, 0.2f, 535245);
        float[,] material = GenerateFractalNoise(width, height, 4, 100, 2f, 0.5f, 356367);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                GameObject newTile = Instantiate(tilePrefab, new Vector2(x, y), Quaternion.identity);
                newTile.transform.parent = this.transform;

                // Initialize TileData with default values
                TileData tileData = new TileData(temperature[x, y]*60f-20f, material[x, y] > 0.4 ? "rock" : "air", newTile);

                newTile.GetComponent<SpriteRenderer>().color = material[x, y] > 0.4 ? Color.black : Color.white;

                gridData[x, y] = tileData;
            }
        }
    }

    void ChangeTileColorUnderMouse()
    {
        TileData tileData = locate();
        if (tileData != null)
        {
            GameObject hoveredTile = tileData.tileObject;
            tileData.material = "rock";
            hoveredTile.GetComponent<SpriteRenderer>().color = Color.black; // Change color to black
        }
    }

    TileData locate()
    {
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);
        TileData tileData = null;

        if (hit.collider != null)
        {
            GameObject hoveredTile = hit.collider.gameObject;

            // Calculate the grid position of the hovered tile
            int x = Mathf.FloorToInt(hoveredTile.transform.position.x);
            int y = Mathf.FloorToInt(hoveredTile.transform.position.y);

            // Check if the tile is within the grid bounds
            if (x >= 0 && x < width && y >= 0 && y < height)
            {
                tileData = gridData[x, y];
            }
        }
        return tileData;
    }

    private bool IsPointerOverUIObject()
    {
        // Check if the pointer is over a UI element
        return EventSystem.current.IsPointerOverGameObject();
    }

    void UpdateHoverText()
    {
        TileData tileData = locate();

        if (tileData != null)
        {
            hoverText.text = "Temperature: " + tileData.temperature;
        }
        else
        {
            hoverText.text = ""; // Clear text when not hovering over a tile
        }
    }

    private void HandleButtonClicked(Button clickedButton)
    {
        // Check which button was clicked and update colors accordingly
        ColorTilesBasedOnButtonState();
    }

    void ColorTilesBasedOnButtonState()
    {
        if (buttonController.activeButton == buttonController.buttons[0])
        {
            // Color tiles based on temperature
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    float value = (gridData[x, y].temperature + 20) / 80;
                    float hue = Mathf.Clamp(240 - (240 * value), 0, 240) / 360.0f; // Hue value normalized to [0, 1]

                    // We keep saturation and brightness at maximum (1) for vivid colors
                    Color tileColor = Color.HSVToRGB(hue, 1, 1);
                    gridData[x, y].tileObject.GetComponent<SpriteRenderer>().color = tileColor;
                }
            }
        }
        else
        {
            // Reset tile colors to default (black for rock, white for air)
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Color tileColor = gridData[x, y].material == "rock" ? Color.black : Color.white;
                    gridData[x, y].tileObject.GetComponent<SpriteRenderer>().color = tileColor;
                }
            }
        }
    }

    private static Color ColorFromHSV(double hue, double saturation, double value)
    {
        int hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
        double f = hue / 60 - Math.Floor(hue / 60);

        value = Mathf.Clamp01((float)value); // Ensure value is in 0-1 range
        float v = (float)value;
        float p = v * (1 - (float)saturation);
        float q = v * (1 - (float)(f * saturation));
        float t = v * (1 - (float)((1 - f) * saturation));


        if (hi == 0)
            return new Color(v, t, p);
        else if (hi == 1)
            return new Color(q, v, p);
        else if (hi == 2)
            return new Color(p, v, t);
        else if (hi == 3)
            return new Color(p, q, v);
        else if (hi == 4)
            return new Color(t, p, v);
        else
            return new Color(v, p, q);
    }




    public float[,] GenerateFractalNoise(int width, int height, int octaves, float scale, float lacunarity, float persistence, int seed)
    {
        float[,] noiseMap = new float[width, height];

        System.Random prng = new System.Random(seed);
        Vector2[] octaveOffsets = new Vector2[octaves];
        for (int i = 0; i < octaves; i++)
        {
            float offsetX = prng.Next(-100000, 100000);
            float offsetY = prng.Next(-100000, 100000);
            octaveOffsets[i] = new Vector2(offsetX, offsetY);
        }

        if (scale <= 0)
        {
            scale = 0.0001f;
        }

        float maxNoiseHeight = float.MinValue;
        float minNoiseHeight = float.MaxValue;

        float halfWidth = width / 2f;
        float halfHeight = height / 2f;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float amplitude = 1;
                float frequency = 1;
                float noiseHeight = 0;

                for (int i = 0; i < octaves; i++)
                {
                    float sampleX = (x - halfWidth + octaveOffsets[i].x) / scale * frequency;
                    float sampleY = (y - halfHeight + octaveOffsets[i].y) / scale * frequency;

                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
                    noiseHeight += perlinValue * amplitude;

                    amplitude *= persistence;
                    frequency *= lacunarity;
                }

                if (noiseHeight > maxNoiseHeight)
                {
                    maxNoiseHeight = noiseHeight;
                }
                else if (noiseHeight < minNoiseHeight)
                {
                    minNoiseHeight = noiseHeight;
                }

                noiseMap[x, y] = noiseHeight;
            }
        }

        // Normalize the noise map
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                noiseMap[x, y] = Mathf.InverseLerp(minNoiseHeight, maxNoiseHeight, noiseMap[x, y]);
            }
        }

        return noiseMap;
    }
}
