using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "ItemSO", menuName = "Scriptable Objects/ItemSO")]
public class ItemSO : ScriptableObject
{
    public string itemName;
    [TextArea] public string description;
    public Sprite icon;

    public enum ItemType { MoveBoost, Jump }
    public ItemType item_type;

    // Which unit base type this item can fit (e.g., only Knight)
    public UnitType fit_unit_type;

    // For MoveBoost: additional landing offsets relative to unit (e.g., (2,1) )
    public List<Vector2> moveBoost_tiles;
}
