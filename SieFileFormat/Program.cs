using System.IO;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;



Console.WriteLine("Hello, World!");

var sieReader = new SieFileReader();
sieReader.Read(File.OpenRead(@"SIE4 Exempelfil.SE"), ".SE");


TestFile(@"D:\Downloads\sitest20240225_031358.si");



var allEnc = System.Text.Encoding.GetEncodings();
var enc = System.Text.Encoding.GetEncoding(437);
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
var lines = File.ReadAllLines(@"SIE4 Exempelfil.SE", enc);

Crc32 crc32 = new Crc32();


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
}