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
using System.Runtime.InteropServices;

namespace QuestNav.Native.NTCore
{
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct WpiString
    {
        public byte* str;
        public UIntPtr len;
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct NativeNtLogMessage
    {
        public uint level;
        public WpiString filename;
        public uint line;
        public WpiString message;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct NativeTimeSyncEventData
    {
        public long serverTimeOffset;
        public long rtt2;
        public int valid;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct NativeNetworkTableValue
    {
        public NtType type;
        public long lastChange;
        public long serverTime;

        public NtValueUnion data;

        [StructLayout(LayoutKind.Explicit)]
        public struct NtValueUnion
        {
            [FieldOffset(0)]
            public int valueBoolean;

            [FieldOffset(0)]
            public long valueInt;

            [FieldOffset(0)]
            public float valueFloat;

            [FieldOffset(0)]
            public double valueDouble;

            [FieldOffset(0)]
            public WpiString valueString;

            [FieldOffset(0)]
            public NtValueRaw valueRaw;

            [FieldOffset(0)]
            public NtValueBooleanArray arrBoolean;

            [FieldOffset(0)]
            public NtValueDoubleArray arrDouble;

            [FieldOffset(0)]
            public NtValueFloatArray arrFloat;

            [FieldOffset(0)]
            public NtValueIntArray arrInt;

            [FieldOffset(0)]
            public NtValueStringArray arrString;

            public unsafe struct NtValueRaw
            {
                public byte* Data;
                public UIntPtr Size;
            }

            public unsafe struct NtValueBooleanArray
            {
                public int* Arr;
                public UIntPtr Size;
            }

            public unsafe struct NtValueDoubleArray
            {
                public double* Arr;

                public UIntPtr Size;
            }

            public unsafe struct NtValueFloatArray
            {
                public float* Arr;

                public UIntPtr Size;
            }

            public unsafe struct NtValueIntArray
            {
                public long* Arr;

                public UIntPtr Size;
            }

            public unsafe struct NtValueStringArray
            {
                public WpiString* Arr;
                public UIntPtr Size;
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct NativeValueEventData
    {
        public int topic;
        public int subentry;
        public NativeNetworkTableValue value;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct NativeTopicInfo
    {
        public int topic;
        public WpiString name;
        public NtType type;
        public WpiString typeStr;
        public WpiString properties;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct NativeConnectionInfo
    {
        public WpiString remoteId;
        public WpiString remoteIp;
        public uint remotePort;
        public ulong lastUpdate;
        public uint protocolVersion;
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct NativeNtEvent
    {
        public uint listenerHandle;
        public uint flags;
        public NtEventUnion data;

        [StructLayout(LayoutKind.Explicit)]
        public struct NtEventUnion
        {
            [FieldOffset(0)]
            public NativeConnectionInfo connInfo;

            [FieldOffset(0)]
            public NativeTopicInfo topicInfo;

            [FieldOffset(0)]
            public NativeValueEventData valueData;

            [FieldOffset(0)]
            public NativeNtLogMessage logMessage;

            [FieldOffset(0)]
            public NativeTimeSyncEventData timeSyncData;
        }
    }

    public enum NtType
    {
        NT_UNASSIGNED = 0,
        NT_BOOLEAN = 0x01,
        NT_DOUBLE = 0x02,
        NT_STRING = 0x04,
        NT_RAW = 0x08,
        NT_BOOLEAN_ARRAY = 0x10,
        NT_DOUBLE_ARRAY = 0x20,
        NT_STRING_ARRAY = 0x40,
        NT_RPC = 0x80,
        NT_INTEGER = 0x100,
        NT_FLOAT = 0x200,
        NT_INTEGER_ARRAY = 0x400,
        NT_FLOAT_ARRAY = 0x800,
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct NativePubSubOptions
    {
        /**
         * Structure size. Must be set to sizeof(NT_NativePubSubOptions).
         */
        public uint structSize;

        /**
         * Polling storage size for a subscription. Specifies the maximum number of
         * updates NetworkTables should store between calls to the subscriber's
         * ReadQueue() function. If zero, defaults to 1 if sendAll is false, 20 if
         * sendAll is true.
         */
        public uint pollStorage;

        /**
         * How frequently changes will be sent over the network, in seconds.
         * NetworkTables may send more frequently than this (e.g. use a combined
         * minimum period for all values) or apply a restricted range to this value.
         * The default is 100 ms.
         */
        public double periodic;

        /**
         * For subscriptions, if non-zero, value updates for ReadQueue() are not
         * queued for this publisher.
         */
        public uint excludePublisher;

        /**
         * Send all value changes over the network.
         */
        public int sendAll;

        /**
         * For subscriptions, don't ask for value changes (only topic announcements).
         */
        public int topicsOnly;

        /**
         * Perform prefix match on subscriber topic names. Is ignored/overridden by
         * Subscribe() functions; only present in struct for the purposes of getting
         * information about subscriptions.
         */
        public int prefixMatch;

        /**
         * Preserve duplicate value changes (rather than ignoring them).
         */
        public int keepDuplicates;

        /**
         * For subscriptions, if remote value updates should not be queued for
         * ReadQueue(). See also disableLocal.
         */
        public int disableRemote;

        /**
         * For subscriptions, if local value updates should not be queued for
         * ReadQueue(). See also disableRemote.
         */
        public int disableLocal;

        /**
         * For entries, don't queue (for ReadQueue) value updates for the entry's
         * internal publisher.
         */
        public int excludeSelf;

        /**
         * For subscriptions, don't share the existence of the subscription with the
         * network. Note this means updates will not be received from the network
         * unless another subscription overlaps with this one, and the subscription
         * will not appear in metatopics.
         */
        public int hidden;
    }

    public unsafe class NtCoreNatives
    {
        public const int NT_DEFAULT_PORT4 = 5810;

        // public const string NT_LIBRARY = "ntcore";

        [DllImport("ntcore")]
        public static extern uint NT_GetDefaultInstance();

        [DllImport("ntcore")]
        public static extern uint NT_GetEntryEx(
            uint topic,
            NtType type,
            WpiString* typeStr,
            NativePubSubOptions* options
        );

        [DllImport("ntcore")]
        public static extern void NT_StartClient4(uint inst, WpiString* identity);

        [DllImport("ntcore")]
        public static extern void NT_SetServerTeam(uint inst, uint team, uint port);

        [DllImport("ntcore")]
        public static extern int NT_IsConnected(uint inst);

        [DllImport("ntcore")]
        public static extern uint NT_GetTopic(uint inst, WpiString* name);

        [DllImport("ntcore")]
        public static extern int NT_SetBoolean(uint publisher, long time, bool value);

        [DllImport("ntcore")]
        public static extern int NT_GetBoolean(uint subscriber, bool defaultValue);

        [DllImport("ntcore")]
        public static extern int NT_SetInteger(uint publisher, long time, long value);

        [DllImport("ntcore")]
        public static extern long NT_GetInteger(uint subscriber, long defaultValue);

        [DllImport("ntcore")]
        public static extern int NT_SetDouble(uint publisher, long time, double value);

        [DllImport("ntcore")]
        public static extern double NT_GetDouble(uint subscriber, double defaultValue);

        [DllImport("ntcore")]
        public static extern int NT_SetFloatArray(
            uint publisher,
            long time,
            float* value,
            UIntPtr len
        );

        [DllImport("ntcore")]
        public static extern float* NT_GetFloatArray(
            uint subscriber,
            float* defaultValue,
            UIntPtr defaultLen,
            UIntPtr* len
        );

        [DllImport("ntcore")]
        public static extern int NT_SetString(uint publisher, long time, WpiString* value);

        [DllImport("ntcore")]
        public static extern void NT_GetString(
            uint publisher,
            WpiString* defaultValue,
            WpiString* value
        );

        [DllImport("ntcore")]
        public static extern int NT_SetStringArray(
            uint publisher,
            long time,
            WpiString* value,
            UIntPtr len
        );

        [DllImport("ntcore")]
        public static extern int NT_SetRaw(uint publisher, long time, byte* value, UIntPtr len);

        [DllImport("ntcore")]
        public static extern byte* NT_GetRaw(
            uint subscriber,
            byte* defaultValue,
            UIntPtr defaultLen,
            UIntPtr* len
        );

        [DllImport("ntcore", EntryPoint = "NT_FreeCharArray")]
        public static extern void NT_FreeRaw(byte* value);

        [DllImport("ntcore")]
        public static extern void NT_FreeFloatArray(float* value);

        [DllImport("ntcore")]
        public static extern uint NT_Subscribe(
            uint topic,
            NtType type,
            WpiString* typeStr,
            NativePubSubOptions* options
        );

        [DllImport("ntcore")]
        public static extern uint NT_Publish(
            uint topic,
            NtType type,
            WpiString* typeStr,
            NativePubSubOptions* options
        );

        [DllImport("ntcore")]
        public static extern void NT_SetServerMulti(
            uint inst,
            UIntPtr count,
            WpiString* serverNames,
            uint* ports
        );

        [DllImport("ntcore")]
        public static extern uint NT_CreateListenerPoller(uint inst);

        [DllImport("ntcore")]
        public static extern void NT_DestroyListenerPoller(uint poller);

        [DllImport("ntcore")]
        public static extern NativeNtEvent* NT_ReadListenerQueue(uint poller, UIntPtr* len);

        [DllImport("ntcore")]
        public static extern uint NT_AddPolledLogger(uint poller, uint minLevel, uint maxLevel);

        [DllImport("ntcore")]
        public static extern void NT_DisposeEventArray(NativeNtEvent* arr, UIntPtr count);

        [DllImport("ntcore")]
        public static extern IntPtr NT_ReadQueueValue(uint subentry, out UIntPtr count);

        [DllImport("ntcore")]
        public static extern void NT_DisposeValueArray(IntPtr arr, UIntPtr count);

        [DllImport("ntcore")]
        public static extern long NT_Now();
    }
}
