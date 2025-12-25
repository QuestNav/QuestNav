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
using System.Text;

namespace QuestNav.Native.NTCore
{
    public class StringSubscriber
    {
        private readonly uint handle;

        internal StringSubscriber(uint handle)
        {
            this.handle = handle;
        }

        public unsafe string Get(string defaultValue)
        {
            byte[] valueUtf8 = defaultValue is not null
                ? Encoding.UTF8.GetBytes(defaultValue)
                : Array.Empty<byte>();

            string result = null;
            fixed (byte* ptr = valueUtf8)
            {
                WpiString defaultWpi = new WpiString { str = ptr, len = (UIntPtr)valueUtf8.Length };
                WpiString outValue = new WpiString();
                NtCoreNatives.NT_GetString(handle, &defaultWpi, &outValue);

                if (outValue.str == defaultWpi.str)
                {
                    // GetString returned our default value - no need to free.
                    result = defaultValue;
                }
                else if (outValue.str != null)
                {
                    try
                    {
                        // Marshal string back to managed memory
                        result = Encoding.UTF8.GetString(outValue.str, (int)outValue.len);
                    }
                    finally
                    {
                        NtCoreNatives.NT_FreeRaw(outValue.str);
                    }
                }
            }

            return result;
        }
    }
}
