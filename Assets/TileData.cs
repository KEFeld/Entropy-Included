using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TileData
{
    public float temperature;
    public Material material;
    public float mass;
    public GameObject tileObject; // Reference to the tile GameObject

    public TileData(float temp, Material mat, float m, GameObject obj)
    {
        temperature = temp;
        material = mat;
        mass = m;
        tileObject = obj;
    }

    // Additional methods to handle data can be added here
}
