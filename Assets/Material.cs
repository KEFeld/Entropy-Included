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
    public float meltingPoint;
    public float heatOfFusion;

    public Sprite sprite;


    public Material(string n, float c,  float k,  float rho, float melt, float L, Sprite s)
    {
        name = n;
        heatCapacity = c;
        thermalConductivity = k;
        density = rho;
        meltingPoint = melt;
        heatOfFusion = L;

        sprite = s;
    }
}


public struct Gas
{
    public string name;
    public float molarMass;
}

