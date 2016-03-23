using System.Reflection;
using System.Windows;

namespace ChatApplication.Dialogs
{
    /// <summary>
    /// Interaction logic for DownloadsWindow.xaml
    /// </summary>
   
    public partial class FileTransferWindow : Window
    {
        System.ComponentModel.BindingList<DataContainers.FileTransferContainer> runningTransfers;
        
        public double overallProgress {
            get { return (double)Resources["overallProgress"]; }
            set { Resources["overallProgress"] = value; }
        }

        public FileTransferWindow(System.ComponentModel.BindingList<DataContainers.FileTransferContainer> runningTransfers)
        {           
            InitializeComponent();

            downloadsListView.ItemsSource = runningTransfers;
            this.runningTransfers = runningTransfers;
            UpdateOverallProgress(this, new System.ComponentModel.ListChangedEventArgs(System.ComponentModel.ListChangedType.ItemDeleted, 0));

            runningTransfers.ListChanged += UpdateOverallProgress;  
        }

        private void ClearFinishedTransfersClicked(object sender, RoutedEventArgs e)
        {
            System.Collections.Generic.List<DataContainers.FileTransferContainer> removeList = new System.Collections.Generic.List<DataContainers.FileTransferContainer>();
            lock (runningTransfers) {
                foreach (DataContainers.FileTransferContainer container in runningTransfers) {
                    if (container.progress == 100) {
                        removeList.Add(container);
                    }
                }
                foreach(DataContainers.FileTransferContainer container in removeList) {
                    runningTransfers.Remove(container);
                }
                removeList.Clear();
            }
            downloadsListView.Items.Refresh();
        }

        public void UpdateOverallProgress(object sender, System.ComponentModel.ListChangedEventArgs e)
        {
            ulong totalSizeToBeTransferred = 0, totalTransferred = 0;
            lock (runningTransfers) {
                foreach (DataContainers.FileTransferContainer transferContainer in runningTransfers) {
                    if ((transferContainer.status & (FileTransferStatus.Finished | FileTransferStatus.Paused | FileTransferStatus.Runnning)) == transferContainer.status) {
                        totalSizeToBeTransferred += (ulong)transferContainer.sizeInBytes;
                        totalTransferred += (ulong)((transferContainer.sizeInBytes * transferContainer.progress)/ 100);
                    }                    
                }
            }
            if(totalSizeToBeTransferred == 0) {
                Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, (System.Action)(() => {
                    overallProgress = 0;
                }));
            }
            else {
                Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, (System.Action)(() => {
                    overallProgress = System.Math.Round((((double)totalTransferred * 100) / totalSizeToBeTransferred), 1);
                }));
            }
        }

    }
}
