using System.Globalization;
using NCalc;
using NCalc.Handlers;

namespace ErsatzTV.Infrastructure.Streaming.Graphics;

public static class OpacityExpressionHelper
{
    public static void EvaluateFunction(string name, FunctionArgs args)
    {
        switch (name)
        {
            case "LinearFadePoints":
            {
                if (args.Parameters.Length != 5)
                {
                    throw new ArgumentException("LinearFadePoints() requires 5 arguments.");
                }

                double time = Convert.ToDouble(args.Parameters[0].Evaluate(), CultureInfo.CurrentCulture);
                double start = Convert.ToDouble(args.Parameters[1].Evaluate(), CultureInfo.CurrentCulture);
                double peakStart = Convert.ToDouble(args.Parameters[2].Evaluate(), CultureInfo.CurrentCulture);
                double peakEnd = Convert.ToDouble(args.Parameters[3].Evaluate(), CultureInfo.CurrentCulture);
                double end = Convert.ToDouble(args.Parameters[4].Evaluate(), CultureInfo.CurrentCulture);

                args.Result = LinearFadePoints(time, start, peakStart, peakEnd, end);
                break;
            }
            case "LinearFadeDuration":
            {
                if (args.Parameters.Length != 4)
                {
                    throw new ArgumentException("LinearFadeDuration() requires 4 arguments.");
                }

                double time = Convert.ToDouble(args.Parameters[0].Evaluate(), CultureInfo.CurrentCulture);
                double start = Convert.ToDouble(args.Parameters[1].Evaluate(), CultureInfo.CurrentCulture);
                double fadeSeconds = Convert.ToDouble(args.Parameters[2].Evaluate(), CultureInfo.CurrentCulture);
                double peakSeconds = Convert.ToDouble(args.Parameters[3].Evaluate(), CultureInfo.CurrentCulture);

                args.Result = LinearFadeDuration(time, start, fadeSeconds, peakSeconds);
                break;
            }
        }
    }

    private static double LinearFadePoints(double time, double start, double peakStart, double peakEnd, double end)
    {
        if (time < start || time >= end)
        {
            return 0;
        }

        // fade in
        if (time < peakStart)
        {
            return (time - start) / (peakStart - start);
        }

        // solid
        if (time < peakEnd)
        {
            return 1.0;
        }

        // fade out
        return (end - time) / (end - peakEnd);
    }

    private static double LinearFadeDuration(double time, double start, double fadeSeconds, double peakSeconds)
    {
        // edge case with no fade
        if (fadeSeconds <= 0)
        {
            double noFadeEnd = start + peakSeconds;
            return (time >= start && time < noFadeEnd) ? 1.0 : 0.0;
        }

        double peakStart = start + fadeSeconds;
        double peakEnd = peakStart + peakSeconds;
        double end = peakEnd + fadeSeconds;

        return LinearFadePoints(time, start, peakStart, peakEnd, end);
    }

    public static float GetOpacity(
        Expression expression,
        TimeSpan timeOfDay,
        TimeSpan contentTime,
        TimeSpan contentTotalTime,
        TimeSpan channelTime)
    {
        expression.Parameters["content_seconds"] = contentTime.TotalSeconds;
        expression.Parameters["content_total_seconds"] = contentTotalTime.TotalSeconds;
        expression.Parameters["channel_seconds"] = channelTime.TotalSeconds;
        expression.Parameters["time_of_day_seconds"] = timeOfDay.TotalSeconds;

        object expressionResult = expression.Evaluate();
        return Convert.ToSingle(expressionResult, CultureInfo.InvariantCulture);
    }
}
