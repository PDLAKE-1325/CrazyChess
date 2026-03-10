using UnityEngine;

public struct InputData
{
    public InputType input_type;
    public Unit target_unit;
    public Vector2 move_pos;

    public InputData(InputType input_type, Unit target_unit, Vector2 move_pos)
    {
        this.input_type = input_type;
        this.target_unit = target_unit;
        this.move_pos = move_pos;
    }

    // 이거 입력 대기 턴에서 액션 
}

