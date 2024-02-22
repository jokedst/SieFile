
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;



Console.WriteLine("Hello, World!");

var sieReader = new SieFile();
sieReader.Read(File.OpenRead(@"SIE4 Exempelfil.SE"));


var allEnc = System.Text.Encoding.GetEncodings();
var enc = System.Text.Encoding.GetEncoding(437);
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
var lines = File.ReadAllLines(@"C:\Users\joawen\AppData\Local\Temp\7zO889B24CB\SIE4 Exempelfil.SE", enc);

Crc32 crc32 = new Crc32();


foreach (var line in lines)
{
    var parts = line.Split(' ', '\t');
    var rowType = parts[0];
}


/// <summary>
/// Represents a SIE file (v 1-4)
/// </summary>
public class SieFile
{
    public bool AlreadyImportedFlag {  get; set; }
    public string? Program { get; set; }
    public string? Contact { get; set; }
    public string? AdressLine1 { get; set; }
    public string? AdressLine2 { get; set; }
    public string? Phone { get; set; }
    public string? CompanySNI { get; set; }


    public void Read(Stream stream)
    {
        // Ensure codepage 437 is loaded
        if (!Encoding.GetEncodings().Any(x => x.CodePage == 437))
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }
        var encoding = Encoding.GetEncoding(437);

        using StreamReader sr = new StreamReader(stream, encoding);
        string? line;
        while ((line = sr.ReadLine()) != null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            //var parts = line.Split(' ', '\t');
            var parts = SplitOutsideQuotes(line, new[] { ' ', '\t' }, false, true, false);
            var rowType = parts[0];

            switch (rowType)
            {
                case "#FLAGGA": this.AlreadyImportedFlag = parts[1] == "1"; break;
                case "#PROGRAM": this.Program = parts[1]; break;
                case "#FORMAT": WarnIf(parts[1] != "PC8","Only format PCB is allowed"); break;
                case "#GEN":
                case "#SIETYP":
                case "#PROSA":
                case "#FTYP":
                case "#FNR":
                case "#ORGNR":
                case "#BKOD": this.CompanySNI = parts[1]; break;
                case "#ADRESS": this.Contact = parts.opt(1); this.AdressLine1 = parts.opt(2); this.AdressLine2 = parts.opt(3); this.Phone = parts.opt(4); break;
                case "#FNAMN":
                case "#RAR":
                case "#TAXAR":
                case "#OMFATTN":
                case "#KPTYP":
                case "#VALUTA":
                case "#KONTO":
                case "#KTYP":
                case "#ENHET":
                case "#SRU":
                case "#DIM": this.Dimensions
                case "#UNDERDIM":
                case "#OBJEKT":
                case "#IB":
                case "#UB":
                case "#OIB":
                case "#OUB":
                case "#RES":
                case "#PSALDO":
                case "#PBUDGET":
                case "#VER":
                case "#TRANS":
                case "#RTRANS":
                case "#BTRANS":
                case "#KSUMMA":

            }
        }
    }

    private void WarnIf(bool statement, string warning)
    {
        if(statement)
        {
            Console.Error.WriteLine(warning);
        }
    }

    public void Write(string program)
    {

    }

    /// <summary>
    /// Splits the string by specified separator, but only when the separator is outside the quotes.
    /// </summary>
    /// <param name="source">The source string to separate.</param>
    /// <param name="splitChars">The characters used to split strings.</param>
    /// <param name="trimSplits">If set to <c>true</c>, split strings are trimmed (whitespaces are removed).</param>
    /// <param name="ignoreEmptyResults">If set to <c>true</c>, empty split results are ignored (not included in the result).</param>
    /// <param name="preserveEscapeCharInQuotes">If set to <c>true</c>, then the escape character (\) used to escape e.g. quotes is included in the results.</param>
    private string[] SplitOutsideQuotes(string source, char[] separators, bool trimSplits = true, bool ignoreEmptyResults = true, bool preserveEscapeCharInQuotes = true)
    {
        if (source == null) return null;

        var result = new List<string>();
        var escapeFlag = false;
        var quotesOpen = false;
        var currentItem = new StringBuilder();

        foreach (var currentChar in source)
        {
            if (escapeFlag)
            {
                currentItem.Append(currentChar);
                escapeFlag = false;
                continue;
            }

            if (separators.Contains(currentChar) && !quotesOpen)
            {
                var currentItemString = trimSplits ? currentItem.ToString().Trim() : currentItem.ToString();
                currentItem.Clear();
                if (string.IsNullOrEmpty(currentItemString) && ignoreEmptyResults) continue;
                result.Add(currentItemString);
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
                    //currentItem.Append(currentChar);
                    // Only allow quotes in the beginning of strings
                    if (!quotesOpen && currentItem.Length == 0)
                        quotesOpen = true;
                    else if(quotesOpen)
                        quotesOpen = !quotesOpen;
                    break;
            }
        }

        if (escapeFlag) currentItem.Append("\\");

        var lastCurrentItemString = trimSplits ? currentItem.ToString().Trim() : currentItem.ToString();
        if (!(string.IsNullOrEmpty(lastCurrentItemString) && ignoreEmptyResults)) result.Add(lastCurrentItemString);

        return result.ToArray();
    }
}

public static class SieExtensions 
{ 
    /// <summary>
    /// Returns an optional part. If it doesn't exist, returns empty string
    /// </summary>
    public static string opt(this string[] parts, int index)
    {
        if (parts.Length <= index) return string.Empty; 
        return parts[index];
    }
}