namespace Majinfwork.World {
    public interface IPSOWarmupProgress {
        int TotalCount { get; }
        int CurrentCount { get; }
        float NormalizedProgress { get; }
        bool IsComplete { get; }
    }
}
