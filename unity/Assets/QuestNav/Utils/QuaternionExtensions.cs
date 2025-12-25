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
    /// Extension methods for Unity's Quaternion class to convert to array format.
    /// </summary>
    public static class QuaternionExtensions
    {
        /// <summary>
        /// Converts a Quaternion to a float array containing x, y, z, and w components.
        /// </summary>
        /// <param name="quaternion">The Quaternion to convert</param>
        /// <returns>Float array containing [x, y, z, w] values</returns>
        public static float[] ToArray(this Quaternion quaternion)
        {
            return new float[] { quaternion.x, quaternion.y, quaternion.z, quaternion.w };
        }
    }
}
