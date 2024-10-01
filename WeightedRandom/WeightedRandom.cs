namespace WeightedRandom;

public record WeightedItem<TItem, TWeight>(TItem Item, TWeight Weight);

public interface IWeightedRandom<TItem, TWeight> where TWeight : struct, IComparable
{
    void AddItem(WeightedItem<TItem, TWeight> item);
    void AddItem(TItem item, TWeight weight);
    TItem GetRandomItem();
    double GetProcChance(TItem item);
    TWeight GetWeight(TItem item);
    bool Remove(TItem item);
}

public class WeightedRandom<TItem, TWeight> : IWeightedRandom<TItem, TWeight> where TWeight : struct, IComparable
{
    private TWeight _totalWeight;
    private readonly IRandomProvider _random;

    private TItem[] _items;
    private TWeight[] _originalWeights;
    private TWeight[] _cumulativeWeights;
    private int _count;

    public const double ResizeThreshold = 0.8;
    public const double ResizeMultiplier = 1.5;
    public const int BaseSize = 32;

    /// <summary>
    /// Ctor for weighted random. Supports long, double, float and int.
    /// </summary>
    /// <param name="randomProvider">Class, which implements IRandomProvider interface. Uses System.Random if ignored. </param>
    /// <param name="items">Items with weights can be passed as params into ctor</param>
    /// <exception cref="InvalidOperationException"></exception>
    public WeightedRandom(IRandomProvider? randomProvider = null, params WeightedItem<TItem, TWeight>[] items)
    {
        if (!IsSupportedWeightType(typeof(TWeight)))
        {
            throw new InvalidOperationException("TWeight must be double, float, int, or long.");
        }

        _random = randomProvider ?? new DefaultRandomProvider();

        _items = new TItem[items.Length];
        _cumulativeWeights = new TWeight[items.Length];
        _originalWeights = new TWeight[items.Length];

        foreach (var item in items)
        {
            AddItem(item.Item, item.Weight);
        }
    }

    public TItem GetRandomItem()
    {
        if (_count <= 0 || ToDouble(_totalWeight) <= 0)
        {
            throw new InvalidOperationException("The weighted random contains no items");
        }

        var randomWeight = _random.NextDouble() * ToDouble(_totalWeight);

        // 10.251233 will be turned into 10 due to loss of data while casting
        // If the weight of item_1 is 10, it will cause more procs on that value
        // E.g. 10.251233 turns into 10, while it has to hit range (10-X)
        // Ceiling for integral values fixes that while preserving lower bounds (9.31 turns into 10 and hits 0-10 range)
        if (typeof(TWeight) == typeof(long) || typeof(TWeight) == typeof(int))
        {
            randomWeight = Math.Ceiling(randomWeight);
        }

        var randomWeightAsTWeight = ConvertToTWeight(randomWeight);

        var index = Array.BinarySearch(_cumulativeWeights, 0, _count, randomWeightAsTWeight);

        if (index < 0)
        {
            index = ~index;
        }

        return _items[index]!;
    }

    #region TypesHandlingHelpers

    private static bool IsSupportedWeightType(Type type)
    {
        return type == typeof(double) || type == typeof(float) || type == typeof(int) || type == typeof(long);
    }

    private static TWeight AddWeights(TWeight left, TWeight right)
    {
        return typeof(TWeight) switch
        {
            _ when typeof(TWeight) == typeof(double) => (TWeight)(object)((double)(object)left + (double)(object)right),
            _ when typeof(TWeight) == typeof(float) => (TWeight)(object)((float)(object)left + (float)(object)right),
            _ when typeof(TWeight) == typeof(int) => (TWeight)(object)((int)(object)left + (int)(object)right),
            _ when typeof(TWeight) == typeof(long) => (TWeight)(object)((long)(object)left + (long)(object)right),
            _ => throw new InvalidOperationException("Unsupported weight type.")
        };
    }

    private static TWeight SubtractWeights(TWeight left, TWeight right)
    {
        return typeof(TWeight) switch
        {
            _ when typeof(TWeight) == typeof(double) => (TWeight)(object)((double)(object)left - (double)(object)right),
            _ when typeof(TWeight) == typeof(float) => (TWeight)(object)((float)(object)left - (float)(object)right),
            _ when typeof(TWeight) == typeof(int) => (TWeight)(object)((int)(object)left - (int)(object)right),
            _ when typeof(TWeight) == typeof(long) => (TWeight)(object)((long)(object)left - (long)(object)right),
            _ => throw new InvalidOperationException("Unsupported weight type.")
        };
    }

	private static double ToDouble(TWeight weight)
	{
		return weight switch
		{
			double d => d,
			float f => f,
			int i => i,
			long l => l,
			_ => throw new InvalidOperationException("Unsupported weight type.")
		};
	}

	private TWeight ConvertToTWeight(double value)
    {
        return typeof(TWeight) switch
        {
            _ when typeof(TWeight) == typeof(double) => (TWeight)(object)value,
            _ when typeof(TWeight) == typeof(float) => (TWeight)(object)(float)value,
            _ when typeof(TWeight) == typeof(int) => (TWeight)(object)(int)value,
            _ when typeof(TWeight) == typeof(long) => (TWeight)(object)(long)value,
            _ => throw new InvalidOperationException("Unsupported weight type.")
        };
    }

    #endregion

    #region ItemPoolModifications

    private void TryResize()
    {
        if (_items.Length == 0)
        {
            _items = new TItem[BaseSize];
            _cumulativeWeights = new TWeight[BaseSize];
            _originalWeights = new TWeight[BaseSize];
            return;
        }

        if ((double)_count / _items.Length > ResizeThreshold)
        {
            var newSize = (int)(_items.Length * ResizeMultiplier);

            var resizedItems = new TItem[newSize];
            var resizedCumulativeWeights = new TWeight[newSize];
            var resizedOriginalWeights = new TWeight[newSize];

            Array.Copy(_items, resizedItems, _items.Length);
            Array.Copy(_cumulativeWeights, resizedCumulativeWeights, _cumulativeWeights.Length);
            Array.Copy(_originalWeights, resizedOriginalWeights, _originalWeights.Length);

            _items = resizedItems;
            _cumulativeWeights = resizedCumulativeWeights;
            _originalWeights = resizedOriginalWeights;
        }
    }

    public void AddItem(WeightedItem<TItem, TWeight> item)
    {
        AddItem(item.Item, item.Weight);
    }

    public void AddItem(TItem item, TWeight weight)
    {
        if (ToDouble(weight) < 0)
        {
            throw new InvalidOperationException("Item weight cannot be negative");
        }

        TryResize();

        _items[_count] = item;
        _originalWeights[_count] = weight;
        _totalWeight = AddWeights(_totalWeight, weight);
        _cumulativeWeights[_count] = _totalWeight;
        _count++;
    }

    public bool Remove(TItem item)
    {
        var index = Array.FindIndex(_items, 0, _count, x => x is not null && x.Equals(item));

        if (index == -1)
        {
            return false;
        }

        var removedItemWeight = _originalWeights[index];

        for (var i = index; i < _count - 1; i++)
        {
            _items[i] = _items[i + 1];
            _originalWeights[i] = _originalWeights[i + 1];
            _cumulativeWeights[i] = SubtractWeights(_cumulativeWeights[i + 1], removedItemWeight);
        }

        _count--;
        _totalWeight = SubtractWeights(_totalWeight, removedItemWeight);

        return true;
    }

    #endregion

    #region InformationalMethods

    public double GetProcChance(TItem item)
    {
        var index = Array.FindIndex(_items, 0, _count, x => x is not null && x.Equals(item));

        if (index == -1)
        {
            return default;
        }

        var originalWeight = _originalWeights[index];

        return ToDouble(originalWeight) / ToDouble(_totalWeight);
    }

    public TWeight GetWeight(TItem item)
    {
        var index = Array.FindIndex(_items, 0, _count, x => x is not null && x.Equals(item));

        if (index == -1)
        {
            return default;
        }

        return _originalWeights[index];
    }

    #endregion
}
