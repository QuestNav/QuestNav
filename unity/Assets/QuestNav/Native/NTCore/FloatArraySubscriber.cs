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
    public class FloatArraySubscriber
    {
        private readonly uint handle;

        internal FloatArraySubscriber(uint handle)
        {
            this.handle = handle;
        }

        public unsafe float[] Get(float[] defaultValue)
        {
            float* res;
            UIntPtr len = UIntPtr.Zero;
            fixed (float* ptr = defaultValue)
            {
                res = NtCoreNatives.NT_GetFloatArray(
                    handle,
                    ptr,
                    (UIntPtr)defaultValue.Length,
                    &len
                );
            }

            float[] ret = new float[(int)len];
            for (int i = 0; i < ret.Length; i++)
            {
                ret[i] = res[i];
            }

            NtCoreNatives.NT_FreeFloatArray(res);

            return ret;
        }
    }
}
