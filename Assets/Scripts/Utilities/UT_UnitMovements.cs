using System.Collections.Generic;
using UnityEngine;
public class UT_UnitMovements
{
    public Dictionary<UnitType, List<Vector2>> unit_movement = new();
    const float posOffset = -7.875f;
    const float posMul = 2.25f;
    public static Vector3 GetPos(int x, int y)
    {
        return new Vector3(posOffset + x * posMul, 0, posOffset + y * posMul);
    }
}