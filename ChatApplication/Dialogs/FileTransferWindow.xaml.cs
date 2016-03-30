using System.Windows;
using System.Linq;

namespace ChatApplication.Dialogs
{
    /// <summary>
    /// Interaction logic for DownloadsWindow.xaml
    /// </summary>

    public partial class FileTransferWindow : Window
    {
        private System.Windows.Controls.GridViewColumnHeader listViewSortCol;
        private Graphics.Adorners.SortAdorner listViewSortAdorner = null;

        public double overallProgress {
            get { return (double)Resources["overallProgress"]; }
            set { Resources["overallProgress"] = value; }
        }

        public FileTransferWindow()
        {           
            InitializeComponent();

            transfersListView.ItemsSource = Network.FileTransfer.RunningTransfers;
            UpdateOverallProgress(this, new System.ComponentModel.ListChangedEventArgs(System.ComponentModel.ListChangedType.ItemDeleted, 0));

            Network.FileTransfer.RunningTransfers.ListChanged += UpdateOverallProgress;
        }

        private void ClearFinishedTransfersClicked(object sender, RoutedEventArgs e)
        {
            System.Collections.Generic.List<DataContainers.FileTransferContainer> removeList = new System.Collections.Generic.List<DataContainers.FileTransferContainer>();
            lock (Network.FileTransfer.RunningTransfers) {
                foreach (DataContainers.FileTransferContainer container in Network.FileTransfer.RunningTransfers) {
                    if (container.status == FileTransferStatus.Finished || container.status == FileTransferStatus.Error || container.status == FileTransferStatus.Cancelled) {
                        removeList.Add(container);
                    }
                }
                foreach(DataContainers.FileTransferContainer container in removeList) {
                    Network.FileTransfer.RunningTransfers.Remove(container);
                }
                removeList.Clear();
            }
        }

        public void UpdateOverallProgress(object sender, System.ComponentModel.ListChangedEventArgs e)
        {
            ulong totalSizeToBeTransferred = 0, totalTransferred = 0;
            lock (Network.FileTransfer.RunningTransfers) {
                foreach (DataContainers.FileTransferContainer transferContainer in Network.FileTransfer.RunningTransfers) {
                    if ((transferContainer.status & (FileTransferStatus.Finished | FileTransferStatus.Paused | FileTransferStatus.Running)) == transferContainer.status) {
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

        private void FileShareWindowClosed(object sender, System.EventArgs e)
        {
            Network.FileTransfer.RunningTransfers.ListChanged -= UpdateOverallProgress;
        }

        private void RemoveItemClicked(object sender, RoutedEventArgs e)
        {
            lock (Network.FileTransfer.RunningTransfers) {
                Network.FileTransfer.RunningTransfers.Remove((DataContainers.FileTransferContainer)transfersListView.SelectedItem);
            }
        }

        private void PauseItemClicked(object sender, RoutedEventArgs e)
        {
            if (((System.Windows.Controls.MenuItem)sender).Parent is System.Windows.Controls.Menu) {
                lock (Network.FileTransfer.RunningTransfers) {
                    foreach (DataContainers.FileTransferContainer _fileTransferContainersObject in Network.FileTransfer.RunningTransfers) {

                        if(_fileTransferContainersObject.status == FileTransferStatus.Cancelled || _fileTransferContainersObject.status == FileTransferStatus.Error || _fileTransferContainersObject.status == FileTransferStatus.Finished) {
                            continue;
                        }

                        if (_fileTransferContainersObject.transferType == FileTransferType.Upload) {
                            _fileTransferContainersObject.status = FileTransferStatus.Paused;
                        }
                        if (_fileTransferContainersObject.pausedBy == PausedBy.None) {
                            _fileTransferContainersObject.pausedBy = PausedBy.User;
                        }
                        else if (_fileTransferContainersObject.pausedBy == PausedBy.OtherPeer) {
                            _fileTransferContainersObject.pausedBy = PausedBy.Both;
                        }


                        System.Threading.ThreadPool.QueueUserWorkItem(state => {
                            lock (_fileTransferContainersObject.FileTransferClassInstance.controlSocket) {
                                try {
                                    Network.NetworkCommunicationManagers.SendByteArrayOverSocket(_fileTransferContainersObject.FileTransferClassInstance.controlSocket, new byte[] { (byte)FileTransferCommands.Pause });
                                }
                                catch (System.ObjectDisposedException) {
                                    Network.NetworkCommunicationManagers.Disconnect(_fileTransferContainersObject.FileTransferClassInstance.controlSocket);
                                    Network.NetworkCommunicationManagers.Disconnect(_fileTransferContainersObject.FileTransferClassInstance.dataSocket);
                                }
                                catch (System.Net.Sockets.SocketException) {
                                    Network.NetworkCommunicationManagers.Disconnect(_fileTransferContainersObject.FileTransferClassInstance.controlSocket);
                                    Network.NetworkCommunicationManagers.Disconnect(_fileTransferContainersObject.FileTransferClassInstance.dataSocket);
                                }
                            }
                        });
                    }
                }
            }
            else {
                DataContainers.FileTransferContainer _fileTransferContainerObject = (DataContainers.FileTransferContainer)transfersListView.SelectedItem;

                lock (Network.FileTransfer.RunningTransfers) {
                    if (_fileTransferContainerObject.transferType == FileTransferType.Upload) {
                        _fileTransferContainerObject.status = FileTransferStatus.Paused;
                    }
                    if (_fileTransferContainerObject.pausedBy == PausedBy.None) {
                        _fileTransferContainerObject.pausedBy = PausedBy.User;
                    }
                    else if (_fileTransferContainerObject.pausedBy == PausedBy.OtherPeer) {
                        _fileTransferContainerObject.pausedBy = PausedBy.Both;
                    }
                }


                System.Threading.ThreadPool.QueueUserWorkItem(state => {
                    lock (_fileTransferContainerObject.FileTransferClassInstance.controlSocket) {
                        try {
                            Network.NetworkCommunicationManagers.SendByteArrayOverSocket(_fileTransferContainerObject.FileTransferClassInstance.controlSocket, new byte[] { (byte)FileTransferCommands.Pause });
                        }
                        catch (System.ObjectDisposedException) {
                            Network.NetworkCommunicationManagers.Disconnect(_fileTransferContainerObject.FileTransferClassInstance.controlSocket);
                            Network.NetworkCommunicationManagers.Disconnect(_fileTransferContainerObject.FileTransferClassInstance.dataSocket);
                        }
                        catch (System.Net.Sockets.SocketException) {
                            Network.NetworkCommunicationManagers.Disconnect(_fileTransferContainerObject.FileTransferClassInstance.controlSocket);
                            Network.NetworkCommunicationManagers.Disconnect(_fileTransferContainerObject.FileTransferClassInstance.dataSocket);
                        }                        
                    }
                });
            }            
        }
        
        private void ResumeItemClicked(object sender, RoutedEventArgs e)
        {
            if (((System.Windows.Controls.MenuItem)sender).Parent is System.Windows.Controls.Menu) {
                lock (Network.FileTransfer.RunningTransfers) {
                    foreach (DataContainers.FileTransferContainer _fileTransferContainersObject in Network.FileTransfer.RunningTransfers) {

                        if (_fileTransferContainersObject.status == FileTransferStatus.Cancelled || _fileTransferContainersObject.status == FileTransferStatus.Error || _fileTransferContainersObject.status == FileTransferStatus.Finished) {
                            continue;
                        }

                        if (_fileTransferContainersObject.pausedBy == PausedBy.Both) {
                            _fileTransferContainersObject.pausedBy = PausedBy.OtherPeer;
                        }
                        else if (_fileTransferContainersObject.pausedBy == PausedBy.User) {
                            _fileTransferContainersObject.pausedBy = PausedBy.None;
                        }


                        if (_fileTransferContainersObject.transferType == FileTransferType.Download && _fileTransferContainersObject.pausedBy == PausedBy.None) {
                            lock (Network.FileTransfer.RunningTransfers) {
                                _fileTransferContainersObject.status = FileTransferStatus.Running;
                            }
                            _fileTransferContainersObject.FileTransferClassInstance.resetEvent.Set();
                        }

                        System.Threading.ThreadPool.QueueUserWorkItem(state => {
                            lock (_fileTransferContainersObject.FileTransferClassInstance.controlSocket) {
                                try {
                                    Network.NetworkCommunicationManagers.SendByteArrayOverSocket(_fileTransferContainersObject.FileTransferClassInstance.controlSocket, new byte[] { (byte)FileTransferCommands.Resume });
                                }
                                catch (System.ObjectDisposedException) {
                                    Network.NetworkCommunicationManagers.Disconnect(_fileTransferContainersObject.FileTransferClassInstance.controlSocket);
                                    Network.NetworkCommunicationManagers.Disconnect(_fileTransferContainersObject.FileTransferClassInstance.dataSocket);
                                }
                                catch (System.Net.Sockets.SocketException) {
                                    Network.NetworkCommunicationManagers.Disconnect(_fileTransferContainersObject.FileTransferClassInstance.controlSocket);
                                    Network.NetworkCommunicationManagers.Disconnect(_fileTransferContainersObject.FileTransferClassInstance.dataSocket);
                                }
                            }
                        });
                    }
                }
            }
            else {
                DataContainers.FileTransferContainer _fileTransferContainerObject = (DataContainers.FileTransferContainer)transfersListView.SelectedItem;

                lock (Network.FileTransfer.RunningTransfers) {
                    if (_fileTransferContainerObject.pausedBy == PausedBy.Both) {
                        _fileTransferContainerObject.pausedBy = PausedBy.OtherPeer;
                    }
                    else if (_fileTransferContainerObject.pausedBy == PausedBy.User) {
                        _fileTransferContainerObject.pausedBy = PausedBy.None;
                    }
                }

                if (_fileTransferContainerObject.transferType == FileTransferType.Download && _fileTransferContainerObject.pausedBy == PausedBy.None) {
                    lock (Network.FileTransfer.RunningTransfers) {
                        try {
                            _fileTransferContainerObject.status = FileTransferStatus.Running;
                        }
                        catch (System.ObjectDisposedException) {
                            Network.NetworkCommunicationManagers.Disconnect(_fileTransferContainerObject.FileTransferClassInstance.controlSocket);
                            Network.NetworkCommunicationManagers.Disconnect(_fileTransferContainerObject.FileTransferClassInstance.dataSocket);
                        }
                        catch (System.Net.Sockets.SocketException) {
                            Network.NetworkCommunicationManagers.Disconnect(_fileTransferContainerObject.FileTransferClassInstance.controlSocket);
                            Network.NetworkCommunicationManagers.Disconnect(_fileTransferContainerObject.FileTransferClassInstance.dataSocket);
                        }
                    }
                    _fileTransferContainerObject.FileTransferClassInstance.resetEvent.Set();
                }

                System.Threading.ThreadPool.QueueUserWorkItem(state => {
                    lock (_fileTransferContainerObject.FileTransferClassInstance.controlSocket) {
                        Network.NetworkCommunicationManagers.SendByteArrayOverSocket(_fileTransferContainerObject.FileTransferClassInstance.controlSocket, new byte[] { (byte)FileTransferCommands.Resume });
                    }
                });
            }            
        }

        private void StopItemClicked(object sender, RoutedEventArgs e)
        {
            DataContainers.FileTransferContainer _fileTransferContainerObject = (DataContainers.FileTransferContainer)transfersListView.SelectedItem;

            lock (Network.FileTransfer.RunningTransfers) {
                if(_fileTransferContainerObject.status != FileTransferStatus.Finished) {
                    _fileTransferContainerObject.status = FileTransferStatus.Cancelled;
                }                
            }

            System.Threading.ThreadPool.QueueUserWorkItem(state => {
                lock (_fileTransferContainerObject.FileTransferClassInstance.controlSocket) {
                    try {
                        Network.NetworkCommunicationManagers.SendByteArrayOverSocket(_fileTransferContainerObject.FileTransferClassInstance.controlSocket, new byte[] { (byte)FileTransferCommands.EndTransfer });
                    }
                    catch (System.ObjectDisposedException) {}
                    catch (System.Net.Sockets.SocketException) {}
                }
                System.Threading.Thread.Sleep(500);
                Network.NetworkCommunicationManagers.Disconnect(_fileTransferContainerObject.FileTransferClassInstance.controlSocket);
                Network.NetworkCommunicationManagers.Disconnect(_fileTransferContainerObject.FileTransferClassInstance.dataSocket);
            });            
        }

        private void transfersListViewHeaderClicked(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.GridViewColumnHeader column = (sender as System.Windows.Controls.GridViewColumnHeader);
            
            string _sortBy = column.Tag.ToString();
            if (listViewSortCol != null) {
                System.Windows.Documents.AdornerLayer.GetAdornerLayer(listViewSortCol).Remove(listViewSortAdorner);
                transfersListView.Items.SortDescriptions.Clear();
            }

            System.ComponentModel.ListSortDirection newDir = System.ComponentModel.ListSortDirection.Ascending;
            if (listViewSortCol == column && listViewSortAdorner.Direction == newDir) {
                newDir = System.ComponentModel.ListSortDirection.Descending;
            }               

            listViewSortCol = column;

            System.Collections.Generic.List<DataContainers.FileTransferContainer> _tempList;

            lock (Network.FileTransfer.RunningTransfers) {
                _tempList = Network.FileTransfer.RunningTransfers.ToList();
                switch (_sortBy) {
                    case "status":
                        if (newDir == System.ComponentModel.ListSortDirection.Ascending) {
                            _tempList.Sort(Comparers.FileTransferContainerCompareStatus.CompareAscending);
                        }
                        else {
                            _tempList.Sort(Comparers.FileTransferContainerCompareStatus.CompareDescending);
                        }
                        break;
                    case "ID":
                        if (newDir == System.ComponentModel.ListSortDirection.Ascending) {
                            _tempList.Sort(Comparers.FileTransferContainerCompareID.CompareAscending);
                        }
                        else {
                            _tempList.Sort(Comparers.FileTransferContainerCompareID.CompareDescending);
                        }
                        break;
                    case "FileName":
                        if (newDir == System.ComponentModel.ListSortDirection.Ascending) {
                            _tempList.Sort(Comparers.FileTransferContainerCompareFileName.CompareAscending);
                        }
                        else {
                            _tempList.Sort(Comparers.FileTransferContainerCompareFileName.CompareDescending);
                        }
                        break;
                    case "FileSize":
                        if (newDir == System.ComponentModel.ListSortDirection.Ascending) {
                            _tempList.Sort(Comparers.FileTransferContainerCompareSize.CompareAscending);
                        }
                        else {
                            _tempList.Sort(Comparers.FileTransferContainerCompareSize.CompareDescending);
                        }
                        break;
                    case "Progress":
                        if (newDir == System.ComponentModel.ListSortDirection.Ascending) {
                            _tempList.Sort(Comparers.FileTransferContainerCompareProgress.CompareAscending);
                        }
                        else {
                            _tempList.Sort(Comparers.FileTransferContainerCompareProgress.CompareDescending);
                        }
                        break;
                }

                Network.FileTransfer.RunningTransfers.Clear();
                foreach (DataContainers.FileTransferContainer _container in _tempList) {
                    Network.FileTransfer.RunningTransfers.Add(_container);
                }
            }            

            listViewSortAdorner = new Graphics.Adorners.SortAdorner(listViewSortCol, newDir);
            System.Windows.Documents.AdornerLayer.GetAdornerLayer(listViewSortCol).Add(listViewSortAdorner);
        }

        private void OpenFileClicked(object sender, RoutedEventArgs e)
        {
            DataContainers.FileTransferContainer _fileTransferContainerObject = (DataContainers.FileTransferContainer)transfersListView.SelectedItem;

            if (_fileTransferContainerObject.status != FileTransferStatus.Finished || _fileTransferContainerObject.transferType != FileTransferType.Download) {
                return;
            }

            if (!System.IO.File.Exists(_fileTransferContainerObject.filePath)) {
                MessageBox.Show("File has been deleted or moved");
                return;
            }

            if (Network.FileTransfer.IsFileExtensionDangerous(System.IO.Path.GetExtension(_fileTransferContainerObject.fileName))) {
                if (MessageBox.Show("This File Maybe Dangerous, Proceed to open file ?", "Open Possible Malicious File", MessageBoxButton.OKCancel) == MessageBoxResult.Cancel) {
                    return;
                }
            }

            System.Diagnostics.Process.Start(_fileTransferContainerObject.filePath);
        }

        private void DeleteFileClicked(object sender, RoutedEventArgs e)
        {
            DataContainers.FileTransferContainer _fileTransferContainerObject = (DataContainers.FileTransferContainer)transfersListView.SelectedItem;

            if (_fileTransferContainerObject.status != FileTransferStatus.Finished || _fileTransferContainerObject.transferType != FileTransferType.Download) {
                return;
            }

            if (!System.IO.File.Exists(_fileTransferContainerObject.filePath)) {
                MessageBox.Show("File has been already deleted or moved");
                return;
            }

            try {
                System.IO.File.Delete(_fileTransferContainerObject.filePath);
            }
            catch { }

            lock (Network.FileTransfer.RunningTransfers) {
                Network.FileTransfer.RunningTransfers.Remove(_fileTransferContainerObject);
            }
        }

        private void OpenFinishedItemDoubleClicked(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            OpenFileClicked(sender, e);
        }
    }
}
