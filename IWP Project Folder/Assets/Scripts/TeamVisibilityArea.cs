using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TeamVisibilityArea : MonoBehaviour
{
    public int TeamID = 0;

    private static int assignID = -1;
    private static List<TeamVisibilityArea> areaList = new List<TeamVisibilityArea>();
    public int AreaID = 0;

    void Start()
    {
        assignID++;
        AreaID = assignID;
        areaList.Add(this);
    }

    public static TeamVisibilityArea FindArea(int id)
    {
        areaList.RemoveAll(s => s == null);

        foreach (TeamVisibilityArea area in areaList)
        {
            if (area.AreaID == id)
                return area;
        }
        return null;
    }
}
