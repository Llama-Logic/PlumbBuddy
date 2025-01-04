namespace PlumbBuddy.Data.Chronicle;

[SuppressMessage("Design", "CA1028: Enum Storage should be Int32")]
public enum SavePackageResourceCompressionType :
    byte
{
    None = 0,
    Deleted = 1,
    Internal = 2,
    Streamable = 3,
    ZLIB = 4
}
