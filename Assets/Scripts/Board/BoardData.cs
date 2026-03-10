using UnityEngine;
using System.Collections.Generic;

public class BoardData : MonoBehaviour
{
    public List<List<Tile>> board = new();

    void Init()
    {
        for (int x = 0; x < 8; x++)
        {
            List<Tile> row = new List<Tile>();
            for (int y = 0; y < 8; y++)
            {
                row.Add(new Tile());
            }
            board.Add(row);
        }
    }

    void Awake()
    {
        Init();
    }
    void Start()
    {
        GameStreamManager.Instance.SetBoard(this);
    }
}
