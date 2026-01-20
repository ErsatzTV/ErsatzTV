using ErsatzTV.Core.Interfaces.Scheduling;
using NCalc;

namespace ErsatzTV.Core.Scheduling;

public static class CountExpression
{
    public static int Evaluate(
        string countExpression,
        IMediaCollectionEnumerator enumerator,
        Random random,
        CancellationToken cancellationToken)
    {
        int enumeratorCount = enumerator is PlaylistEnumerator playlistEnumerator
            ? playlistEnumerator.CountForRandom
            : enumerator.Count;
        var expression = new Expression(countExpression);
        expression.EvaluateParameter += (name, e) =>
        {
            e.Result = name switch
            {
                "count" => enumeratorCount,
                "random" => enumeratorCount > 0 ? random.Next() % enumeratorCount : 0,
                _ => e.Result
            };
        };

        object expressionResult = expression.Evaluate(cancellationToken);
        return expressionResult switch
        {
            double doubleResult => (int)Math.Floor(doubleResult),
            int intResult => intResult,
            _ => 0
        };
    }
}
