using UnityEngine;

public class PathMarker : MonoBehaviour
{
    public int x, y;

    private void Start()
    {
        Vector3 posOffset = new Vector3(0, -0.49f, 0);
        transform.position = UT_UnitMovements.GetPos(x, y) + posOffset;
    }
}
