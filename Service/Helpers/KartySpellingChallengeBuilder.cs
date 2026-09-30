namespace Service.Helpers;

public static class KartySpellingChallengeBuilder
{
    public static KartySpellingChallenge Build(string nounText)
    {
        var noun = nounText.Trim();
        var showCorrect = Random.Shared.Next(2) == 0;
        return new KartySpellingChallenge(
            showCorrect ? noun : CreateTypo(noun),
            noun,
            showCorrect);
    }

    private static string CreateTypo(string noun)
    {
        if (noun.Length < 2) return noun + "x";

        var candidates = Enumerable.Range(0, noun.Length - 1)
            .Where(index => noun[index] != noun[index + 1])
            .ToList();
        if (candidates.Count == 0) return noun + "x";

        var preferred = candidates.Where(index => index > 0).ToList();
        var pool = preferred.Count > 0 ? preferred : candidates;
        var swapIndex = pool[Random.Shared.Next(pool.Count)];
        var letters = noun.ToCharArray();
        (letters[swapIndex], letters[swapIndex + 1]) =
            (letters[swapIndex + 1], letters[swapIndex]);
        return new string(letters);
    }
}

public sealed record KartySpellingChallenge(
    string DisplayText,
    string CorrectText,
    bool IsCorrect);
