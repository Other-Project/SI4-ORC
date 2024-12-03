namespace Cache.OpenRouteService;

public enum InstructionType
{
    Left = 0,
    Right = 1,
    SharpLeft = 2,
    SharpRight = 3,
    SlightLeft = 4,
    SlightRight = 5,
    Straight = 6,
    EnterRoundabout = 7,
    ExitRoundabout = 8,
    UTurn = 9,
    Goal = 10,
    Depart = 11,
    KeepLeft = 12,
    KeepRight = 13,

    ChangeVehicle = 14 // Special
}
