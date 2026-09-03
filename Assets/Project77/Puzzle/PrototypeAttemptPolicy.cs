namespace Project77.Puzzle
{
    public static class PrototypeAttemptPolicy
    {
        public static bool CanRestartAttempt(PuzzleRunStatus status, bool continuationOffered)
        {
            return !continuationOffered &&
                (status == PuzzleRunStatus.Active || status == PuzzleRunStatus.Failed);
        }

        public static bool CanModifyActiveAttempt(PuzzleRunStatus status, bool continuationOffered)
        {
            return !continuationOffered && status == PuzzleRunStatus.Active;
        }
    }
}
