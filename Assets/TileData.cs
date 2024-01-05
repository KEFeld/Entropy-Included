using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TileData
{
    public float temperature;
    public string material;
    public GameObject tileObject; // Reference to the tile GameObject

    public TileData(float temp, string mat, GameObject obj)
    {
        temperature = temp;
        material = mat;
        tileObject = obj;
    }

    // Additional methods to handle data can be added here
}
