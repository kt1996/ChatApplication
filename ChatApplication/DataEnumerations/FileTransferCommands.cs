namespace ChatApplication
{
    public enum FileTransferCommands : byte
    {
        BlockTransferred,
        Pause,
        Resume,
        EndTransfer,
        PauseOrResumeRequestReceived,
    }
}