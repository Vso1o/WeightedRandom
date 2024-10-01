using WeightedRandom;

namespace ConsoleShowcase;

internal class Program
{
    public enum DropTypes
    {
        Sample1 = 0,
        Sample2 = 1,
        Sample3 = 2,
        Sample4 = 3,
        Sample5 = 4,
        Sample6 = 5,
    }

    static void Main(string[] args)
    {
        var weightedRandom = new WeightedRandom<DropTypes, int>();

        weightedRandom.AddItem(DropTypes.Sample1, 1200);
        weightedRandom.AddItem(DropTypes.Sample2, 550);
        weightedRandom.AddItem(DropTypes.Sample3, 50);
        weightedRandom.AddItem(DropTypes.Sample4, 550);
        weightedRandom.AddItem(DropTypes.Sample5, 50);
        weightedRandom.AddItem(DropTypes.Sample6, 550);

        //weightedRandom.Remove(DropTypes.Sample1);
        //weightedRandom.Remove(DropTypes.Sample2);

        Dictionary<DropTypes, int> dick = [];
        dick.Add(DropTypes.Sample1, 0);
        dick.Add(DropTypes.Sample2, 0);
        dick.Add(DropTypes.Sample3, 0);
        dick.Add(DropTypes.Sample4, 0);
        dick.Add(DropTypes.Sample5, 0);
        dick.Add(DropTypes.Sample6, 0);

        foreach (var item in dick)
        {
            Console.WriteLine($"Chance for {item.Key} to proc is {weightedRandom.GetProcChance(item.Key):F4} with weight of {weightedRandom.GetWeight(item.Key)}");
        }

        for (var i = 0; i < 1000000; i++)
        {
            var item = weightedRandom.GetRandomItem();

            dick[item] += 1;
        }

        foreach (var item in dick)
        {
            Console.WriteLine($"{item.Key} count was {dick[item.Key]}");
        }
    }
}
