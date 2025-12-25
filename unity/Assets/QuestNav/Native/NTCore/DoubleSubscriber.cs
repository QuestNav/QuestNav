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
    public class DoubleSubscriber
    {
        private readonly uint handle;

        internal DoubleSubscriber(uint handle)
        {
            this.handle = handle;
        }

        public double Get(double defaultValue)
        {
            return NtCoreNatives.NT_GetDouble(handle, defaultValue);
        }
    }
}
