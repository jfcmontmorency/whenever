using System.Text;

namespace JFCartier.Whenever.Editor
{
    public static class NameFormatUtility
    {
        /// <summary>
        /// Inserts spaces before capital letters, e.g. "LookingAtCondition" -> "Looking At Condition".
        /// A number sticks to the capital that follows it ("Rigidbody2D" -> "Rigidbody 2D", not
        /// "Rigidbody2 D"): a space goes before a run of digits, but never between a digit and a capital.
        /// </summary>
        public static string AddSpacesToPascalCase(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;

            var result = new StringBuilder(text.Length * 2);
            result.Append(text[0]);

            for (int i = 1; i < text.Length; i++)
            {
                char current = text[i];
                char previous = text[i - 1];

                bool startsNewWord = char.IsUpper(current) && !char.IsUpper(previous) && !char.IsDigit(previous);
                bool startsNumber = char.IsDigit(current) && char.IsLetter(previous);

                if (startsNewWord || startsNumber)
                {
                    result.Append(' ');
                }

                result.Append(text[i]);
            }

            return result.ToString();
        }
    }
}
