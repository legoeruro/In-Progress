public static class AlphaScoreSession
{
    public static int CurrentScore { get; private set; }

    public static void SetScore(int score)
    {
        CurrentScore = score;
    }

    public static void Reset()
    {
        CurrentScore = 0;
    }
}
