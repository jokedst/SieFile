namespace SieFileFormat.Sie;

/// <summary>
/// Performs 32-bit reversed cyclic redundancy checks, using the polynomial for SIE files.
/// </summary>
public class SieCrc32
{
    /// <summary>
    /// Generator polynomial (modulo 2) for the reversed CRC32 algorithm. 
    /// </summary>
    private const uint s_generator = 0xEDB88320;

    /// <summary>
    /// Contains a cache of calculated checksum chunks.
    /// </summary>
    private static readonly uint[] s_checksumTable;

    private uint _checksum;

    /// <summary>
    /// Generates a static lookup table which is used to optimize the checksum.
    /// </summary>
    static SieCrc32()
    {
        s_checksumTable = Enumerable.Range(0, 256).Select(i =>
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
    /// Creates and initializes a new instance of the Crc32 class.
    /// </summary>
    public SieCrc32() { 
        _checksum = 0xFFFFFFFF;
    }

    /// <summary>
    /// Current checksum.
    /// </summary>
    public uint Checksum => ~_checksum;

    /// <summary>
    /// Add bytes to be included in the checksum.
    /// </summary>
    public void AddBytes(IEnumerable<byte> bytes)
    {
        foreach (var b in bytes)
        {
            _checksum = (_checksum >> 8) ^ s_checksumTable[(_checksum ^ b) & 0xff];
        }
    }
}