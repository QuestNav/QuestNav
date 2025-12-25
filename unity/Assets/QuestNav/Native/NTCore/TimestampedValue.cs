// QUESTNAV
// https://github.com/QuestNav
// Copyright (C) 2026 QuestNav
// SPDX-License-Identifier: LGPL-3.0-or-later
//
// This file is part of QuestNav.
//
// QuestNav is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// QuestNav is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public License
// along with QuestNav. If not, see https://www.gnu.org/licenses/.
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
