using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "CombineSO", menuName = "Scriptable Objects/CombineSO")]
public class CombineSO : ScriptableObject
{
    public List<UnitType> combine_from_types;
    public List<Vector2> combine_from_offsets;
    public int combine_property_id;
    public GameObject combine_to_object_prefab;
    public Vector2 combine_to_offset;
    public bool is_white_unit;
}
