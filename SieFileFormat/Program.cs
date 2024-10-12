using System.Reflection.Metadata;
using System.Text;

var sieReader = new SieFileReader();

Console.WriteLine("Testing SIE files");

TestFile(@"C:\Users\joawen\Source\Repos\SieFile\SieFileTests\sie_test_files\Norstedts Bokslut SIE 4I.si");

sieReader.Read(File.OpenRead(@"SIE4 Exempelfil.SE"), ".SE");


TestFile(@"D:\Downloads\sitest20240225_031358.si");



var allEnc = System.Text.Encoding.GetEncodings();
var enc = System.Text.Encoding.GetEncoding(437);
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
var lines = File.ReadAllLines(@"SIE4 Exempelfil.SE", enc);


//var writer = new SieFileWriter();
var stream = new MemoryStream();
using (StreamWriter sw = new StreamWriter(stream, enc))
{
    Row(sw, "#MUU", "one", null, "two", null);
    Row(sw, "#UNO");
    Row(sw, "#UNO",null);
    Row(sw, "#UNO", null,null);
};
stream.TryGetBuffer(out var buffer);
var linex = Encoding.GetEncoding(437).GetString(buffer);
Console.WriteLine(linex);

foreach (var line in lines)
{
    var parts = line.Split(' ', '\t');
    var rowType = parts[0];
}


void TestFile(string filename)
{
    var sie = sieReader.Read(File.OpenRead(filename), filename);
    foreach (var error in sieReader.Errors)
    {
        Console.WriteLine("ERROR:" + Path.GetFileName(filename) + ": " + error);
    }
    foreach (var warning in sieReader.Warnings)
    {
        Console.WriteLine("WARN:" + Path.GetFileName(filename) + ": " + warning);
    }

    Console.WriteLine($"Read file '{Path.GetFileName(filename)}'");
    Console.WriteLine($"File had {sie.Accounts.Count} accounts" );
    Console.WriteLine("File had {0} verifications", sie.Verifications.Count);
}


void Row(StreamWriter sw, string sieKeyword, params string[] optionalParameters)
{
    sw.Write(sieKeyword);

    var lastParamWithValue = (optionalParameters?.Length??0) - 1;
    while (lastParamWithValue >= 0 && optionalParameters[lastParamWithValue] == null) 
        lastParamWithValue--;

    for(int i = 0; i <= lastParamWithValue; i++)
    {
        sw.Write(' ');
        sw.Write(Escape(optionalParameters[i]));
    }
    sw.WriteLine();
}
string Escape(string data, bool andPrefix = false)
{
    if (string.IsNullOrEmpty(data)) return (andPrefix ? " " : "") + "\"\"";
    if (data.Contains(' ')) return (andPrefix ? " " : "") + "\"" + data.Replace("\"", "\\\"") + "\"";
    return (andPrefix ? " " : "") + data;
}