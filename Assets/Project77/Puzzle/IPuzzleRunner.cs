namespace Project77.Puzzle
{
    public enum PuzzleRunStatus
    {
        NotLoaded = 0,
        Ready = 1,
        Active = 2,
        Succeeded = 3,
        Failed = 4
    }

    public interface IPuzzleAction
    {
    }

    public readonly struct PuzzleActionResult
    {
        public PuzzleActionResult(bool accepted, string reason)
        {
            Accepted = accepted;
            Reason = reason;
        }

        public bool Accepted { get; }
        public string Reason { get; }

        public static PuzzleActionResult Accept()
        {
            return new PuzzleActionResult(true, string.Empty);
        }

        public static PuzzleActionResult Reject(string reason)
        {
            return new PuzzleActionResult(false, reason ?? string.Empty);
        }
    }

    public interface IPuzzleRunner
    {
        string Variant { get; }
        PrototypeLevelDefinition Level { get; }
        PuzzleRunStatus Status { get; }

        void Load(PrototypeLevelDefinition level);
        void Start();
        PuzzleActionResult Apply(IPuzzleAction action);
        void Restart();
    }
}
