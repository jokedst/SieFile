using System.Text;

namespace SieFileTests;

public static class TestExtentions
{
    /// <summary>
    /// Converts string to stream with encoding CodePage 437
    /// </summary>
    public static MemoryStream To437Stream(this string data)
    {
        var stream = new MemoryStream();
        var writer = new StreamWriter(stream, Encoding.GetEncoding(437));
        writer.Write(data);
        writer.Flush();
        stream.Position = 0;
        return stream;
    }
}