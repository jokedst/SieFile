using System.Text;

namespace SieFileFormat.Sie;

public static class SieExtensions
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
        if (source == null) return [];
        separators ??= [' ', '\t'];

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
                // Only " is escaped in sie. All other slashes should be preserved.
                if(currentChar != '"') currentItem.Append('\\');
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
                    if (bracketsOpen)
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

        if (escapeFlag) currentItem.Append('\\');

        var lastCurrentItemString = trimSplits ? currentItem.ToString().Trim() : currentItem.ToString();
        if (!(string.IsNullOrEmpty(lastCurrentItemString) && ignoreEmptyResults && !hadQuotes)) result.Add(lastCurrentItemString);

        return result.ToArray();
    }

    public static string ToSieString(this SieFileType fileType)
        => fileType switch
        {
            SieFileType.Type1 => "1",
            SieFileType.Type2 => "2",
            SieFileType.Type3 => "3",
            SieFileType.Type4I => "4",
            SieFileType.Type4E => "4",
            _ => throw new ArgumentException("Unknown sie file type '" + fileType + "'", nameof(fileType)),
        };

    public static string ToRowType(this AmountType amountType)
        => amountType switch
        {
            AmountType.IncomingBalance => "#IB",
            AmountType.OutgoingBalance => "#UB",
            AmountType.ObjectIncomingBalance => "#OIB",
            AmountType.ObjectOutgoingBalance => "#OUB",
            AmountType.Result => "#RES",
            AmountType.PeriodChange => "#PSALDO",
            AmountType.PeriodBudgetChange => "#PBUDGET",
            _ => throw new InvalidOperationException($"AmountType '{amountType}' has no associated row type.")
        };
}