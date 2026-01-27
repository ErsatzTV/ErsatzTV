using ErsatzTV.Core.Domain;
using NCalc;

namespace ErsatzTV.Core.Scheduling;

public static class FillerExpression
{
    public static List<MediaChapter> FilterChapters(
        string fillerExpression,
        List<MediaChapter> effectiveChapters,
        PlayoutItem playoutItem)
    {
        if (effectiveChapters.Count == 0 || string.IsNullOrWhiteSpace(fillerExpression))
        {
            return effectiveChapters;
        }

        var candidateChapters = effectiveChapters.SkipLast().ToList();

        var newChapters = new List<MediaChapter>();

        TimeSpan start = effectiveChapters.Select(c => c.StartTime).Min();
        TimeSpan end = effectiveChapters.Select(c => c.EndTime).Max();

        double lastFiller = start.TotalSeconds - 99999.0;
        double contentDuration = (playoutItem.FinishOffset - playoutItem.StartOffset).TotalSeconds;
        var matches = 0;

        for (var index = 0; index < candidateChapters.Count; index++)
        {
            MediaChapter chapter = candidateChapters[index];
            TimeSpan chapterPoint = chapter.EndTime;
            var expression = new Expression(fillerExpression, ExpressionOptions.CaseInsensitiveStringComparer);
            int chapterNum = index + 1;
            double sinceLastFiller = chapterPoint.TotalSeconds - lastFiller;
            int matchedPoints = matches;
            expression.EvaluateParameter += (name, e) =>
            {
                e.Result = name switch
                {
                    "last_mid_filler" => sinceLastFiller,
                    "total_points" => effectiveChapters.Count - 1,
                    "total_duration" => contentDuration,
                    "total_progress" => chapterPoint.TotalSeconds / end.TotalSeconds,
                    "remaining_duration" => contentDuration - chapterPoint.TotalSeconds,
                    "matched_points" => matchedPoints,
                    "point" => chapterPoint.TotalSeconds,
                    "num" => chapterNum,
                    "title" => chapter.Title ?? string.Empty,
                    _ => e.Result
                };
            };

            if (expression.Evaluate() as bool? == true)
            {
                matches += 1;
                lastFiller = chapterPoint.TotalSeconds;
                newChapters.Add(effectiveChapters[index]);
            }
        }

        if (newChapters.Count > 0)
        {
            newChapters[0].StartTime = start;

            TimeSpan currentTime = start;
            foreach (MediaChapter chapter in newChapters)
            {
                chapter.StartTime = currentTime;
                currentTime = chapter.EndTime;
            }

            newChapters.Add(new MediaChapter { StartTime = currentTime, EndTime = end });
        }

        return newChapters;
    }
}
