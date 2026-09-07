namespace RatioMaster.Core.Abstractions
{
    /// <summary>Source of the current time, replaceable in tests.</summary>
    public interface ISystemClock
    {
        DateTimeOffset Now { get; }
    }

    public sealed class SystemClock : ISystemClock
    {
        public static SystemClock Instance { get; } = new();

        public DateTimeOffset Now => DateTimeOffset.Now;
    }
}
