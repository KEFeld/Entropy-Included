using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using System;
using UnityEngine.UI;

public interface TemperatureAffecting
{
    void UpdateTemperature(float[,] heatTransfer, TileData[,] gridData);
}

public class Buildings : MonoBehaviour
{
    public int x;
    public int y;
    public GridManager gridManager;
    public List<int> test = new List<int>();
    
    public virtual void Start()
    {
        x = Mathf.FloorToInt(transform.position.x);
        y = Mathf.FloorToInt(transform.position.y);   
    }

    void Update()
    {
        
    }

    public virtual string hoverText()
    {
        return "";
    }
}

public class Peltier : Buildings, TemperatureAffecting
{

    int horizontal = 0; //0 if in up-down orientation, 1 if left-right
    int flipped = 1; //1 if downside or left side hot, -1 if reversed 
    float intendedPower = 200;
    float power;
    float tHot;
    float tCold;
    float carnot;
    float flowIn;

    public void UpdateTemperature(float[,] heatTransfer, TileData[,] gridData)
    {
        flipped = -1;
        power = intendedPower;
        tHot = gridData[x + horizontal, y + (1 - horizontal)].temperature;
        tCold = gridData[x - horizontal, y - (1 - horizontal)].temperature;
        if (tCold > tHot)
        {
            (tCold, tHot) = (tHot, tCold);
            flipped = 1;
        }
        carnot = Carnot(tHot, tCold);

        flowIn = intendedPower / carnot;
        float maxTFlow = Mathf.Min(500, 20 * (tHot - tCold));
        if (flowIn > maxTFlow)
        {
            flowIn = maxTFlow;
            power = flowIn * carnot;
        }
        float flowOut = flowIn - power;
        heatTransfer[x + flipped * horizontal, y + flipped * (1 - horizontal)] += flowIn;
        heatTransfer[x - flipped * horizontal, y - flipped * (1 - horizontal)] -= flowOut;

    }

    private float Carnot(float tHot, float tCold)
    {
        return 1f - tCold / tHot;
    }

    public void setPower(float power)
    {
        intendedPower = power;
    }

    public override string hoverText()
    {
        return "Peltier element\nTemperature difference: " + (tHot - tCold) +
            "\nEfficiency: " + (carnot * 100).ToString("F2") + " %" +
            "\nProducing " + power + " W" +
            "\nAnd moving an additional  " + (flowIn-power).ToString("F2") + " W from hot to cold side";
    }

}

public class RTG : Buildings, TemperatureAffecting
{
    private float power;

    public override void Start()
    {
        base.Start();
        power = 1000;
    }

    void Update()
    {

    }

    public void UpdateTemperature(float[,] heatTransfer, TileData[,] gridData)
    {
        heatTransfer[x, y] += power;
    }

    public override string hoverText()
    {
        return "Radioisotope Thermal Generator\nContaining 1.7kg of Plutonium-238\nproducing 1000W of heat";
    }
}

public class TemperatureFixer : Buildings, TemperatureAffecting
{
    private float temperatureTarget;
    

    void Update()
    {

    }

    public void UpdateTemperature(float[,] heatTransfer, TileData[,] gridData)
    {
        gridData[x, y].temperature = temperatureTarget;
    }

    public void SetTarget(float temp)
    {
        temperatureTarget = temp;
    }

    public override string hoverText()
    {
        return "Magic seems to keep this tile at a near constant temperature";
    }
}
