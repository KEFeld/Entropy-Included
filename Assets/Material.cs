using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Material
{
    public string name;
    public float heatCapacity;
    public float thermalConductivity;
    public float density;
    public Sprite sprite;

    public Material(string n, float c,  float k,  float rho, Sprite s)
    {
        name = n;
        heatCapacity = c;
        thermalConductivity = k;
        density = rho;
        sprite = s;
    }

    // Additional methods to handle data can be added here
}
