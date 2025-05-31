using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;

[CreateAssetMenu(fileName = "TeamNumberValidator", menuName = "TMP Input Validators/TeamNumber")]
public class TeamNumberValidator : TMP_InputValidator
{
    // This regex matches a complete positive integer between 1 and 25599.
    private Regex completeRegex = new Regex(
        @"^(?:[1-9]\d{0,3}|1\d{4}|2(?:[0-4]\d{3}|5[0-5]\d{2}))$"
    );

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
