using System.Text;

public static class StringExtensions
{
    /// <summary>
    /// Splits the string by space/tab, but only when the separator is outside quotes or brackets.
    /// </summary>
    /// <param name="source">The source string to separate.</param>
    /// <param name="splitChars">The characters used to split strings.</param>
    /// <param name="trimSplits">If set to <c>true</c>, split strings are trimmed (whitespaces are removed).</param>
    /// <param name="ignoreEmptyResults">If set to <c>true</c>, empty split results are ignored (not included in the result).</param>
    /// <param name="preserveEscapeCharInQuotes">If set to <c>true</c>, then the escape character (\) used to escape e.g. quotes is included in the results.</param>
    public static string[] SplitSieLine(this string source, char[] separators = null, bool trimSplits = false, bool ignoreEmptyResults = true, bool preserveEscapeCharInQuotes = false)
    {
        // new[] { ' ', '\t' }, false, true, false
        if (source == null) return Array.Empty<string>();
        if(separators == null) separators = [' ', '\t'];

        var result = new List<string>();
        var escapeFlag = false;
        var quotesOpen = false;
        var hadQuotes = false;
        var bracketsOpen = false;
        var currentItem = new StringBuilder();

        foreach (var currentChar in source)
        {
            if (escapeFlag)
            {
                currentItem.Append(currentChar);
                escapeFlag = false;
                continue;
            }

            if (separators.Contains(currentChar) && !quotesOpen && !bracketsOpen)
            {
                var currentItemString = trimSplits ? currentItem.ToString().Trim() : currentItem.ToString();
                currentItem.Clear();
                if (string.IsNullOrEmpty(currentItemString) && ignoreEmptyResults && !hadQuotes) continue;
                result.Add(currentItemString);
                hadQuotes = false;
                continue;
            }

            switch (currentChar)
            {
                default:
                    currentItem.Append(currentChar);
                    break;
                case '\\':
                    if (quotesOpen && preserveEscapeCharInQuotes) currentItem.Append(currentChar);
                    escapeFlag = true;
                    break;
                case '"':
                    if(bracketsOpen)
                      currentItem.Append(currentChar);
                    // Only allow quotes in the beginning of strings
                    else if (!quotesOpen && currentItem.Length == 0)
                        quotesOpen = hadQuotes = true;
                    else if (quotesOpen)
                        quotesOpen = !quotesOpen;
                    else
                        currentItem.Append(currentChar);
                    break;
                case '{':
                    if (!bracketsOpen && !quotesOpen && currentItem.Length == 0)
                        bracketsOpen = true;
                    currentItem.Append(currentChar);
                    break;
                case '}':
                    if (bracketsOpen && !quotesOpen)
                        bracketsOpen = false;
                    currentItem.Append(currentChar);
                    break;
            }
        }

        if (escapeFlag) currentItem.Append("\\");

        var lastCurrentItemString = trimSplits ? currentItem.ToString().Trim() : currentItem.ToString();
        if (!(string.IsNullOrEmpty(lastCurrentItemString) && ignoreEmptyResults)) result.Add(lastCurrentItemString);

        return result.ToArray();
    }
}