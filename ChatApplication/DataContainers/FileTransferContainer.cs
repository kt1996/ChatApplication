namespace ChatApplication.DataContainers
{
    /*-----------------------------------------------------------------------------------------------------
    Description
    -----------------------------------------------------------------------------------------------------*/

    public class FileTransferContainer : System.ComponentModel.INotifyPropertyChanged
    {

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            System.ComponentModel.PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }
        protected bool SetField<T>(ref T field, T value, string propertyName)
        {
            if (System.Collections.Generic.EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        private string IDvalue;
        public string ID {
            get { return IDvalue; }
            set { SetField(ref IDvalue, value, "ID"); }
        }

        private string fileNameValue;
        public string fileName {
            get {return fileNameValue; }
            set { SetField(ref fileNameValue, value, "fileName"); }
        }

        private string sizeValue;
        public string size {
            get { return sizeValue; }
            set { SetField(ref sizeValue, value, "size"); }
        }

        private long sizeInBytesValue;
        public long sizeInBytes
        {
            get { return sizeInBytesValue; }
            set { SetField(ref sizeInBytesValue, value, "sizeInBytes"); }
        }

        private FileTransferType transferTypeValue;
        public FileTransferType transferType {
            get { return transferTypeValue; }
            set { SetField(ref transferTypeValue, value, "transferType"); }
        }

        private float progressValue;
        public float progress {
            get { return progressValue; }
            set { SetField(ref progressValue, value, "progress"); }
        }

        public FileTransferStatus statusValue;
        public FileTransferStatus status {
            get { return statusValue; }
            set { SetField(ref statusValue, value, "status"); }
        }

        private PausedBy pausedByValue;
        public PausedBy pausedBy
        {
            get { return pausedByValue; }
            set { SetField(ref pausedByValue, value, "pausedBy"); }
        }

        public Network.FileTransfer FileTransferClassInstance;
    }
}
