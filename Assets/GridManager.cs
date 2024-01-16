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
    public float thermalConductivitySpeedup = 100f; // max safe ~100
    public float airflowSpeed = 0.00001f;
    public ButtonController buttonController;
    public bool isPaused = false;
    public float gravity = 0.3f;
    public float friction;

    private Material rock;
    private Material air;
    private Material plutonium;
    private Material aluminium;
    private Material peltier;
    private Material rockwool;
    private Material ice;
    private Gas[] gasses;
    private TemperatureAffecting[] temperatureAffectingBuildings = new TemperatureAffecting[3];




    private float[,] velocitiesY;
    private float[,] velocitiesX;
    private float[,] pressure;

    private Sprite arrow;



    public TileData[,] gridData; // 2D array to store tile states

    private bool isDragging = false; // To track if dragging started outside of UI elements

    void Start()
    {
        pressure = new float[width, height];
        velocitiesX = new float[width, height];
        velocitiesY = new float[width, height];
        arrow = Resources.Load<Sprite>("arrow");
        gasses = new Gas[] {
            new Gas { name = "Oxygen", molarMass = 0.032f },
            new Gas { name = "Hydrogen", molarMass = 0.002f },
            new Gas { name = "Nitrogen", molarMass = 0.028f },
            new Gas { name = "Carbondioxide", molarMass = 0.044f },
            new Gas { name = "water", molarMass = 0.018f}
        };


 
        rock = new Material("Rock", 0.8f, 3f, 2600f, 1983f, 1787, Resources.Load<Sprite>("rock"));
        air = new Material("Air", 2.0f, 0.0025f, 1.4f, 0, 0, Resources.Load<Sprite>("white"));
        plutonium = new Material("RTG", 2.8f, 10f, 5000f, 10000, 0, Resources.Load<Sprite>("RTG100"));
        aluminium = new Material("Aluminium", 0.9f, 205f, 2700f, 933f, 376f, Resources.Load<Sprite>("metal"));
        peltier = new Material("Peltier", 2.0f, 0, 2000, 10000, 0, Resources.Load<Sprite>("peltier"));
        rockwool = new Material("Rockwool", 0.8f, 0.04f, 80f, 1983f, 1787, Resources.Load<Sprite>("rockwool"));
        ice = new Material("Ice", 2.1f, 2.2f, 1000, 273, 334, Resources.Load<Sprite>("ice"));

        buttonController.materials = new Material[] { aluminium, rock, rockwool, ice };

        buttonController.CreateBuildButtons(buttonController.materials);

        hoverText.text = ""; // Initialize the text as empty
        ButtonController.OnOverlayClicked += HandleOverlayClicked;
        ButtonController.OnBuildClicked += HandleBuildClicked;

        CreateGrid();
        ColorTilesBasedOnOverlayState();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space)) isPaused = !isPaused;
        if (Input.GetKeyDown(KeyCode.Escape)) ColorTilesBasedOnOverlayState();

        // Check for the initial mouse down event
        if (Input.GetMouseButtonDown(0) && !IsPointerOverUIObject())
        {
            isDragging = true; // Start dragging if the click was not on a UI element
        }

        // If dragging and the left mouse button is held down, change tile color
        if (isDragging && Input.GetMouseButton(0) && buttonController.activeBuild is not null)
        {
            ChangeTileMaterialUnderMouse();
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
        if (buttonController.activeOverlay is not null)
        {
            ColorTilesBasedOnOverlayState();
        }
    }

    void FixedUpdate()
    {
        if (!isPaused)
        {
            float deltaTime = Time.fixedDeltaTime;

            // Temporary array to store heat transfer values
            float[,] heatTransfer = new float[width, height];
            float[,] accelerationsY = new float[width, height];
            float[,] accelerationsX = new float[width, height];
            float[,] newVelocitiesY = new float[width, height];
            float[,] newVelocitiesX = new float[width, height];
            // float[,] massflowY = new float[width, height];
            // float[,] massflowX = new float[width, height];
            float[,] tflowY = new float[width, height];
            float[,] tflowX = new float[width, height];
            float[,] massChange = new float[width, height];
            float[,] thermalEnergyChange = new float[width, height];
            float[,,] gasChange = new float[width, height, gasses.Length];
            float frictionFactor = Mathf.Pow(friction, deltaTime * airflowSpeed);



            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    TileData tile = gridData[x, y];

                    // Iterate through neighboring tiles

                    if (x < width - 1)
                    {
                        TileData neighborX = gridData[x + 1, y];
                        if (tile.isGas) 
                        {
                            if (!neighborX.isGas) 
                            {
                                GasTileData gasTile = tile as GasTileData;
                                float tempDifference = tile.temperature - neighborX.temperature;
                                float heatTransferPerSecond = Mathf.Min(gasTile.TotalGas()/1000, neighborX.material.thermalConductivity) * tempDifference;
                                heatTransfer[x, y] -= heatTransferPerSecond;
                                heatTransfer[x + 1, y] += heatTransferPerSecond;
                            }   
                        } 
                        else
                        {
                            if(neighborX.isGas)
                            {
                                GasTileData gasTile = neighborX as GasTileData;
                                float tempDifference = tile.temperature - neighborX.temperature;
                                float heatTransferPerSecond = Mathf.Min(gasTile.TotalGas()/1000, tile.material.thermalConductivity) * tempDifference;
                                heatTransfer[x, y] -= heatTransferPerSecond;
                                heatTransfer[x + 1, y] += heatTransferPerSecond;
                            } 
                            else //if both are solid
                            {
                                float minThermalConductivity = Mathf.Min(tile.material.thermalConductivity, neighborX.material.thermalConductivity);

                                float tempDifference = tile.temperature - neighborX.temperature;
                                float heatTransferPerSecond = minThermalConductivity * tempDifference;

                                // Accumulate heat transfer
                                heatTransfer[x, y] -= heatTransferPerSecond;
                                heatTransfer[x + 1, y] += heatTransferPerSecond;
                            }
                        }
                            
                    }
                    if (y < height - 1)
                    {
                        TileData neighborY = gridData[x, y + 1];
                        if (tile.isGas)
                        {
                            if (!neighborY.isGas)
                            {
                                GasTileData gasTile = tile as GasTileData;
                                float tempDifference = tile.temperature - neighborY.temperature;
                                float heatTransferPerSecond = Mathf.Min(gasTile.TotalGas()/1000, neighborY.material.thermalConductivity) * tempDifference;
                                heatTransfer[x, y] -= heatTransferPerSecond;
                                heatTransfer[x, y  + 1] += heatTransferPerSecond;
                            }
                        } 
                        else
                        {
                            if (neighborY.isGas)
                            {
                                GasTileData gasTile = neighborY as GasTileData;
                                float tempDifference = tile.temperature - neighborY.temperature;
                                float heatTransferPerSecond = Mathf.Min(gasTile.TotalGas()/1000, tile.material.thermalConductivity) * tempDifference;
                                heatTransfer[x, y] -= heatTransferPerSecond;
                                heatTransfer[x, y + 1] += heatTransferPerSecond;
                            }
                            else
                            {
                                float minThermalConductivity = Mathf.Min(tile.material.thermalConductivity, neighborY.material.thermalConductivity);

                                float tempDifference = tile.temperature - neighborY.temperature;
                                float heatTransferPerSecond = minThermalConductivity * tempDifference;

                                // Accumulate heat transfer
                                heatTransfer[x, y] -= heatTransferPerSecond;
                                heatTransfer[x, y + 1] += heatTransferPerSecond;
                            }
                        }   
                    }
                }
            }

            foreach (var building in temperatureAffectingBuildings)
            {
                building.UpdateTemperature(heatTransfer, gridData);
            }
            // Apply the heat transfer calculations to update temperatures
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    TileData tile = gridData[x, y];
                    // Update the temperature
                    float energyChange = heatTransfer[x, y] * deltaTime * thermalConductivitySpeedup;
                    tile.ChangeTemperature(energyChange);

                    if (tile.isGas)
                    {
                        GasTileData gasTile = (GasTileData)tile;
                        pressure[x, y] = tile.temperature * gasTile.TotalGas() * 0.00831f; //pressures in kPa
                        //Debug.Log("pressure" + pressure[x, y]);
                    }
                }
            }



            //calculate accelerations
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    TileData tile = gridData[x, y];

                    if (x < width - 1)
                    {
                        TileData neighborX = gridData[x + 1, y];

                        if (tile.isGas && neighborX.isGas)
                        {
                            accelerationsX[x, y] = 2 * (pressure[x, y] - pressure[x + 1, y]) / (tile.mass + neighborX.mass); // acc in km/s^2
                            velocitiesX[x, y] += accelerationsX[x, y] * deltaTime * airflowSpeed;  //as v is taken in m/s this makes for a factor 1000 slowdown on top of the airflowSpeed variable
                            //massflowX[x, y] = velocitiesX[x, y] * (tile.mass + neighborX.mass) / 2;
                        }
                    }
                    if (y < height - 1)
                    {
                        TileData neighborY = gridData[x, y + 1];

                        if (tile.isGas && neighborY.isGas)
                        {
                            accelerationsY[x, y] = 2 * ((pressure[x, y] - pressure[x, y + 1]) / (tile.mass + neighborY.mass)) - gravity;
                            velocitiesY[x, y] += accelerationsY[x, y] * deltaTime * airflowSpeed;
                            //massflowY[x, y] = velocitiesY[x, y] * (tile.mass + neighborY.mass) / 2;
                        }
                    }
                }
            }
            //advectionX
            for (int x = 0; x < width - 1; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (gridData[x, y].isGas && gridData[x + 1, y].isGas)
                    {
                        float velY = (velocitiesY[x, y] + ((y == 0) ? 0 : velocitiesY[x, y - 1]) + velocitiesY[x + 1, y] + ((y == 0) ? 0 : velocitiesY[x + 1, y - 1])) / 4 * deltaTime * airflowSpeed;
                        int i = velocitiesX[x, y] > 0 ? -1 : 1;
                        int j = velY > 0 ? -1 : 1;
                        velY = Mathf.Abs(velY);
                        float velX = Mathf.Abs(velocitiesX[x, y]) * deltaTime * airflowSpeed;
                        if (velX > .5 || velY > .5)
                        {
                            Debug.Log("Wind speed dangerous at (" + x + "," + y + ")");
                            isPaused = true;
                        }
                        velX = Mathf.Clamp(velX, 0, 1);
                        velY = Mathf.Clamp(velY, 0, 1);
                        newVelocitiesX[x, y] = velocitiesX[x, y] * (1 - velX) * (1 - velY) + ((x + i < 0) ? 0 : velocitiesX[x + i, y]) * velX * (1 - velY) + ((y + j < 0 || y + j >= height) ? 0 : velocitiesX[x, y + j]) * (1 - velX) * velY + ((x + i < 0 || y + j < 0 || y + j >= height) ? 0 : velocitiesX[x + 1, y + j]) * velX * velY;
                    }
                }
            }
            //advectionY
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height - 1; y++)
                {
                    if (gridData[x, y].isGas && gridData[x, y + 1].isGas)
                    {
                        float velX = (velocitiesX[x, y] + ((x == 0) ? 0 : velocitiesX[x - 1, y]) + velocitiesX[x, y + 1] + ((x == 0) ? 0 : velocitiesX[x - 1, y + 1])) / 4 * deltaTime * airflowSpeed;
                        int i = velocitiesY[x, y] > 0 ? -1 : 1;
                        int j = velX > 0 ? -1 : 1;
                        velX = Mathf.Abs(velX);
                        float velY = Mathf.Abs(velocitiesY[x, y]) * deltaTime * airflowSpeed;
                        if (velX > .5 || velY > .5)
                        {
                            Debug.Log("Wind speed dangerous at (" + x + "," + y + ")");
                            Debug.Log(velX + " , " + velY);
                            isPaused = true;
                        }
                        velX = Mathf.Clamp(velX, 0, 1);
                        velY = Mathf.Clamp(velY, 0, 1);
                        // try
                        // {
                        newVelocitiesY[x, y] = velocitiesY[x, y] * (1 - velX) * (1 - velY) + ((x + i < 0 || x + i >= width) ? 0 : velocitiesY[x + i, y]) * velX * (1 - velY) + ((y + j < 0) ? 0 : velocitiesY[x, y + j]) * (1 - velX) * velY + ((x + i < 0 || y + j < 0 || x + i >= width) ? 0 : velocitiesY[x + i, y + j]) * velX * velY;
                       /* }
                        catch
                        {
                            Debug.Log(x + " , " + i + " , " + y + " , " + j + " , " + velX + " , " + velY);

                        }*/
                    }

                }
            }
            
            //move mass and temperature
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    velocitiesX[x, y] = newVelocitiesX[x, y]*frictionFactor;
                    velocitiesY[x, y] = newVelocitiesY[x, y]*frictionFactor;
                    float time = deltaTime * airflowSpeed;

                    if (x < width - 1)
                    {
                        
                        if (gridData[x, y].isGas && gridData[x + 1, y].isGas)
                        {
                            GasTileData tile = (GasTileData)gridData[x, y];
                            GasTileData neighbor = (GasTileData)gridData[x + 1, y];
                            float totalFlow = 0;
                            bool direction = (Mathf.Sign(newVelocitiesX[x, y]) == 1);
                            for (int i = 0; i < gasses.Length; i++)
                            {
                                float flow = newVelocitiesX[x, y] * (direction ? tile.gasses[i] : neighbor.gasses[i]) * time;
                                totalFlow += flow;
                                gasChange[x, y, i] -= flow;
                                gasChange[x + 1, y, i] += flow;
                            }
                            float energyFlow = totalFlow * (direction ? tile.temperature : neighbor.temperature);
                            thermalEnergyChange[x, y] -= energyFlow;
                            thermalEnergyChange[x + 1, y] += energyFlow;
                        }
                    }
                    if (y < height - 1)
                    {
                        if (gridData[x, y].isGas && gridData[x, y + 1].isGas)
                        {
                            GasTileData tile = (GasTileData)gridData[x, y];
                            GasTileData neighbor = (GasTileData)gridData[x, y + 1];
                            float totalFlow = 0;
                            bool direction = (Mathf.Sign(newVelocitiesY[x, y]) == 1);
                            for (int i = 0; i < gasses.Length; i++)
                            {
                                float flow = newVelocitiesY[x, y] * (direction ? tile.gasses[i] : neighbor.gasses[i]) * time;
                                totalFlow += flow;
                                gasChange[x, y, i] -= flow;
                                gasChange[x, y + 1, i] += flow;
                            }
                            float energyFlow = totalFlow * (direction ? tile.temperature : neighbor.temperature);
                            thermalEnergyChange[x, y] -= energyFlow;
                            thermalEnergyChange[x, y + 1] += energyFlow; //energy flow in arbitrary units excluding factor of molar heat capacity (for now all the same across gasses)
                        }
                    }

                }
            } 
            //update mass and temperature
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (gridData[x, y].isGas)
                    {
                        GasTileData gasTile = gridData[x, y] as GasTileData;
                        float totalGas = gasTile.TotalGas();
                        float thermalEnergyPrior = (gasTile.temperature) * totalGas; // same arbitrary units

                        for (int i = 0; i < gasses.Length; i++)
                        {
                            if (gasTile.gasses[i] < -gasChange[x, y, i])
                            {
                                Debug.Log("Trying to remove gas that is not there at ( " + x + " ' " + y + ")");
                                isPaused = true;
                                gasTile.gasses[i] = 0;
                            }
                            else
                            {
                                gasTile.gasses[i] += gasChange[x, y, i];
                                totalGas += gasChange[x, y, i];
                            }
                        }
                        gasTile.temperature = (thermalEnergyPrior + thermalEnergyChange[x, y]) / totalGas; //set new temp based on updated thermal energy and new amount of gass
                        gasTile.SetMass();
                    }
                }
            } 
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        ButtonController.OnOverlayClicked -= HandleOverlayClicked;
        ButtonController.OnBuildClicked -= HandleBuildClicked;
    }

    void CreateGrid()
    {
        gridData = new TileData[width, height];
        float[,] temperature = GenerateFractalNoise(width, height, 4, 50, 2f, 0.2f, 3812);
        float[,] material = GenerateFractalNoise(width, height, 4, 50, 2f, 0.5f, 3450);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                GameObject newTile = Instantiate(tilePrefab, new Vector2(x, y), Quaternion.identity);
                newTile.transform.parent = this.transform;
                TileData tileData;
                // Initialize TileData with default values
                if (material[x, y] > 0.5f)
                {
                    tileData = new SolidTileData(2600, temperature[x, y] * 60f + 250f, rock, newTile);
                }
                else
                {
                    float[] amounts = new float[4]{ Mathf.Pow(1.003f, (50 - y)) * 12, 0, Mathf.Pow(1.003f, (50 - y)) * 38, .15f };
                    tileData = new GasTileData(temperature[x, y] * 60f + 250f, air, amounts, newTile);
                }
                newTile.GetComponent<SpriteRenderer>().sprite = tileData.material.sprite;
                gridData[x, y] = tileData;
            }
        }
        { //scope limiter
            int y = 96;
            for (int x = 20; x < 30; x++)
            {
                gridData[x, y] = new SolidTileData(aluminium.density, 300, aluminium, gridData[x, y].tileObject);
                gridData[x, y].tileObject.GetComponent<SpriteRenderer>().sprite = aluminium.sprite;
            }
            y = 28;
            for (int x = 45; x < 67; x++)
            {
                gridData[x, y] = new SolidTileData(aluminium.density, 400, aluminium, gridData[x, y].tileObject);
                gridData[x, y].tileObject.GetComponent<SpriteRenderer>().sprite = aluminium.sprite;
            }
            y = 80;
            for (int x = 30; x < 50; x++)
            {
                gridData[x, y] = new SolidTileData(aluminium.density, 300, aluminium, gridData[x, y].tileObject);
                gridData[x, y].tileObject.GetComponent<SpriteRenderer>().sprite = aluminium.sprite;
            }
            y = 30;
            for (int x = 66; x < 90; x++)
            {
                gridData[x, y] = new SolidTileData(aluminium.density, 300, aluminium, gridData[x, y].tileObject);
                gridData[x, y].tileObject.GetComponent<SpriteRenderer>().sprite = aluminium.sprite;
            }
            y = 29;
            for (int x = 45; x < 90; x++)
            {
                gridData[x, y] = new SolidTileData(rockwool.density, 300, rockwool, gridData[x, y].tileObject);
                gridData[x, y].tileObject.GetComponent<SpriteRenderer>().sprite = rockwool.sprite;
            }
        }
        {
            int x = 94;
            for (int y = 0; y < 9; y++)
            {
                gridData[x, y] = new SolidTileData(aluminium.density, 400, aluminium, gridData[x, y].tileObject);
                gridData[x, y].tileObject.GetComponent<SpriteRenderer>().sprite = aluminium.sprite;
            }
            x = 30;
            for (int y = 96; y > 70; y--)
            {
                gridData[x, y] = new SolidTileData(aluminium.density, 200, aluminium, gridData[x, y].tileObject);
                gridData[x, y].tileObject.GetComponent<SpriteRenderer>().sprite = aluminium.sprite;
            }
        }
        gridData[93, 7] = new SolidTileData(5000, 400, plutonium, gridData[93, 7].tileObject);

        gridData[66, 29] = new SolidTileData(5000, 300, peltier, gridData[66, 29].tileObject);
        gridData[93, 7].tileObject.AddComponent<RTG>();
        temperatureAffectingBuildings[0] = gridData[93, 7].tileObject.GetComponent<RTG>();

        gridData[26, 96].tileObject.AddComponent<TemperatureFixer>();
        gridData[26, 96].tileObject.GetComponent<TemperatureFixer>().SetTarget(150);
        temperatureAffectingBuildings[1] = gridData[26, 96].tileObject.GetComponent<TemperatureFixer>();

        gridData[66, 29].tileObject.AddComponent<Peltier>();
        gridData[66, 29].tileObject.GetComponent<Peltier>().setPower(400);
        temperatureAffectingBuildings[2] = gridData[66, 29].tileObject.GetComponent<Peltier>();


        gridData[93, 7].tileObject.GetComponent<SpriteRenderer>().sprite = plutonium.sprite;
        gridData[66, 29].tileObject.GetComponent<SpriteRenderer>().sprite = peltier.sprite;

    }

    void ChangeTileMaterialUnderMouse()
    {
        TileData tileData = Locate();
        if (tileData != null && buttonController.activeBuild != null)
        {
            
            int i = (int)buttonController.GetActiveBuildButtonIndex();
            Material material = buttonController.materials[i]; 
            GameObject hoveredTile = tileData.tileObject;
            if (tileData.isGas)
            {
                gridData[Mathf.FloorToInt(hoveredTile.transform.position.x), Mathf.FloorToInt(hoveredTile.transform.position.y)] = new SolidTileData(material.density, 300f, material, hoveredTile);
            }
            else
            {
                tileData.material = material;
                tileData.mass = material.density;
                hoveredTile.GetComponent<SpriteRenderer>().sprite = material.sprite;
                // change material and set sprite
            }
        }
    }

    TileData Locate()
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

        TileData tileData = Locate();

        if (tileData != null)
        {
            hoverText.text = tileData.hoverText();
            if (tileData.isGas)
            {

                int x = Mathf.FloorToInt(tileData.tileObject.transform.position.x);
                int y = Mathf.FloorToInt(tileData.tileObject.transform.position.y);
                float totalGas = (tileData as GasTileData).TotalGas();
                hoverText.text += 
                    "\n\nAirflows:\nTop: " + velocitiesY[x, y] + 
                    "\nBottom: " + (y == 0 ? 0 : velocitiesY[x, y - 1]) + 
                    "\nLeft: " + (x == 0 ? 0 : velocitiesX[x - 1, y]) + 
                    "\nRigth: " + velocitiesX[x, y];

            }
        }
        else
        {
            hoverText.text = ""; // Clear text when not hovering over a tile
        }
    }

    private void HandleOverlayClicked(Button clickedButton)
    {
        // Check which button was clicked and update colors accordingly

        if (buttonController.activeOverlay == buttonController.overlays[2])
        {
            SetSpritesToArrows();
        } else
        {
            RemoveArrows();
        }

        ColorTilesBasedOnOverlayState();
    }

    private void HandleBuildClicked(Button clickedButton)
    {

    }

    void SetSpritesToArrows()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (gridData[x, y].isGas)
                {
                    gridData[x, y].tileObject.GetComponent<SpriteRenderer>().sprite = arrow;
                }
            }
        }

    }

    void RemoveArrows()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                TileData tile = gridData[x, y];
                tile.tileObject.GetComponent<SpriteRenderer>().sprite = tile.material.sprite;
                tile.tileObject.transform.rotation = Quaternion.Euler(0, 0, 0);
            }
        }
    }

    void ColorTilesBasedOnOverlayState()
    {
        if (buttonController.activeOverlay == buttonController.overlays[0])
        {
            // Color tiles based on temperature
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    float value = (gridData[x, y].temperature - 250) / 80;
                    float hue = Mathf.Clamp(240 - (240 * value), 0, 240) / 360.0f; // Hue value normalized to [0, 1]

                    // We keep saturation and brightness at maximum (1) for vivid colors
                    Color tileColor = Color.HSVToRGB(hue, 1, 1);
                    gridData[x, y].tileObject.GetComponent<SpriteRenderer>().color = tileColor;
                }
            }
        }
        else if (buttonController.activeOverlay == buttonController.overlays[1])
        {
            // Color tiles based on pressure
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    float value = (pressure[x, y] - 90) / 40;
                    float hue = Mathf.Clamp(240 - (240 * value), 0, 240) / 360.0f; // Hue value normalized to [0, 1]

                    // We keep saturation and brightness at maximum (1) for vivid colors
                    Color tileColor = Color.HSVToRGB(hue, 1, 1);
                    gridData[x, y].tileObject.GetComponent<SpriteRenderer>().color = tileColor;
                }
            }
        }
        else if (buttonController.activeOverlay == buttonController.overlays[2])
        {
            // Color tiles and set arrow sprite based on wind
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (gridData[x, y].isGas)
                    {
                        Vector2 wind = new Vector2(velocitiesX[x, y] + ((x == 0) ? 0 : velocitiesX[x - 1, y]), velocitiesY[x, y] + ((y == 0) ? 0 : velocitiesY[x, y - 1]));

                        float hue = Mathf.Clamp(240 - (wind.magnitude * 500), 0, 240) / 360.0f;

                        // We keep saturation and brightness at maximum (1) for vivid colors
                        Color tileColor = Color.HSVToRGB(hue, 1, 1);
                        gridData[x, y].tileObject.GetComponent<SpriteRenderer>().color = tileColor;
                        float angle = Mathf.Atan2(wind.y, wind.x) * Mathf.Rad2Deg;
                        gridData[x, y].tileObject.transform.rotation = Quaternion.Euler(0, 0, angle);
                    }

                }
            }
        }
        else
        {
            // Reset tile colors to default
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    //Color tileColor = gridData[x, y].material.name == "rock" ? Color.black : (gridData[x, y].material.name == "plutonium" ? Color.red : Color.white);
                    if (gridData[x, y].isGas)
                    {
                        gridData[x, y].tileObject.GetComponent<SpriteRenderer>().color = Color.grey;
                    }
                    else
                    {
                        gridData[x, y].tileObject.GetComponent<SpriteRenderer>().color = Color.white;
                    }

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
