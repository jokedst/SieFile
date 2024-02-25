using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;



Console.WriteLine("Hello, World!");

var sieReader = new SieFileReader();
sieReader.Read(File.OpenRead(@"SIE4 Exempelfil.SE"), ".SE");


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