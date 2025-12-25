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
using System;
using System.Collections.Generic;
using Google.Protobuf;
using QuestNav.Utils;

namespace QuestNav.Native.NTCore
{
    /// <summary>
    /// Subscriber for protobuf messages over NetworkTables. Handles deserialization of byte arrays to protobuf messages.
    /// </summary>
    /// <typeparam name="T">The protobuf message type</typeparam>
    public class ProtobufSubscriber<T>
        where T : IMessage<T>, new()
    {
        /// <summary>
        /// The underlying raw subscriber for byte array data
        /// </summary>
        private readonly RawSubscriber rawSubscriber;

        /// <summary>
        /// Parser for deserializing byte arrays to protobuf messages
        /// </summary>
        private readonly MessageParser<T> parser;

        /// <summary>
        /// Creates a new protobuf subscriber wrapping the given raw subscriber
        /// </summary>
        /// <param name="rawSubscriber">The raw subscriber to wrap</param>
        internal ProtobufSubscriber(RawSubscriber rawSubscriber)
        {
            this.rawSubscriber = rawSubscriber;
            this.parser = new MessageParser<T>(() => new T());
        }

        /// <summary>
        /// Gets the latest protobuf message from NetworkTables, or returns the default value if none available
        /// </summary>
        /// <param name="defaultValue">The default value to return if no message is available</param>
        /// <returns>The latest protobuf message or the default value</returns>
        public T Get(T defaultValue)
        {
            if (defaultValue == null)
            {
                defaultValue = new T();
            }

            byte[] defaultBytes = defaultValue.ToByteArray();
            byte[] data = rawSubscriber.Get(defaultBytes);

            try
            {
                return parser.ParseFrom(data);
            }
            catch (Exception)
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// Reads the protobuf message queue for this subscriber, returning only successfully parsed messages.
        /// </summary>
        /// <returns>An array of protobuf messages of type T from the queue</returns>
        public TimestampedValue<T>[] ReadQueueValues()
        {
            var rawQueue = rawSubscriber.ReadQueue();
            if (rawQueue == null)
            {
                return Array.Empty<TimestampedValue<T>>();
            }
            var list = new List<TimestampedValue<T>>(rawQueue.Length);
            for (int i = 0; i < rawQueue.Length; i++)
            {
                try
                {
                    // Extract the byte array from the TimestampedValue
                    var msg = parser.ParseFrom(rawQueue[i].Value);
                    if (msg != null)
                        list.Add(
                            new TimestampedValue<T>
                            {
                                LastChange = rawQueue[i].LastChange,
                                ServerTime = rawQueue[i].ServerTime,
                                Value = msg,
                            }
                        );
                }
                catch (Exception e)
                {
                    // Log and discard messages that fail to parse
                    QueuedLogger.LogException(
                        $"ProtobufSubscriber<{typeof(T).Name}>: Failed to parse queued message at index {i}, skipping.",
                        e
                    );
                }
            }
            return list.ToArray();
        }
    }
}
