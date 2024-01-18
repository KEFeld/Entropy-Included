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
    public float a;
    public float b; //constants for vapor pressure defaults to water 

    public Liquid(string n, float c, float k, float rho, float L, Sprite s, float a = 18.2f, float b=5065f) : base(n, c, k, rho, s)
    {
        heatOfVaporization = L;
        this.a = a;
        this.b = b;
    }
}

public class Solid : Material
{
    public float meltingPoint;
    public float heatOfFusion;
    public Liquid meltsTo;

    public Solid(string n, float c, float k, float rho, float melt, float L, Sprite s, Liquid to = null) : base(n, c, k, rho, s)
    {
        meltingPoint = melt;
        heatOfFusion = L;
        meltsTo = to;
    }
}


public struct Gas
{
    public string name;
    public float molarMass;
}

