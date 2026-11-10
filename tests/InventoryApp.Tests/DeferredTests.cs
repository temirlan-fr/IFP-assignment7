using static InventoryApp.Tests.Check;

namespace InventoryApp.Tests;

internal static class DeferredTests
{
    public static IEnumerable<(string, Action)> Cases()
    {
        yield return ("Yield processing starts on iteration and snapshot reuse does not repeat it", () =>
        {
            var calls = 0;
            var data = new[] { MakeProduct("A", "Office", 3), MakeProduct("B", "Office", 4) };
            var score = Scoring.CreateScore(ScoreConfiguration.Create(2, 5).Value);
            long Probe(Product product) { calls++; return score(product); }
            var deferred = Reports.ProjectStock(data, Probe, score);
            Equal(0, calls);
            using (var iterator = deferred.GetEnumerator())
            {
                Equal(0, calls);
                Equal(true, iterator.MoveNext()); Equal(1, calls);
                Equal("A", iterator.Current.Id); Equal(14L, iterator.Current.StandardScore);
            }
            var snapshot = deferred.ToArray();
            Equal(3, calls); Equal(2, snapshot.Length);
            Equal(26L, snapshot.Sum(row => row.StandardScore));
            Equal(26L, snapshot.Sum(row => row.StandardScore)); Equal(3, calls);
            Equal(2, deferred.ToArray().Length); Equal(5, calls);
        });
    }
}
