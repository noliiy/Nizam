using Nizam.Scheduling.Graph;

namespace Nizam.Scheduling.CPM;

public static class CriticalPathCalculator
{
    public static void Calculate(IReadOnlyList<ActivityNode> nodes, int thresholdMinutes)
    {
        foreach (var node in nodes)
            node.IsCritical = node.TotalFloatMinutes <= thresholdMinutes;
    }
}
