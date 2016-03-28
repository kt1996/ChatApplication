namespace ChatApplication
{
    [System.Flags]
    public enum FileTransferStatus : byte
    {
        Running,
        Paused,
        Finished,
        Cancelled,
        Error,
    }
}