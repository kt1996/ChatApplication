﻿using ChatApplication.DataContainers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace ChatApplication
{
    public partial class MainWindow: Window
    {
        private bool AddPeer(PeerDataContainer client)
        {
            //Check if already in List
            foreach (PeerDataContainer _peer in broadcastingPeersList) {
                if (_peer.IP == client.IP && _peer.nick == client.nick) {
                    broadcastingPeersList[broadcastingPeersList.IndexOf(_peer)].time = DateTime.Now;
                    Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => { BroadcastingList.ItemsSource = broadcastingPeersList; }));
                    return false;
                }
            }

            broadcastingPeersList.Add(client);
            if (listViewSortAdorner == null) {
                //Do nothing items will be sorted by themself later when content has been rendered
            }
            else if (listViewSortAdorner.Direction == System.ComponentModel.ListSortDirection.Ascending) {
                broadcastingPeersList.Sort(Comparers.PeerContainerCompare.CompareAscending);
            }
            else {
                broadcastingPeersList.Sort(Comparers.PeerContainerCompare.CompareDescending);
            }

            Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => { BroadcastingList.ItemsSource = broadcastingPeersList; BroadcastingList.Items.Refresh(); }));

            return true;
        }

        private void AddNewTab(string nick, string ip)
        {
            //Check if the tab is previously open
            foreach (TabItem _t in TabControl.Items) {
                if (ip == ((string)_t.Tag).Remove(0, ((string)_t.Tag).IndexOf(':') + 1) && nick == ((string)_t.Tag).Remove(((string)_t.Tag).IndexOf(':'))) {
                    return;
                }
            }

            Image _closeButtonImage = new Image();
            StackPanel _st = new StackPanel();
            StackPanel _st2 = new StackPanel();
            Button _btn = new Button();
            Button _btn2 = new Button();
            Button _btn3 = new Button();
            Button _btn4 = new Button();
            TabItem _tab = new TabItem();
            Grid _grd = new Grid();
            TextBlock _txt = new TextBlock();
            TextBox _txt2 = new TextBox();
            ListBox _ltbox = new ListBox();
            Binding _bndng = new Binding();

            //Tab Item Header
            _btn.Padding = new Thickness(0.5);
            _btn.Height = 17;
            _btn.Width = 17;
            _btn.BorderThickness = new Thickness(0);
            _btn.GotMouseCapture += new MouseEventHandler(MouseDownOnTabCloseButton);
            _btn.LostMouseCapture += new MouseEventHandler(MouseUpOnTabCloseButton);
            _btn.Focusable = false;
            _btn.Margin = new Thickness(7, 0, 0, 0);
            _st.VerticalAlignment = VerticalAlignment.Center;
            _st.HorizontalAlignment = HorizontalAlignment.Center;
            _closeButtonImage.Source = new BitmapImage(new Uri(@"/Resources/Images/NormalCloseIcon.png", UriKind.Relative));
            _closeButtonImage.Height = 13;
            _closeButtonImage.Width = 13;
            _st.Children.Add(_closeButtonImage);
            _btn.Content = _st;
            _st2.Orientation = Orientation.Horizontal;
            _txt.VerticalAlignment = VerticalAlignment.Center;

            if (nick.Length > 12) {
                _txt.Text = nick.Substring(0, 9) + "...";
            }
            else {
                _txt.Text = nick;
            }

            _st2.Children.Add(_txt);
            _st2.Children.Add(_btn);

            _tab.Header = _st2;

            //Content
            RowDefinition rdefinition = new RowDefinition();
            RowDefinition rdefinition2 = new RowDefinition();
            rdefinition.Height = new GridLength(451, GridUnitType.Star);
            _grd.RowDefinitions.Add(rdefinition);
            rdefinition2.Height = new GridLength(25, GridUnitType.Pixel);
            _grd.RowDefinitions.Add(rdefinition2);
            _ltbox.SetValue(Grid.RowProperty, 0);
            _bndng.Path = new PropertyPath("ActualWidth");
            _bndng.ElementName = "MainContentGrid";
            _bndng.ConverterParameter = "0.7142857-228.61272-86";
            _bndng.Converter = new Converters.MessageboxWidthConverter();
            _txt2.SetBinding(TextBox.WidthProperty, _bndng);
            _txt2.HorizontalAlignment = HorizontalAlignment.Left;
            _txt2.Height = 22;
            _txt2.SetValue(Grid.RowProperty, 1);
            _txt2.TextWrapping = TextWrapping.Wrap;
            _txt2.VerticalAlignment = VerticalAlignment.Center;
            _btn2.Content = "Send";
            _btn2.IsDefault = true;
            _btn2.Tag = nick + ":" + ip;
            _btn2.HorizontalAlignment = HorizontalAlignment.Right;
            _btn2.SetValue(Grid.RowProperty, 1);
            _btn2.VerticalAlignment = VerticalAlignment.Center;
            _btn2.Width = 40;
            _btn2.Margin = new Thickness(0, 2, 0, 0);
            _btn2.Click += new RoutedEventHandler(SendMessageButtonClicked);
            _btn3.Content = "CL";
            _btn3.Tag = nick + ":" + ip;
            _btn3.HorizontalAlignment = HorizontalAlignment.Right;
            _btn3.SetValue(Grid.RowProperty, 1);
            _btn3.VerticalAlignment = VerticalAlignment.Center;
            _btn3.Width = 20;
            _btn3.Margin = new Thickness(0, 2, 43, 0);
            _btn3.Click += new RoutedEventHandler(CallButtonClicked);
            _btn4.Content = "FL";
            _btn4.Tag = nick + ":" + ip;
            _btn4.HorizontalAlignment = HorizontalAlignment.Right;
            _btn4.SetValue(Grid.RowProperty, 1);
            _btn4.VerticalAlignment = VerticalAlignment.Center;
            _btn4.Width = 20;
            _btn4.Margin = new Thickness(0, 2, 66, 0);
            _btn4.Click += new RoutedEventHandler(SendFileButtonClicked);

            _grd.Children.Add(_ltbox);
            _grd.Children.Add(_txt2);
            _grd.Children.Add(_btn2);
            _grd.Children.Add(_btn3);
            _grd.Children.Add(_btn4);

            _tab.Content = _grd;

            _tab.Tag = nick + ":" + ip;
            //Finally add it
            TabControl.Items.Add(_tab);

            //If it is the only tab change focus to it
            if (TabControl.Items.Count == 1) {
                TabControl.SelectedIndex = 0;
            }
        }

        private void CallButtonClicked(object sender, RoutedEventArgs e)
        {
            Button _btn = ((Button)sender);
            string _buttonTag = (string)_btn.Tag;

            string _ip = _buttonTag.Remove(0, _buttonTag.IndexOf(':') + 1);
            string _nick = _buttonTag.Remove(_buttonTag.IndexOf(':'));
            ConnectedPeerDataContainer _peer = new ConnectedPeerDataContainer();
            _peer.nick = "";
            _peer.socket = null;
            foreach (ConnectedPeerDataContainer _connectedPeer in connectedPeersList) {
                string _connectedPeerSocketString = _connectedPeer.socket.RemoteEndPoint.ToString();
                if (_connectedPeerSocketString.Remove(_connectedPeerSocketString.LastIndexOf(':')) == _ip) {
                    _peer = _connectedPeer;
                    break;
                }
            }

            if (_peer.socket == null) {
                WriteToTab(_ip, "Client not available", nick, 0);
                return;
            }

            MessageBox.Show("Calling Feature not implemented as yet, Please check back later :-D");
        }
        
        private void ConnectFromBroadcastList(object sender, RoutedEventArgs e)
        {
            string _tag = (string)((MenuItem)sender).Tag;
            string _ip = _tag.Remove(0, _tag.IndexOf(':') + 1);
            new Thread(() => ConnectToPeerByIP(_ip))
            {
                Name = _ip + " handler",
                IsBackground = true
            }.Start();
        }

        private void EditPasswordClicked(object sender, RoutedEventArgs e)
        {
            Dialogs.EditPasswordWindow _passwordWindow = new Dialogs.EditPasswordWindow(password != null);
            _passwordWindow.ShowInTaskbar = false;
            _passwordWindow.Owner = this;
            if ((bool)_passwordWindow.ShowDialog()) {
                if (password != _passwordWindow.Password) {
                    password = _passwordWindow.Password;
                    if (password == null) {
                        WriteToLogbox("Password Removed");
                    }
                    else {
                        WriteToLogbox("Password Successfully changed");
                    }
                }
            }
        }

        private void HideLogbox(object sender, MouseButtonEventArgs e)
        {
            if (isLogWindowOpen) {
                System.Windows.Media.Animation.Storyboard _sb = Resources["HideOverlayRectangleForLog"] as System.Windows.Media.Animation.Storyboard;
                _sb.Begin(MainWindowOverlayRectangleForLog);
                _sb = Resources["HideLog"] as System.Windows.Media.Animation.Storyboard;
                _sb.Begin(LogCanvas);
                Panel.SetZIndex(MainWindowOverlayRectangleForLog, -1);

                isLogWindowOpen = false;
            }
        }

        private void LoseSearchAreaFocusRectMouseUp(object sender, MouseButtonEventArgs e)
        {
            Panel.SetZIndex(MainWindowOverlayRectangleForSearch, -1);
            SearchTextbox.Text = "Search..";
            Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => { BroadcastingList.ItemsSource = broadcastingPeersList; BroadcastingList.Items.Refresh(); }));
            Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)delegate { SearchTextbox.SelectAll(); });
        }

        private void MouseDownOnTabCloseButton(object sender, MouseEventArgs e)
        {
            Button _btn = sender as Button;
            StackPanel _st = (StackPanel)_btn.Content;
            _st.Children.OfType<Image>().First().Source = new BitmapImage(new Uri(@"/Resources/Images/PressedCloseIcon.png", UriKind.Relative));
        }

        private void MouseUpOnTabCloseButton(object sender, MouseEventArgs e)
        {
            Button _btn = sender as Button;
            StackPanel _st = (StackPanel)_btn.Content;
            _st.Children.OfType<Image>().First<Image>().Source = new BitmapImage(new Uri(@"/Resources/Images/NormalCloseIcon.png", UriKind.Relative));
            if (MessageBox.Show("      Do you really want to close this window ?\n        (Currently all chat history will be lost)", "Close Connection", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) == MessageBoxResult.Yes) {
                TabItem _tab = ((TabItem)((StackPanel)_btn.Parent).Parent);
                Disconnect((string)_tab.Tag);
                Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => { TabControl.Items.Remove(_tab); }));
            }
        }

        private void SearchAreaGotFocus(object sender, RoutedEventArgs e)
        {
            Panel.SetZIndex(MainWindowOverlayRectangleForSearch, 35);
            Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)delegate { SearchTextbox.SelectAll(); });
        }

        private void SearchTextboxKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key.ToString() == "Escape") {
                SearchTextbox.Text = "Search..";
                Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => { BroadcastingList.ItemsSource = broadcastingPeersList; BroadcastingList.Items.Refresh(); }));
                Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)delegate { SearchTextbox.SelectAll(); });
                return;
            }

            List<PeerDataContainer> _clientsMatchingSearchText = new List<PeerDataContainer>();

            foreach (PeerDataContainer _peer in broadcastingPeersList) {
                if (_peer.nick.ToLower().Contains(SearchTextbox.Text.ToLower())) {
                    _clientsMatchingSearchText.Add(_peer);
                }
            }

            Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => { BroadcastingList.ItemsSource = _clientsMatchingSearchText; BroadcastingList.Items.Refresh(); }));
        }

        private void SendMessageButtonClicked(object sender, RoutedEventArgs e)
        {
            Button _btn = ((Button)sender);
            string _buttonTag = (string)_btn.Tag;
            TextBox _txtbox = ((Grid)((Button)sender).Parent).Children.OfType<TextBox>().First();
            string _message = _txtbox.Text.Trim();
            ((Grid)_btn.Parent).Children.OfType<TextBox>().First().Text = "";
            _txtbox.Dispatcher.BeginInvoke(DispatcherPriority.Input, new ThreadStart(delegate () { _txtbox.Focus(); }));
            string _ip = _buttonTag.Remove(0, _buttonTag.IndexOf(':') + 1);
            string _nick = _buttonTag.Remove(_buttonTag.IndexOf(':'));
            if (_message == "") {
                return;
            }

            ConnectedPeerDataContainer _peer = new ConnectedPeerDataContainer();
            _peer.nick = "";
            _peer.socket = null;
            foreach (ConnectedPeerDataContainer _connectedPeer in connectedPeersList) {
                string _connectedPeerSocketString = _connectedPeer.socket.RemoteEndPoint.ToString();
                if (_connectedPeerSocketString.Remove(_connectedPeerSocketString.LastIndexOf(':')) == _ip) {
                    _peer = _connectedPeer;
                    break;
                }
            }

            if (_peer.socket == null) {
                WriteToTab(_ip, "Client not available", nick, 0);
                return;
            }
            WriteToTab(_ip, _message, _peer.nick, 2);
            Thread thread = new Thread(() => SendMessage(_message, _peer));
            thread.Name = "Message Sending to " + _ip;
            thread.Start();
        }

        private void SendFileButtonClicked(object sender, RoutedEventArgs e)
        {
            Button _btn = ((Button)sender);
            string _buttonTag = (string)_btn.Tag;
            string _ip = _buttonTag.Remove(0, _buttonTag.IndexOf(':') + 1);
            string _nick = _buttonTag.Remove(_buttonTag.IndexOf(':'));

            ConnectedPeerDataContainer _peer = new ConnectedPeerDataContainer();
            _peer.nick = "";
            _peer.socket = null;
            foreach (ConnectedPeerDataContainer _connectedPeer in connectedPeersList) {
                string _connectedPeerSocketString = _connectedPeer.socket.RemoteEndPoint.ToString();
                if (_connectedPeerSocketString.Remove(_connectedPeerSocketString.LastIndexOf(':')) == _ip) {
                    _peer = _connectedPeer;
                    break;
                }
            }

            if (_peer.socket == null) {
                WriteToTab(_ip, "Client not available", nick, 0);
                return;
            }

            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

            // Display OpenFileDialog by calling ShowDialog method 
            Nullable<bool> result = dlg.ShowDialog();

            // Get the selected file name and display in a TextBox 
            if ((result.HasValue && result.Value)) {
                string _filePath = dlg.FileName;

                FileTransferContainer fileTransferContainer = new FileTransferContainer() {
                    fileName = System.IO.Path.GetFileName(_filePath),
                    ID = _peer.nick + " (" + _ip + ")",
                    progress = 0,
                    status = FileTransferStatus.Running,
                    transferType = FileTransferType.Upload,
                    pausedBy = PausedBy.None,
                };
                using (System.IO.FileStream _fs = new System.IO.FileStream(_filePath, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite)) {
                    fileTransferContainer.sizeInBytes = _fs.Length;
                    fileTransferContainer.size = Converters.DataConverter.bytesToReadableString(fileTransferContainer.sizeInBytes);
                }

                lock (RunningTransfers) {
                    RunningTransfers.Add(fileTransferContainer);
                }

                Thread _thread = new Thread(() => {
                    Network.NetworkCommunicationManagers.SendEncryptedIntOverSocket(_peer.socket, _peer.key, (int)PrimaryCommands.FileTransfer);
                    
                    int _port1 = Network.NetworkCommunicationManagers.FindNextFreeTcpPort();
                    if (!Network.NetworkCommunicationManagers.SendIntOverSocket(_peer.socket, _port1)) {
                        WriteToLogbox("Failed to send file (Handshake Failed)");
                        lock (RunningTransfers) {
                            fileTransferContainer.status = FileTransferStatus.Error;
                        }
                        return;
                    };
                    
                    new Network.FileTransfer(fileTransferContainer, _filePath, _port1);
                });
                _thread.Name = "File Transfer Handler";
                _thread.IsBackground = true;
                _thread.Start();                
            }
        }

        private void SortBroadcastingPeers(object sender, EventArgs e)
        {
            //Incase it is being called by window for first time initialization then get the nick too
            if (sender.GetType() == PrimaryWindow.GetType()) {
                while (nick == "" || nick == "Enter Nick" || (nick.IndexOf(':') != -1) || (nick.IndexOf('<') != -1) || (nick.IndexOf('>') != -1) || nick.Length > 30) {
                    Dialogs.InputNickWindow _dialog = new Dialogs.InputNickWindow();
                    _dialog.ShowInTaskbar = false;
                    _dialog.Owner = this;
                    if (_dialog.ShowDialog() == true) {
                        nick = _dialog.ResponseText.Trim();
                    }
                }

                WriteToLogbox("Starting Status Broadcasts");
                broadcastTimer = new Timer(Broadcast, null, 0, Timeout.Infinite);
            }

            GridViewColumnHeader _column = SortHeader;
            if (listViewSortCol != null) {
                System.Windows.Documents.AdornerLayer.GetAdornerLayer(listViewSortCol).Remove(listViewSortAdorner);
            }

            System.ComponentModel.ListSortDirection newDir = System.ComponentModel.ListSortDirection.Descending;
            if (listViewSortCol == _column && listViewSortAdorner.Direction == newDir)
                newDir = System.ComponentModel.ListSortDirection.Ascending;

            listViewSortCol = _column;

            if (newDir == System.ComponentModel.ListSortDirection.Ascending) {
                broadcastingPeersList.Sort(Comparers.PeerContainerCompare.CompareAscending);
            }
            else {
                broadcastingPeersList.Sort(Comparers.PeerContainerCompare.CompareDescending);
            }

            listViewSortAdorner = new Graphics.Adorners.SortAdorner(listViewSortCol, newDir);
            System.Windows.Documents.AdornerLayer.GetAdornerLayer(listViewSortCol).Add(listViewSortAdorner);

            BroadcastingList.ItemsSource = broadcastingPeersList;
            BroadcastingList.Items.Refresh();

        }

        private void TabSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //If the event was bubbled up via some other subcontrol selection change event
            if (e.Source != TabControl) {
                return;
            }
            TabControl _tabctrl = sender as TabControl;
            int index = _tabctrl.SelectedIndex;
            if (index == -1) {
                return;
            }

            TextBox _txtbox = ((Grid)((TabItem)TabControl.Items[index]).Content).Children.OfType<TextBox>().First<TextBox>();
            _txtbox.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Input, new ThreadStart(delegate () { _txtbox.Focus(); }));
        }

        private void ToggleBroadcasting(object sender, RoutedEventArgs e)
        {
            if (isBroadcasting) {
                StopBroadcasts();
            }
            else {
                if (!isServerRunning) {
                    StartServer();
                    ToggleServer.IsChecked = true;
                    isServerRunning = true;
                }
                WriteToLogbox("Starting Status Broadcasts");
                broadcastTimer = new Timer(Broadcast, null, 0, Timeout.Infinite);
            }
        }

        private void ToggleLogVisibilityButtonClicked(object sender, RoutedEventArgs e)
        {
            if (isLogWindowOpen) {
                System.Windows.Media.Animation.Storyboard _storyboard = Resources["HideOverlayRectangleForLog"] as System.Windows.Media.Animation.Storyboard;
                _storyboard.Begin(MainWindowOverlayRectangleForLog);
                _storyboard = Resources["HideLog"] as System.Windows.Media.Animation.Storyboard;
                _storyboard.Begin(LogCanvas);
                Panel.SetZIndex(MainWindowOverlayRectangleForLog, -1);

                isLogWindowOpen = false;
            }
            else {
                //Scroll the log box to bottom
                LogCanvas.Children.OfType<ListBox>().First<ListBox>().ScrollIntoView(LogCanvas.Children.OfType<ListBox>().First<ListBox>().Items[LogCanvas.Children.OfType<ListBox>().First<ListBox>().Items.Count - 1]);

                System.Windows.Media.Animation.Storyboard sb = Resources["ShowOverlayRectangleForLog"] as System.Windows.Media.Animation.Storyboard;
                sb.Begin(MainWindowOverlayRectangleForLog);
                sb = Resources["ShowLog"] as System.Windows.Media.Animation.Storyboard;
                sb.Begin(LogCanvas);

                Panel.SetZIndex(MainWindowOverlayRectangleForLog, 40);

                isLogWindowOpen = true;
            }
        }

        private void ToggleServerStatus(object sender, RoutedEventArgs e)
        {
            if (isServerRunning) {
                StopServer();
                if (isBroadcasting) {
                    ToggleBroadcast.IsChecked = false;
                    StopBroadcasts();
                    isBroadcasting = false;
                }
            }
            else {
                StartServer();
                isBroadcasting = false;
                ToggleBroadcast.IsChecked = false;
            }
        }

        private void UpdateAvailableClients(object state)
        {
            TimeSpan _lastActive;
            List<PeerDataContainer> _removeList = new List<PeerDataContainer>();
            foreach (PeerDataContainer _peer in broadcastingPeersList) {
                _lastActive = DateTime.Now.Subtract(_peer.time);
                if (TimeSpan.Compare(_lastActive, new TimeSpan(0, 0, 30)) == 1) {
                    _removeList.Add(_peer);
                }
            }
            foreach (PeerDataContainer _peer in _removeList) {
                broadcastingPeersList.Remove(_peer);
            }

            if (listViewSortAdorner == null) {
                //Do nothing items will be sorted by themself later when content has been rendered
            }
            else if (listViewSortAdorner.Direction == System.ComponentModel.ListSortDirection.Ascending) {
                broadcastingPeersList.Sort(Comparers.PeerContainerCompare.CompareAscending);
            }
            else {
                broadcastingPeersList.Sort(Comparers.PeerContainerCompare.CompareDescending);
            }
            Dispatcher.Invoke(DispatcherPriority.Background, (Action)(() => { BroadcastingList.ItemsSource = broadcastingPeersList; BroadcastingList.Items.Refresh(); }));
        }

        private void WriteToLogbox(string text)
        {
            Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => { (Log.Tag as System.Text.StringBuilder).Append("--> " + RemovePersonalData(text) + "\r\n"); }));            
            text = "** " + text;
            string _subtext;
            int _index;
            while (text.Length > 34) {
                _subtext = text.Substring(0, 34);
                _index = _subtext.LastIndexOf(' ');
                text = text.Remove(0, 34);
                if (_index == -1) {
                    Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => { Log.Items.Add(_subtext); }));
                }
                else {
                    Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => { Log.Items.Add(_subtext.Substring(0, _index)); }));
                    _subtext = _subtext.Remove(0, _index + 1);
                    text = _subtext + text;
                }
            }
            Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => { Log.Items.Add(text); }));
        }

        private void WriteToTab(string ip, string message, string nick, int fromCode)
        {
            TabItem _tab = null;
            foreach (TabItem t in TabControl.Items) {
                if ((nick + ":" + ip) == ((string)t.Tag)) {
                    _tab = t;
                    break;
                }
            }

            if (_tab == null) {
                return;
            }

            //From is 0 for system responses, 1 if it is from someone else, and 2 for yourself
            if (fromCode == 1) {
                message = "<" + nick + ">: " + message;
            }
            else if (fromCode == 2) {
                message = "<Me>: " + message;
            }
            TextBlock _text = new TextBlock();
            StackPanel _container = new StackPanel();
            ListBoxItem _item = new ListBoxItem();
            _container.Orientation = Orientation.Horizontal;
            _text.Text = message;
            _container.Children.Add(_text);
            _item.Content = _container;
            ((ListBox)((Grid)_tab.Content).Children[0]).Items.Add(_item);
            ((ListBox)((Grid)_tab.Content).Children[0]).ScrollIntoView(((ListBox)((Grid)_tab.Content).Children[0]).Items[(((ListBox)((Grid)_tab.Content).Children[0]).Items.Count - 1)]);
        }

        private void MainWindowClosed(object sender, EventArgs e)
        {
            ExitCommandExecuted(sender, e as ExecutedRoutedEventArgs);
        }
    }
}