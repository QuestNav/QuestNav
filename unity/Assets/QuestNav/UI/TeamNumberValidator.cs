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
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;

/// <summary>
/// TextMeshPro input validator for FRC team numbers (1-25599).
/// Prevents invalid input during typing and enforces proper team number format.
/// </summary>
[CreateAssetMenu(fileName = "TeamNumberValidator", menuName = "TMP Input Validators/TeamNumber")]
public class TeamNumberValidator : TMP_InputValidator
{
    /// <summary>
    /// Regular expression that matches a complete positive integer between 1 and 25599 (valid FRC team number range).
    ///
    /// REGEX BREAKDOWN: ^(?:[1-9]\d{0,3}|1\d{4}|2(?:[0-4]\d{3}|5[0-5]\d{2}))$
    /// - [1-9]\d{0,3}: Matches 1-4 digits starting with 1-9 (covers 1-9999)
    /// - 1\d{4}: Matches 5 digits starting with 1 (covers 10000-19999)
    /// - 2[0-4]\d{3}: Matches 5 digits starting with 20-24 (covers 20000-24999)
    /// - 25[0-5]\d{2}: Matches 5 digits starting with 250-255 (covers 25000-25599)
    ///
    /// This covers the complete valid range of FRC team numbers as allocated by FIRST.
    /// </summary>
    private Regex completeRegex = new Regex(
        @"^(?:[1-9]\d{0,3}|1\d{4}|2(?:[0-4]\d{3}|5[0-5]\d{2}))$"
    );

    /// <summary>
    /// Validates character input for team number field, ensuring only valid FRC team numbers can be entered.
    /// </summary>
    /// <param name="text">Current text in the input field</param>
    /// <param name="pos">Current cursor position</param>
    /// <param name="ch">Character being input</param>
    /// <returns>The validated character, or '\0' if the character should be rejected</returns>
    public override char Validate(ref string text, ref int pos, char ch)
    {
        // Allow control characters (such as backspace).
        if (char.IsControl(ch))
            return ch;

        // Only allow digits.
        if (!char.IsDigit(ch))
            return '\0';

        // Build the prospective new text.
        string newText = text.Insert(pos, ch.ToString());

        // Disallow any string that would start with a leading zero.
        if (newText.Length > 0 && newText[0] == '0')
            return '\0';

        // Enforce a maximum length of 5 digits.
        if (newText.Length > 5)
            return '\0';

        // For a complete (5-digit) entry, use the regex to verify the valid range.
        if (newText.Length == 5)
        {
            if (!completeRegex.IsMatch(newText))
                return '\0';
        }
        // For partial input (fewer than 5 digits), we allow any entry that doesn�t break our basic rules.
        // (For example, "2", "25", "255", or "2559" can all be extended to a valid number.)

        // Accept the input.
        text = newText;
        pos++;
        return ch;
    }
}
