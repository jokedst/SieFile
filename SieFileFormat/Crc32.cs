
/// <summary>
/// Performs 32-bit reversed cyclic redundancy checks.
/// </summary>
public class Crc32
{
    /// <summary>
    /// Generator polynomial (modulo 2) for the reversed CRC32 algorithm. 
    /// </summary>
    private const UInt32 s_generator = 0xEDB88320;

    /// <summary>
    /// Creates a new instance of the Crc32 class.
    /// </summary>
    public Crc32()
    {
        // Constructs the checksum lookup table. Used to optimize the checksum.
        m_checksumTable = Enumerable.Range(0, 256).Select(i =>
        {
            var tableEntry = (uint)i;
            for (var j = 0; j < 8; ++j)
            {
                tableEntry = ((tableEntry & 1) != 0)
                    ? (s_generator ^ (tableEntry >> 1))
                    : (tableEntry >> 1);
            }
            return tableEntry;
        }).ToArray();
    }

    /// <summary>
    /// Calculates the checksum of the byte stream.
    /// </summary>
    /// <param name="byteStream">The byte stream to calculate the checksum for.</param>
    /// <returns>A 32-bit reversed checksum.</returns>
    public UInt32 Get<T>(IEnumerable<T> byteStream)
    {
        try
        {
            // Initialize checksumRegister to 0xFFFFFFFF and calculate the checksum.
            return ~byteStream.Aggregate(0xFFFFFFFF, (checksumRegister, currentByte) =>
                      (m_checksumTable[(checksumRegister & 0xFF) ^ Convert.ToByte(currentByte)] ^ (checksumRegister >> 8)));
        }
        catch (FormatException e)
        {
            throw new Exception("Could not read the stream out as bytes.", e);
        }
        catch (InvalidCastException e)
        {
            throw new Exception("Could not read the stream out as bytes.", e);
        }
        catch (OverflowException e)
        {
            throw new Exception("Could not read the stream out as bytes.", e);
        }
    }

    /// <summary>
    /// Contains a cache of calculated checksum chunks.
    /// </summary>
    private readonly UInt32[] m_checksumTable;
}

public class CRC32
{
    private readonly uint[] ChecksumTable;
    private readonly uint Polynomial = 0xEDB88320;

    public CRC32()
    {
        ChecksumTable = new uint[0x100];

        for (uint index = 0; index < 0x100; ++index)
        {
            uint item = index;
            for (int bit = 0; bit < 8; ++bit)
                item = ((item & 1) != 0) ? (Polynomial ^ (item >> 1)) : (item >> 1);
            ChecksumTable[index] = item;
        }
    }

    public byte[] ComputeHash(Stream stream)
    {
        uint result = 0xFFFFFFFF;

        int current;
        while ((current = stream.ReadByte()) != -1)
            result = ChecksumTable[(result & 0xFF) ^ (byte)current] ^ (result >> 8);

        byte[] hash = BitConverter.GetBytes(~result);
        Array.Reverse(hash);
        return hash;
    }

    public byte[] ComputeHash(byte[] data)
    {
        using (MemoryStream stream = new MemoryStream(data))
            return ComputeHash(stream);
    }
}