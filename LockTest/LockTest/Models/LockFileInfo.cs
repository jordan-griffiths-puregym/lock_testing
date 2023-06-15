public class LockFileInfo
{
    public LockFileInfo(string fileName, DateTime timestamp)
    {
        FileName = fileName;
        Timestamp = timestamp;
    }

    public string FileName { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
