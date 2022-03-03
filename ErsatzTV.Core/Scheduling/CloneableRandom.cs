using System;

namespace ErsatzTV.Core.Scheduling;

public class CloneableRandom
{
    private readonly int _seed;
    private readonly Random _random;
    private int _count;
        
    public CloneableRandom(int seed)
    {
        _seed = seed;
        _random = new Random(_seed);
    }

    public CloneableRandom Clone()
    {
        var clone = new CloneableRandom(_seed);
            
        for (var i = 0; i < _count; i++)
        {
            clone.Next();
        }

        return clone;
    }

    public int Next()
    {
        _count++;
        return _random.Next();
    }

    public int Next(int maxValue)
    {
        _count++;
        return _random.Next(maxValue);
    }
}