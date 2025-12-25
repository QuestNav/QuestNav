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
using UnityEngine;

namespace QuestNav.Utils
{
    /// <summary>
    /// Extension methods for Unity's Vector3 class to convert to array format.
    /// </summary>
    public static class VectorExtensions
    {
        /// <summary>
        /// Converts a Vector3 to a float array containing x, y, and z components.
        /// </summary>
        /// <param name="vector">The Vector3 to convert</param>
        /// <returns>Float array containing [x, y, z] values</returns>
        public static float[] ToArray(this Vector3 vector)
        {
            return new float[] { vector.x, vector.y, vector.z };
        }
    }
}
