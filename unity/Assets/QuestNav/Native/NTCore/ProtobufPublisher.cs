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
using Google.Protobuf;

namespace QuestNav.Native.NTCore
{
    /// <summary>
    /// Publisher for protobuf messages over NetworkTables. Handles serialization of protobuf messages to byte arrays.
    /// </summary>
    /// <typeparam name="T">The protobuf message type</typeparam>
    public class ProtobufPublisher<T>
        where T : IMessage<T>
    {
        /// <summary>
        /// The underlying raw publisher for byte array data
        /// </summary>
        private readonly RawPublisher rawPublisher;

        /// <summary>
        /// Creates a new protobuf publisher wrapping the given raw publisher
        /// </summary>
        /// <param name="rawPublisher">The raw publisher to wrap</param>
        internal ProtobufPublisher(RawPublisher rawPublisher)
        {
            this.rawPublisher = rawPublisher;
        }

        /// <summary>
        /// Publishes a protobuf message by serializing it to bytes and sending over NetworkTables
        /// </summary>
        /// <param name="message">The protobuf message to publish</param>
        /// <returns>True if the message was successfully published, false otherwise</returns>
        public bool Set(T message)
        {
            if (message == null)
            {
                return false;
            }

            try
            {
                byte[] data = message.ToByteArray();
                return rawPublisher.Set(data);
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
