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
    public unsafe class StringPublisher
    {
        private readonly uint handle;

        internal StringPublisher(uint handle)
        {
            this.handle = handle;
        }

        public unsafe bool Set(string value)
        {
            byte[] valueUtf8 = value is not null
                ? Encoding.UTF8.GetBytes(value)
                : Array.Empty<byte>();

            fixed (byte* ptr = valueUtf8)
            {
                WpiString str = new WpiString { str = ptr, len = (UIntPtr)valueUtf8.Length };

                return NtCoreNatives.NT_SetString(handle, 0, &str) != 0;
            }
        }
    }
}
