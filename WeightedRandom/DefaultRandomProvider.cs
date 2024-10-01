namespace WeightedRandom;
public class DefaultRandomProvider : IRandomProvider
{
    private readonly Random _random = new();

    public double NextDouble()
    {
        return _random.NextDouble();
    }
}
