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
}


public class Liquid : Material
{
    public float heatOfVaporization;

    public Liquid(string n, float c, float k, float rho, float L, Sprite s) : base(n, c, k, rho, s)
    {
        heatOfVaporization = L;
    }
}

public class Solid : Material
{
    public float meltingPoint;
    public float heatOfFusion;

    public Solid(string n, float c, float k, float rho, float melt, float L, Sprite s) : base(n, c, k, rho, s)
    {
        meltingPoint = melt;
        heatOfFusion = L;
    }
}


public struct Gas
{
    public string name;
    public float molarMass;
}

