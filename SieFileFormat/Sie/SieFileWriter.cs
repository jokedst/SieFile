using System.Text;

public class SieFileWriter
{
    public void Write(Stream stream, SieFile sie)
    {
        // Ensure codepage 437 is loaded
        if (!Encoding.GetEncodings().Any(x => x.CodePage == 437))
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }
        var encoding = Encoding.GetEncoding(437);

        using StreamWriter sw = new StreamWriter(stream, encoding);

        sw.WriteLine("#FLAGGA " + (sie.AlreadyImportedFlag ? '1' : '0'));
        sw.WriteLine("#FORMAT PC8");
    }
}
