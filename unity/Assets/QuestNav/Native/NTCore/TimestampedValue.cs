namespace QuestNav.Native.NTCore
{
    /// <summary>
    /// Represents a NetworkTables value with its associated timestamp information.
    /// </summary>
    /// <typeparam name="T">The type of the value</typeparam>
    public struct TimestampedValue<T>
    {
        /// <summary>
        /// The actual value from NetworkTables
        /// </summary>
        public T Value { get; set; }

        /// <summary>
        /// When this value was published, in the NetworkTables server time base (nanoseconds).
        /// May be 0 or 1 for values set locally.
        /// </summary>
        public long ServerTime { get; set; }

        /// <summary>
        /// When this value last changed, in local NetworkTables time (nanoseconds, same base as <see cref="NtInstance.Now"/>)
        /// </summary>
        public long LastChange { get; set; }

        /// <summary>
        /// Creates a new timestamped value
        /// </summary>
        /// <param name="value">The value</param>
        /// <param name="serverTime">Publish time in server time, in nanoseconds</param>
        /// <param name="lastChange">Last change time in local NetworkTables time, in nanoseconds</param>
        public TimestampedValue(T value, long serverTime, long lastChange)
        {
            Value = value;
            ServerTime = serverTime;
            LastChange = lastChange;
        }

        public override string ToString()
        {
            return $"TimestampedValue<{typeof(T).Name}> {{ Value = {Value}, ServerTime = {ServerTime}, LastChange = {LastChange} }}";
        }
    }
}
