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
        /// The NetworkTables server timestamp when this value was published (in microseconds since server start)
        /// </summary>
        public long ServerTime { get; set; }

        /// <summary>
        /// The timestamp when this value was last changed (in microseconds since Unix epoch)
        /// </summary>
        public long LastChange { get; set; }

        /// <summary>
        /// Creates a new timestamped value
        /// </summary>
        /// <param name="value">The value</param>
        /// <param name="serverTime">Server timestamp in microseconds since server start</param>
        /// <param name="lastChange">Timestamp when value last changed in microseconds since Unix epoch</param>
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
