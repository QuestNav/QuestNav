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

namespace QuestNav.Native.NTCore
{
    /// <summary>
    /// Options for NetworkTables publishers and subscribers.
    /// Controls behavior such as update frequency, storage, and filtering.
    /// </summary>
    public struct PubSubOptions
    {
        /// <summary>
        /// Default publisher/subscriber options with reasonable defaults for most use cases.
        /// </summary>
        public static PubSubOptions AllDefault { get; } =
            new PubSubOptions()
            {
                Periodic = 0.005,
                SendAll = true,
                KeepDuplicates = true,
                PollStorage = 0,
                ExcludePublisher = 0,
                TopicsOnly = false,
                PrefixMatch = false,
                DisableRemote = false,
                DisableLocal = false,
                ExcludeSelf = false,
                Hidden = false,
            };

        /// <summary>
        /// How frequently changes will be sent over the network, in seconds.
        /// NetworkTables may send more frequently than this (e.g. use a combined
        /// minimum period for all values) or apply a restricted range to this value.
        /// The default is 100 ms (0.1 seconds).
        /// </summary>
        public double Periodic { get; set; }

        /// <summary>
        /// Send all value changes over the network.
        /// If false, only the latest value is sent when the periodic timer expires.
        /// If true, all value changes are sent as they occur.
        /// </summary>
        public bool SendAll { get; set; }

        /// <summary>
        /// Preserve duplicate value changes (rather than ignoring them).
        /// If false, consecutive identical values are filtered out.
        /// If true, all value updates are preserved even if they contain the same data.
        /// </summary>
        public bool KeepDuplicates { get; set; }

        /// <summary>
        /// Polling storage size for a subscription. Specifies the maximum number of
        /// updates NetworkTables should store between calls to the subscriber's
        /// ReadQueue() function. If zero, defaults to 1 if SendAll is false, 20 if
        /// SendAll is true.
        /// </summary>
        public uint PollStorage { get; set; }

        /// <summary>
        /// For subscriptions, if non-zero, value updates for ReadQueue() are not
        /// queued for this publisher. Use this to exclude updates from a specific
        /// publisher by its handle/ID.
        /// </summary>
        public uint ExcludePublisher { get; set; }

        /// <summary>
        /// For subscriptions, don't ask for value changes (only topic announcements).
        /// If true, the subscriber will only receive notifications when topics are
        /// created or destroyed, but not when their values change.
        /// </summary>
        public bool TopicsOnly { get; set; }

        /// <summary>
        /// Perform prefix match on subscriber topic names. Is ignored/overridden by
        /// Subscribe() functions; only present in struct for the purposes of getting
        /// information about subscriptions. When true, the subscription will match
        /// all topics that start with the specified name prefix.
        /// </summary>
        public bool PrefixMatch { get; set; }

        /// <summary>
        /// For subscriptions, if remote value updates should not be queued for
        /// ReadQueue(). See also DisableLocal. If true, updates from remote clients
        /// (other NetworkTables instances) will not be stored in the queue.
        /// </summary>
        public bool DisableRemote { get; set; }

        /// <summary>
        /// For subscriptions, if local value updates should not be queued for
        /// ReadQueue(). See also DisableRemote. If true, updates from the local
        /// client (same NetworkTables instance) will not be stored in the queue.
        /// </summary>
        public bool DisableLocal { get; set; }

        /// <summary>
        /// For entries, don't queue (for ReadQueue) value updates for the entry's
        /// internal publisher. If true, updates published by the same entry that
        /// is also subscribing will not be queued for ReadQueue().
        /// </summary>
        public bool ExcludeSelf { get; set; }

        /// <summary>
        /// For subscriptions, don't share the existence of the subscription with the
        /// network. Note this means updates will not be received from the network
        /// unless another subscription overlaps with this one, and the subscription
        /// will not appear in metatopics. Useful for internal monitoring without
        /// affecting network traffic.
        /// </summary>
        public bool Hidden { get; set; }

        /// <summary>
        /// Converts this managed PubSubOptions to the native NativePubSubOptions structure
        /// used by the NetworkTables native library.
        /// </summary>
        /// <returns>A NativePubSubOptions struct with equivalent settings</returns>
        public unsafe NativePubSubOptions ToNative()
        {
            NativePubSubOptions native = new NativePubSubOptions
            {
                structSize = (uint)sizeof(NativePubSubOptions),
                periodic = Periodic,
                sendAll = SendAll ? 1 : 0,
                keepDuplicates = KeepDuplicates ? 1 : 0,
                pollStorage = PollStorage,
                excludePublisher = ExcludePublisher,
                topicsOnly = TopicsOnly ? 1 : 0,
                prefixMatch = PrefixMatch ? 1 : 0,
                disableRemote = DisableRemote ? 1 : 0,
                disableLocal = DisableLocal ? 1 : 0,
                excludeSelf = ExcludeSelf ? 1 : 0,
                hidden = Hidden ? 1 : 0,
            };

            return native;
        }
    }
}
