using static InventoryApp.Tests.Check;

namespace InventoryApp.Tests;

internal static class ScoringTests
{
    public static IEnumerable<(string, Action)> Cases()
    {
        yield return ("Partially applied configurations produce different scores on the same data", () =>
        {
            var standard = Scoring.CreateScore(ScoreConfiguration.Create(2, 5).Value);
            var urgent = Scoring.CreateScore(ScoreConfiguration.Create(5, 20).Value);
            var product = MakeProduct("P", "Office", 3);
            Equal(14L, standard(product)); Equal(35L, urgent(product));
            var rows = Reports.LowStock([product], standard, urgent);
            Equal(14L, rows[0].StandardScore); Equal(35L, rows[0].UrgentScore);
            Equal(25L, standard(MakeProduct("Z", "Office", 0)));
            Equal(70L, urgent(MakeProduct("Z", "Office", 0)));
            Equal(0L, urgent(MakeProduct("Z", "Office", 0, level: 0)));
        });
        yield return ("Scoring configuration rejects invalid weights and bonuses", () =>
        {
            Equal(false, ScoreConfiguration.Create(0, 0).IsSuccess);
            Equal(false, ScoreConfiguration.Create(101, 0).IsSuccess);
            Equal(false, ScoreConfiguration.Create(2, -1).IsSuccess);
            Equal(false, ScoreConfiguration.Create(2, 1001).IsSuccess);
        });
    }
}
