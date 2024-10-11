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