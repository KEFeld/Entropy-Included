using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public enum Gasses
{
    oxygen,
    hydrogen,
    nitrogen,
    carbondioxide,
    water
}

[System.Serializable]
public abstract class TileData
{
    public bool isGas;
    public bool hasLiquid;
    public float temperature;
    public float mass;
    //public Material material;
    public GameObject tileObject;

    public abstract Sprite Sprite { get; }
    public abstract Color defaultColor { get; }
    public abstract float thermalConductivity { get; }
    public event Action<int, int, TileData> OnStateChangeRequest;

    //public abstract Material Material { set; }

    public TileData(float temp/*, Material mat*/, GameObject obj)
    {
        temperature = temp;
        tileObject = obj;
       // material = mat;
    }

    public virtual string hoverText()
    {
        return "";

    }

    protected void RequestStateChange(int x, int y, TileData newTile)
    {
        OnStateChangeRequest?.Invoke(x, y, newTile);
    }

    public virtual void ChangeTemperature(float energyChange)
    {

    }
}

public class SolidTileData : TileData
{
    public bool melting = false;
    public float fractionMolten = 0;
    public Solid material;

    public SolidTileData(float m, float temp, Solid mat, GameObject obj) : base(temp, obj)
    {
        material = mat;
        mass = m;
        isGas = false;
        hasLiquid = false;
    }

    public override Sprite Sprite => material?.sprite;
    public override Color defaultColor => Color.white;

    public override float thermalConductivity => material.thermalConductivity;

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
            if (fractionMolten > 1)
            {
                float excessEnergy = (fractionMolten - 1) * material.heatOfFusion * 1000;
                Melt(excessEnergy);
            }
        }

        void Melt(float excessEnergy)
        {
            int x = Mathf.FloorToInt(tileObject.transform.position.x);
            int y = Mathf.FloorToInt(tileObject.transform.position.y);
            GasTileData gasTile = new GasTileData(temperature, material, tileObject, l : material.meltsTo, lm: mass); //placeholder
            RequestStateChange(x, y, gasTile);                                                                            
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

public class GasTileData : TileData //really FluidTileData handles both liquids and gasses 
{
    public float[] gasses;
    public Material material;
    public Liquid liquid;
    public float liquidMass;
    public float fogMass;
    public float rainMass;
    public float totalWater;

    Sprite[] waterSprites;
    Sprite white;
    Sprite rain;

    public override Sprite Sprite => (liquidMass == 0) ? ((rainMass == 0) ? white : rain) : waterSprites[Mathf.FloorToInt(liquidMass/100+0.01f)];
    public override Color defaultColor => (liquidMass == 0) ? Color.Lerp(Color.grey, Color.white, fogMass) : Color.white;

    public override float thermalConductivity => TotalGas()/100 + liquidMass/10;

    public GasTileData(float temp, Material mat, GameObject obj, float[] g = null, Liquid l = null, float lm = 0f) : base(temp, obj)
    {
        gasses = (g ?? new float[5]);
        SetMass();

        material = mat;
        liquid = l;
        liquidMass = lm;
        fogMass = 0;
        rainMass = 0;
        totalWater = liquidMass;
        

        isGas = (liquidMass / (liquid?.density ?? 1) < 0.99); //isGas has changed job and should probably be hasGas, becomes true once gasses are tracked.
       
        waterSprites = new Sprite[10];

        white = Resources.Load<Sprite>("white");
        rain = Resources.Load<Sprite>("rain");
        // Load each sprite
        for (int i = 0; i < waterSprites.Length; i++)
        {
            // Construct the sprite name. Assuming names are "water10", "water20", ..., "water90"
            string spriteName = "water" + (i + 1) * 10;

            // Load the sprite from the Resources folder
            waterSprites[i] = Resources.Load<Sprite>(spriteName);

            // Check if the sprite was successfully loaded
            if (waterSprites[i] == null)
            {
                Debug.LogError("Sprite not found: " + spriteName);
            }
        }
    }

    public void SetMass()
    {
        this.mass = 0.032f * gasses[0] + 0.002f * gasses[1] + 0.028f * gasses[2] + 0.044f * gasses[3] + gasses[4] * 0.018f; //needs to be dynamically coded
    }

    public float TotalGas()
    {
        float total = 0;
        for (int i = 0; i < gasses.Length; i++)
        {
            //Debug.Log("total" + total);
            total += gasses[i];
        }
        return total;
    }

    public float VaporPressure()
    {
        // return Mathf.Exp(liquid.a - liquid.b / temperature);
        return Mathf.Exp(18.2f - 5065f / temperature);
    }

    public bool Evaporate()
    {
        bool updateNeeded = false;
        float discrepancy = (gasses[4] - VaporPressure()) / 10; //needs to check all gasses eventually
        if (discrepancy > 0 || totalWater > 0)
        {
            hasLiquid = true;

            float change = discrepancy * 0.018f;

            fogMass += change;
            if (fogMass < 0)
            {
                rainMass += fogMass;
                fogMass = 0;
                if (rainMass < 0)
                {
                    liquidMass += rainMass;
                    rainMass = 0;
                    updateNeeded = true;
                    if (liquidMass < 0)
                    {
                        change += liquidMass;
                        liquidMass = 0;
                        discrepancy = change / 0.018f;
                    }
                }
            }
            if (fogMass > 1)
            {
                rainMass += fogMass - 1;
                fogMass = 1;
                updateNeeded = true;
            }
       
            gasses[4] -= discrepancy;
            totalWater += change;
            ChangeTemperature(change * /*liquid.heatOfVaporization*/ 2260*10); //fudge for now, only water
            isGas = (liquidMass / (liquid?.density ?? 1) < 0.99);

        }
        return updateNeeded;
    }

    public void SetTemperature(float energy)
    {
        this.temperature = energy / TotalGas() * 20.79f;
    }

    public override void ChangeTemperature(float energyChange)
    {
        this.temperature += energyChange / (TotalGas() * 20.79f + (liquid?.heatCapacity ?? 0) * liquidMass);
    }

    public override string hoverText()
    {
        int x = Mathf.FloorToInt(tileObject.transform.position.x);
        int y = Mathf.FloorToInt(tileObject.transform.position.y);
        float totalGas = TotalGas();
        string liquidText = "";
        string gasText = "";
        string text = "Temperature: " + temperature.ToString("F4") + "K";
        if (totalWater > 0)
        {
            liquidText = "\n\nLiquid: water\nMass: " + liquidMass.ToString("F3") + " kg" +
                "\nFog: " + fogMass.ToString("F3") + " kg" +
                "\nRain: " + rainMass.ToString("F3") + " kg" + 
                "\nTotal Water: " + totalWater.ToString("F3") + " kg\n";
        }
        if (isGas)
        {
            gasText = ("\nGas:\nOxygen: " + (gasses[0] * 100 / totalGas).ToString("F1") +
            "%\nHydrogen: " + (gasses[1] * 100 / totalGas).ToString("F1") +
            "%\nNitrogen: " + (gasses[2] * 100 / totalGas).ToString("F1") +
            "%\nCarbondioxide: " + (gasses[3] * 100 / totalGas).ToString("F1") +
            "%\nWater vapor: " + (gasses[4] * 100 / totalGas).ToString("F1") +
            "%\nThermal Energy: " + (totalGas * temperature * 20.79f) +
            "\nMass: " + mass.ToString("F5") + " kg" +
            "\n( " + x + " , " + y + " )");
        }


        return text + liquidText + gasText;
    }
}
