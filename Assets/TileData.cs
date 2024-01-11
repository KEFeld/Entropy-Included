using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Gasses
{
    oxygen,
    hydrogen,
    nitrogen,
    carbondioxide
}

[System.Serializable]
public class TileData
{
    public bool isGas;
    public float temperature;
    public float mass;
    public Material material;
    public GameObject tileObject; // Reference to the tile GameObject

    public TileData(float temp, Material mat, GameObject obj)
    {
        temperature = temp;
        tileObject = obj;
        material = mat;
    }

    public virtual void ChangeTemperature(float energyChange)
    {
        
    }
}

public class SolidTileData : TileData
{


    public SolidTileData(float m, float temp, Material mat, GameObject obj) : base(temp, mat, obj)
    {
        mass = m;
        isGas = false;
    }

    public override void ChangeTemperature(float energyChange)
    {
        this.temperature += energyChange / (this.mass * this.material.heatCapacity);
    }

}

public class GasTileData : TileData
{
    public float[] gasses;

    public GasTileData(float temp, Material mat, float[] g, GameObject obj) : base(temp, mat, obj)
    {
        gasses = g;
        SetMass();
        isGas = true;
    }

    public void SetMass()
    {
        this.mass = 0.032f * gasses[0] + 0.002f * gasses[1] + 0.028f * gasses[2] + 0.044f * gasses[3];
    }

    public float TotalGas()
    {
        float total = 0;
        for (int i = 0; i<gasses.Length; i++)
        {
            //Debug.Log("total" + total);
            total += gasses[i];
        }
        return total;
    }

    public void SetTemperature(float energy)
    {
        this.temperature = energy / TotalGas() * 20.79f;
    }

    public override void ChangeTemperature(float energyChange)
    {
        this.temperature += energyChange / TotalGas() * 20.79f;
    }
}

