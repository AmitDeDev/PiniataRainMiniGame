public class GameModel
{
    public int Score { get; set; } = 0;
    public float Timer { get; set; } = 120; // Start from 2 minutes

    public int BombCount { get; set; } = 0;
    public int CriticalCount { get; set; } = 0;
    public int NextCriticalValue { get; set; } = 0;
}