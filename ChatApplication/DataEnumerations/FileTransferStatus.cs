namespace ChatApplication
{
    [System.Flags]
    public enum FileTransferStatus : byte
    {
        Runnning,
        Paused,
        Finished,
        Cancelled,
        Error,
    }
}