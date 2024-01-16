using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Gasses
{
    oxygen,
    hydrogen,
    nitrogen,
    carbondioxide,
    water
}

[System.Serializable]
public class TileData<T> where T : Material
{
    public bool isGas;
    public float temperature;
    public float mass;
    public T material;
    public GameObject tileObject; // Reference to the tile GameObject

    public TileData(float temp, Material mat, GameObject obj)
    {
        temperature = temp;
        tileObject = obj;
        material = mat;
    }

    public virtual string hoverText()
    {
        return "";

    }

    public virtual void ChangeTemperature(float energyChange)
    {

    }
}

public class SolidTileData : TileData<Solid>
{
    public bool melting = false;
    public float fractionMolten = 0;
    public new Solid material;

    public SolidTileData(float m, float temp, Solid mat, GameObject obj) : base(temp, obj)
    {
        material = mat;
        mass = m;
        isGas = false;
    }

    public override void ChangeTemperature(float energyChange)
    {

        if (!melting)
        {
            this.temperature += energyChange / (this.mass * this.material.heatCapacity);
            if (temperature > material.meltingPoint)
            {
                melting = true;
                fractionMolten = material.heatCapacity * (temperature - material.meltingPoint) / (material.heatOfFusion * 1000);
                temperature = material.meltingPoint;
            }
        }
        else
        {
            fractionMolten += energyChange / (material.heatOfFusion * 1000 * mass);
            if (fractionMolten < 0)
            {
                melting = false;
                temperature = fractionMolten * material.heatOfFusion * 1000 / material.heatCapacity + material.meltingPoint;
                fractionMolten = 0;
            }
        }
    }

    public override string hoverText()
    {
        Buildings building = tileObject.GetComponent<Buildings>();
        return material.name +
            "\nTemperature: " + temperature.ToString("F1") + "K" + // \u00B0
            "\nMass: " + mass.ToString("F2") + " kg" +
            "\nThermal Conductivity:\n" + material.thermalConductivity.ToString("F1") + " W/mK" +
            "\nSpecifc heat capacity:\n" + material.heatCapacity.ToString("F1") + " J/kgK" +
            "\nHeat Capacity:\n" + (material.heatCapacity * mass).ToString("F1") + " J/K" +
            "\nHeat of fusion:\n" + material.heatOfFusion.ToString("F0") + " kJ/kg" +
            (melting ? ("\nFraction molten:\n" + (fractionMolten*100).ToString("F1") + " %") : "") +
            "\n\n( " + tileObject.transform.position.x + " , " + tileObject.transform.position.y + " )\n\n" + building?.hoverText();

    }

}

public class GasTileData : TileData<Material>
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

    public override string hoverText()
    {
        int x = Mathf.FloorToInt(tileObject.transform.position.x);
        int y = Mathf.FloorToInt(tileObject.transform.position.y);
        float totalGas = TotalGas();
        return ("Gas\nOxygen: " + (gasses[0] * 100 / totalGas).ToString("F1") +
            "%\nHydrogen: " + (gasses[1] * 100 / totalGas).ToString("F1") +
            "%\nNitrogen: " + (gasses[2] * 100 / totalGas).ToString("F1") +
            "%\nCarbondioxide: " + (gasses[3] * 100 / totalGas).ToString("F1") +
            "%\nThermal Energy: " + (totalGas * temperature * 20.79f) +
            "\nTemperature: " + temperature.ToString("F4") + "K" +
            "\nMass: " + mass.ToString("F5") + " kg" +
            "\n( " + x + " , " + y + " )");
    }
}
