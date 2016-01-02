using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace ChatApplication
{
    /// uncomment loopback
    /// ctrl + w to close tab
    /// first send command type

    public class PeerDataContainer
    {
        public string nick { get; set; }
        public string IP { get; set; }
        public string encodedMachineName { get; set; }
        public DateTime time { get; set; }
    }

    public class SortAdorner : Adorner
    {
        private static Geometry ascGeometry =
                Geometry.Parse("M 0 4 L 3.5 0 L 7 4 Z");

        private static Geometry descGeometry =
                Geometry.Parse("M 0 0 L 3.5 4 L 7 0 Z");

        public System.ComponentModel.ListSortDirection Direction { get; private set; }

        public SortAdorner(UIElement element, System.ComponentModel.ListSortDirection dir)
                : base(element)
        {
            this.Direction = dir;
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            if (AdornedElement.RenderSize.Width < 20)
                return;

            TranslateTransform transform = new TranslateTransform
                    (
                            AdornedElement.RenderSize.Width - 15,
                            (AdornedElement.RenderSize.Height - 5) / 2
                    );
            drawingContext.PushTransform(transform);

            Geometry geometry = ascGeometry;
            if (this.Direction == System.ComponentModel.ListSortDirection.Descending)
                geometry = descGeometry;
            drawingContext.DrawGeometry(Brushes.Black, null, geometry);

            drawingContext.Pop();
        }
    }

    struct ConnectedPeerDataContainer
    {
        public Socket socket;
        public string nick;
        public string encodedMachineName;
    }

    public partial class MainWindow : Window
    {

        /*--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------*/
        //Window variables
        /*--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------*/
        private bool isLogWindowOpen = false;

        private GridViewColumnHeader listViewSortCol = null;
        private SortAdorner listViewSortAdorner = null;

        private List<PeerDataContainer> broadcastingPeersList = new List<PeerDataContainer>();

        /*--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------*/
        //Back-end variables
        /*--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------*/
        private List<ConnectedPeerDataContainer> connectedPeersList = new List<ConnectedPeerDataContainer>();
        private Socket serverSocket;
        private ushort[] TCPPorts = { 5198, 9018, 9019, 9020, 9056 };
        private short numberOfBroadcastsSinceListUpdate = 0;
        private bool isServerRunning = false;
        private bool isBroadcasting = true;
        private string nick = "";
        private string password = null;
        private string encodedMachineName;
        private UdpClient broadcastReceiver;
        List<IPAddress> broadcastIPs = new List<IPAddress>();
        Timer updateBroadcastListTimer, broadcastTimer;

        /*--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------*/
        //Window functions
        /*--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------*/

        public MainWindow()
        {
            InitializeComponent();

            //Get and encode Machine Name
            byte[] hash = ((HashAlgorithm)CryptoConfig.CreateFromName("MD5")).ComputeHash(new UTF8Encoding().GetBytes(Environment.MachineName));
            encodedMachineName = BitConverter.ToString(hash).Replace("-", string.Empty).ToLower();

            //Start the server for receiving requests
            StartServer();

            //Start Receiving Broadcasts
            ReceiveBroadcasts();

            //Update clients list periodically
            updateBroadcastListTimer = new Timer(UpdateAvailableClients, null, 0, 20000);

        }

        private bool AddPeer(PeerDataContainer client)
        {
            //Check if already in List
            foreach (PeerDataContainer _peer in broadcastingPeersList)
            {
                if (_peer.IP == client.IP && _peer.nick == client.nick)
                {
                    broadcastingPeersList[broadcastingPeersList.IndexOf(_peer)].time = DateTime.Now;
                    Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => { BroadcastingList.ItemsSource = broadcastingPeersList; }));
                    return false;
                }
            }

            broadcastingPeersList.Add(client);
            if (listViewSortAdorner == null)
            {
                //Do nothing items will be sorted by themself later when content has been rendered
            }
            else if (listViewSortAdorner.Direction == System.ComponentModel.ListSortDirection.Ascending)
            {
                broadcastingPeersList.Sort(CompareAscending);
            }
            else
            {
                broadcastingPeersList.Sort(CompareDescending);
            }

            Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => { BroadcastingList.ItemsSource = broadcastingPeersList; BroadcastingList.Items.Refresh(); }));

            return true;
        }

        private void AddNewTab(string nick, string ip)
        {
            //Check if the tab is previously open
            foreach (TabItem _t in TabControl.Items)
            {
                if (ip == ((string)_t.Tag).Remove(0, ((string)_t.Tag).IndexOf(':') + 1) && nick == ((string)_t.Tag).Remove(((string)_t.Tag).IndexOf(':')))
                {
                    return;
                }
            }

            Image _closeButtonImage = new Image();
            StackPanel _st = new StackPanel();
            StackPanel _st2 = new StackPanel();
            Button _btn = new Button();
            Button _btn2 = new Button();
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
            _closeButtonImage.Source = new BitmapImage(new Uri(@"/Resources/normal_close_icon.png", UriKind.Relative));
            _closeButtonImage.Height = 13;
            _closeButtonImage.Width = 13;
            _st.Children.Add(_closeButtonImage);
            _btn.Content = _st;
            _st2.Orientation = Orientation.Horizontal;
            _txt.VerticalAlignment = VerticalAlignment.Center;

            if (nick.Length > 12)
            {
                _txt.Text = nick.Substring(0, 9) + "...";
            }
            else
            {
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
            _bndng.ConverterParameter = "0.7142857-228.61272-40";
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

            _grd.Children.Add(_ltbox);
            _grd.Children.Add(_txt2);
            _grd.Children.Add(_btn2);

            _tab.Content = _grd;

            _tab.Tag = nick + ":" + ip;
            //Finally add it
            TabControl.Items.Add(_tab);

            //If it is the only tab change focus to it
            if (TabControl.Items.Count == 1)
            {
                TabControl.SelectedIndex = 0;
            }
        }

        private int CompareAscending(PeerDataContainer a, PeerDataContainer b)
        {
            if (a == null)
            {
                if (b == null)
                {
                    return 0;
                }
                else
                {
                    return -1;
                }
            }
            else
            {
                if (b == null)
                {
                    return 1;
                }
                else
                {
                    int retval = a.nick.Length.CompareTo(b.nick.Length);

                    if (retval != 0)
                    {
                        return retval;
                    }
                    else
                    {
                        return a.nick.CompareTo(b.nick);
                    }

                }

            }
        }

        private int CompareDescending(PeerDataContainer a, PeerDataContainer b)
        {
            int result = CompareAscending(a, b);
            if (result == 1)
            {
                return -1;
            }
            else if (result == -1)
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }

        private void ConnectFromBroadcastList(object sender, RoutedEventArgs e)
        {
            string _tag = (string)((MenuItem)sender).Tag;
            string _ip = _tag.Remove(0, _tag.IndexOf(':') + 1);
            Thread _thread = new Thread(() => ConnectToPeerByIP(_ip));
            _thread.Name = _ip + " handler";
            _thread.IsBackground = true;
            _thread.Start();
        }

        private void EditPasswordClicked(object sender, RoutedEventArgs e)
        {
            EditPasswordWindow _passwordWindow = new EditPasswordWindow(password != null);
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
            if (isLogWindowOpen)
            {
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
            _st.Children.OfType<Image>().First().Source = new BitmapImage(new Uri(@"/Resources/pressed_close_icon.png", UriKind.Relative));
        }

        private void MouseUpOnTabCloseButton(object sender, MouseEventArgs e)
        {
            Button _btn = sender as Button;
            StackPanel _st = (StackPanel)_btn.Content;
            _st.Children.OfType<Image>().First<Image>().Source = new BitmapImage(new Uri(@"/Resources/normal_close_icon.png", UriKind.Relative));
            if (MessageBox.Show("      Do you really want to close this window ?\n        (Currently all chat history will be lost)", "Close Connection", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) == MessageBoxResult.Yes)
            {
                TabItem _tab = ((TabItem)((StackPanel)_btn.Parent).Parent);
                Disconnect((string)_tab.Tag);
                Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => { TabControl.Items.Remove(_tab);}));
            }
        }

        private void SearchAreaGotFocus(object sender, RoutedEventArgs e)
        {
            Panel.SetZIndex(MainWindowOverlayRectangleForSearch, 35);
            Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)delegate { SearchTextbox.SelectAll(); });
        }

        private void SearchTextboxKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key.ToString() == "Escape")
            {
                SearchTextbox.Text = "Search..";
                Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => { BroadcastingList.ItemsSource = broadcastingPeersList; BroadcastingList.Items.Refresh(); }));
                Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)delegate { SearchTextbox.SelectAll(); });
                return;
            }

            List<PeerDataContainer> _clientsMatchingSearchText = new List<PeerDataContainer>();

            foreach (PeerDataContainer _peer in broadcastingPeersList)
            {
                if (_peer.nick.ToLower().Contains(SearchTextbox.Text.ToLower()))
                {
                    _clientsMatchingSearchText.Add(_peer);
                }
            }

            Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => { BroadcastingList.ItemsSource = _clientsMatchingSearchText; BroadcastingList.Items.Refresh(); }));
        }

        private void SendMessageButtonClicked(object sender, RoutedEventArgs e)
        {
            Button _btn = ((Button)sender);
            string _buttonTag = (string)_btn.Tag;
            TextBox _txtbox = ((Grid)((Button)sender).Parent).Children.OfType<TextBox>().First<TextBox>();
            string _message = _txtbox.Text;
            ((Grid)_btn.Parent).Children.OfType<TextBox>().First<TextBox>().Text = "";
            _txtbox.Dispatcher.BeginInvoke(DispatcherPriority.Input, new ThreadStart(delegate () { _txtbox.Focus(); }));
            string _ip = _buttonTag.Remove(0, _buttonTag.IndexOf(':') + 1);
            string _nick = _buttonTag.Remove(_buttonTag.IndexOf(':'));
            if (_message == "")
            {
                return;
            }

            ConnectedPeerDataContainer _peer = new ConnectedPeerDataContainer();
            _peer.nick = "";
            _peer.socket = null;
            foreach (ConnectedPeerDataContainer _connectedPeer in connectedPeersList)
            {
                string _connectedPeerSocketString = _connectedPeer.socket.RemoteEndPoint.ToString();
                if (_connectedPeerSocketString.Remove(_connectedPeerSocketString.LastIndexOf(':')) == _ip)
                {
                    _peer = _connectedPeer;
                    break;
                }
            }

            if (_peer.socket == null)
            {
                WriteToTab(_ip, "Client not available", nick, 0);
                return;
            }
            WriteToTab(_ip, _message, _peer.nick, 2);
            Thread thread = new Thread(() => SendMessage(_message, _peer));
            thread.Name = "Message Sending to " + _ip;
            thread.Start();
        }

        private void SortBroadcastingPeers(object sender, EventArgs e)
        {
            //Incase it is being called by window for first time initialization then get the nick too
            if (sender.GetType() == PrimaryWindow.GetType())
            {
                // Get Nick From user
                while (nick == "" || nick == "Enter Nick" || (nick.IndexOf(':') != -1) || (nick.IndexOf('<') != -1) || (nick.IndexOf('>') != -1) || nick.Length > 50)
                {
                    InputDialogWindow _dialog = new InputDialogWindow("Enter Nick", "Enter a Nickname:\n(Should not contain the \':\', \'<\' or \'>\' Characters)", "Enter Nick", 0, 50);
                    _dialog.ShowInTaskbar = false;
                    _dialog.Owner = this;
                    if (_dialog.ShowDialog() == true)
                    {
                        nick = _dialog.ResponseText.Trim();
                    }
                }

                WriteToLogbox("Starting Status Broadcasts");
                broadcastTimer = new Timer(Broadcast, null, 0, Timeout.Infinite);
            }

            GridViewColumnHeader _column = SortHeader;
            if (listViewSortCol != null)
            {
                AdornerLayer.GetAdornerLayer(listViewSortCol).Remove(listViewSortAdorner);
            }

            System.ComponentModel.ListSortDirection newDir = System.ComponentModel.ListSortDirection.Descending;
            if (listViewSortCol == _column && listViewSortAdorner.Direction == newDir)
                newDir = System.ComponentModel.ListSortDirection.Ascending;

            listViewSortCol = _column;

            if (newDir == System.ComponentModel.ListSortDirection.Ascending)
            {
                broadcastingPeersList.Sort(CompareAscending);
            }
            else
            {
                broadcastingPeersList.Sort(CompareDescending);
            }

            listViewSortAdorner = new SortAdorner(listViewSortCol, newDir);
            AdornerLayer.GetAdornerLayer(listViewSortCol).Add(listViewSortAdorner);

            BroadcastingList.ItemsSource = broadcastingPeersList;
            BroadcastingList.Items.Refresh();

        }

        private void TabSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //If the event was bubbled up via some other subcontrol selection change event
            if (e.Source != TabControl)
            {
                return;
            }
            TabControl _tabctrl = sender as TabControl;
            int index = _tabctrl.SelectedIndex;
            if (index == -1)
            {
                return;
            }

            TextBox _txtbox = ((Grid)((TabItem)TabControl.Items[index]).Content).Children.OfType<TextBox>().First<TextBox>();
            _txtbox.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Input, new ThreadStart(delegate () { _txtbox.Focus(); }));
        }

        private void ToggleBroadcasting(object sender, RoutedEventArgs e)
        {
            if (isBroadcasting)
            {
                StopBroadcasts();
            }
            else
            {
                if (!isServerRunning)
                {
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
            if (isLogWindowOpen)
            {
                System.Windows.Media.Animation.Storyboard _storyboard = Resources["HideOverlayRectangleForLog"] as System.Windows.Media.Animation.Storyboard;
                _storyboard.Begin(MainWindowOverlayRectangleForLog);
                _storyboard = Resources["HideLog"] as System.Windows.Media.Animation.Storyboard;
                _storyboard.Begin(LogCanvas);
                Panel.SetZIndex(MainWindowOverlayRectangleForLog, -1);

                isLogWindowOpen = false;
            }
            else
            {
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
            if (isServerRunning)
            {
                StopServer();
                if (isBroadcasting)
                {
                    ToggleBroadcast.IsChecked = false;
                    StopBroadcasts();
                    isBroadcasting = false;
                }
            }
            else
            {
                StartServer();
                isBroadcasting = false;
                ToggleBroadcast.IsChecked = false;
            }
        }

        private void UpdateAvailableClients(object state)
        {
            TimeSpan _lastActive;
            List<PeerDataContainer> _removeList = new List<PeerDataContainer>();
            foreach (PeerDataContainer _peer in broadcastingPeersList)
            {
                _lastActive = DateTime.Now.Subtract(_peer.time);
                if (TimeSpan.Compare(_lastActive, new TimeSpan(0, 0, 30)) == 1)
                {
                    _removeList.Add(_peer);
                }
            }
            foreach (PeerDataContainer _peer in _removeList)
            {
                broadcastingPeersList.Remove(_peer);
            }

            if (listViewSortAdorner == null)
            {
                //Do nothing items will be sorted by themself later when content has been rendered
            }
            else if (listViewSortAdorner.Direction == System.ComponentModel.ListSortDirection.Ascending)
            {
                broadcastingPeersList.Sort(CompareAscending);
            }
            else
            {
                broadcastingPeersList.Sort(CompareDescending);
            }
            Dispatcher.Invoke(DispatcherPriority.Background, (Action)(() => { BroadcastingList.ItemsSource = broadcastingPeersList; BroadcastingList.Items.Refresh(); }));
        }

        private void WriteToLogbox(string text)
        {
            text = "** " + text;
            string _subtext;
            int _index;
            while (text.Length > 34)
            {
                _subtext = text.Substring(0, 34);
                _index = _subtext.LastIndexOf(' ');
                text = text.Remove(0, 34);
                if (_index == -1)
                {
                    Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => { Log.Items.Add(_subtext); }));
                }
                else
                {
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
            foreach (TabItem t in TabControl.Items)
            {
                if ((nick + ":" + ip) == ((string)t.Tag))
                {
                    _tab = t;
                    break;
                }
            }

            if (_tab == null)
            {
                return;
            }

            //From is 0 for system responses, 1 if it is from someone else, and 2 for yourself
            if (fromCode == 1)
            {
                message = "<" + nick + ">: " + message;
            }
            else if (fromCode == 2)
            {
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


        /*--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------*/
        //Back-end functions
        /*--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------*/

        private void ConnectToPeerByIP(string address, string password = null)
        {
            //Invalid IP address
            IPAddress _ip;
            if (!IPAddress.TryParse(address, out _ip))
            {
                MessageBox.Show("Invalid IP address");
                WriteToLogbox("Invalid IP address entered \"" + address + "\"");
                return;
            }

            // Trying to connect to the device itself
            //if (IPAddress.IsLoopback(IPAddress.Parse(address)))
            //{
            //    MessageBox.Show("Don't try to talk to yourself!!");
            //    logbox_write("Feeling Lonely");
            //    return;
            //}


            short _numberOfTries = 1;
            bool _isConnected = false;
            SocketException _exception = null;
            while (_numberOfTries <= 5)
            {
                for (int portOffset = 0; portOffset < TCPPorts.Length; portOffset++)
                {
                    Socket _peerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    Thread thread = new Thread(() => ConnectToParticularEndpoint(ref _isConnected, ref _exception, ref _peerSocket, portOffset, address));
                    thread.Name = "Attempt connection to " + address;
                    thread.IsBackground = true;
                    thread.Start();
                    thread.Join(1000);

                    //connected
                    if (_isConnected)
                    {
                        //Greet the server
                        try
                        {
                            if (Greet(false, _peerSocket, password))
                            {
                                AcceptGreetAndProcess(false, _peerSocket);
                                return;
                            }
                        }
                        catch (Exception)
                        {
                            //Connection problem while greeting
                        }

                        return;
                    }

                    //Some other exception occurs
                    if (_exception != null && _exception.ErrorCode != 10061 && _exception.ErrorCode != 10065 && _exception.ErrorCode != 10060 && _exception.ErrorCode != 10064)
                    {
                        WriteToLogbox("Exception caught- " + _exception.Message);
                        return;
                    }
                }

                //Connection Timed out
                WriteToLogbox(string.Format("Connection attempt {0} to {1} timed out", _numberOfTries, address));
                _numberOfTries++;

            }
            MessageBox.Show("Failed To establish connection to \"" + address + "\"");
            WriteToLogbox(string.Format("Failed To establish connection to the client {0}", address));
        }

        private void ConnectToParticularEndpoint(ref bool connected, ref SocketException ex, ref Socket _peerSocket, int portOffset, string address)
        {
            IPEndPoint client_endpoint = new IPEndPoint(IPAddress.Parse(address), TCPPorts[portOffset]);
            try
            {
                _peerSocket.Connect(client_endpoint);
                connected = true;
            }
            catch (SocketException e)
            {
                ex = e;
            }
        }

        public void Disconnect(string nickAndIP)
        {
            ConnectedPeerDataContainer _obj = new ConnectedPeerDataContainer();
            _obj.nick = "";
            _obj.socket = null;
            string _peerSocketRemoteEndPointString;
            foreach (ConnectedPeerDataContainer _peer in connectedPeersList)
            {
                _peerSocketRemoteEndPointString = _peer.socket.RemoteEndPoint.ToString();
                if ((_peer.nick + ":" + _peerSocketRemoteEndPointString.Remove(_peerSocketRemoteEndPointString.LastIndexOf(':'))) == nickAndIP)
                {
                    _obj = _peer;
                    break;
                }
            }
            try
            {
                _obj.socket.Shutdown(SocketShutdown.Both);
                _obj.socket.Close();
            }
            catch (Exception)
            {
                try
                {
                    _obj.socket.Close();
                }
                catch (Exception) { }
            }

            Console.WriteLine("Client " + _obj.nick + "(" + nickAndIP.Remove(0, nickAndIP.IndexOf(':') + 1) + ") has been successfully disconnected");
            connectedPeersList.Remove(_obj);
        }

        private void EndAcceptConnection(IAsyncResult a)
        {
            if (isServerRunning && serverSocket != null)
            {
                try
                {
                    Socket client_socket = serverSocket.EndAccept(a);

                    //send the standard greeting to the server
                    try
                    {
                        if (!Greet(true, client_socket))
                        {
                            serverSocket.BeginAccept(new AsyncCallback(EndAcceptConnection), null);
                            return;
                        }
                    }
                    catch (Exception)
                    {
                        serverSocket.BeginAccept(new AsyncCallback(EndAcceptConnection), null);
                        return;
                        //Error while greeting
                    }



                    Thread thread = new Thread(() => AcceptGreetAndProcess(true, client_socket));
                    string _clientSocketRemoteEndPointString = client_socket.RemoteEndPoint.ToString();
                    thread.Name = _clientSocketRemoteEndPointString.Remove(_clientSocketRemoteEndPointString.IndexOf(':')) + " handler";
                    thread.IsBackground = true;
                    thread.Start();

                    serverSocket.BeginAccept(new AsyncCallback(EndAcceptConnection), null);
                }
                catch (Exception e)
                {
                    WriteToLogbox("Exception caught- " + e.Source);
                    return;
                }
            }
        }

        private void EndReceiveBroadcasts(IAsyncResult ar)
        {
            IPEndPoint _ip = new IPEndPoint(IPAddress.Any, 15069);
            byte[] _bytes = broadcastReceiver.EndReceive(ar, ref _ip);
            string _message = Encoding.ASCII.GetString(_bytes);
            if (!_message.StartsWith("Hello everybody- "))
            {
                broadcastReceiver.BeginReceive(EndReceiveBroadcasts, null);
                return;
            }
            _message = _message.Remove(0, "Hello everybody- ".Length);
            string[] _messageParts = (_message).Split(new char[] { ':' });
            if (_messageParts.Length != 2 || (_messageParts[0] == nick && _messageParts[1] == encodedMachineName))
            {
                broadcastReceiver.BeginReceive(EndReceiveBroadcasts, null);
                return;
            }
            AddPeer(new PeerDataContainer() { IP = _ip.ToString().Remove(_ip.ToString().IndexOf(':')), nick = _messageParts[0], time = DateTime.Now, encodedMachineName = _messageParts[1] });

            broadcastReceiver.BeginReceive(EndReceiveBroadcasts, null);
        }        

        private bool Greet(bool serverOrClient, Socket socket, string password = null)
        {
            //Sends the initial greeting message with the nick, serverOrClient
            //is true for server and false in case of client

            string _message;

            if (serverOrClient)
            {
                _message = "Hello client, this is " + nick + ":" + encodedMachineName;
            }
            else
            {
                _message = "Hello server, this is " + nick + ":" + encodedMachineName;
                if (password != null && password != "d41d8cd98f00b204e9800998ecf8427e")
                {
                    _message = _message + ":" + password;
                }
            }

            byte[] _buffer;
            int _size, _sentSoFar;
            bool _continue = true;

            _buffer = new byte[4];
            _buffer = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(_message.Length));
            _sentSoFar = 0;
            _size = 4;
            while (_sentSoFar < _size)
            {
                int _read = socket.Send(_buffer, _sentSoFar, _size - _sentSoFar, SocketFlags.None);
                _sentSoFar += _read;
                if (_read == 0)
                {
                    // connection was broken
                    _continue = false;
                    break;
                }
            }
            if (!_continue)
            {
                return false;
            }

            _buffer = System.Text.Encoding.ASCII.GetBytes(_message);
            _sentSoFar = 0;
            _size = _buffer.Length;
            while (_sentSoFar < _size)
            {
                int _read = socket.Send(_buffer, _sentSoFar, _size - _sentSoFar, SocketFlags.None);
                _sentSoFar += _read;
                if (_read == 0)
                {
                    // connection was broken
                    _continue = false;
                    break;
                }
            }
            if (!_continue)
            {
                return false;
            }

            return true;
        }

        private void AcceptGreetAndProcess(bool serverOrClient, Socket socket)
        {
            // Checks greeting message and processes if correct, 
            // Extracts the nick and machine_name if true
            // serverOrClient is true for server and false in case of client

            //server recieves hello server, client receives hello client
            string _prefix;
            if (serverOrClient)
            {
                _prefix = "Hello server, this is ";
            }
            else
            {
                _prefix = "Hello client, this is ";
            }

            string _message;

            byte[] _buffer;
            int _size, _readSoFar;

            _size = 4;
            _buffer = new byte[_size];
            _readSoFar = 0;
            while (_readSoFar < _size)
            {
                int _read = 0;
                try
                {
                    _read = socket.Receive(_buffer, _readSoFar, _size - _readSoFar, SocketFlags.None);
                    _readSoFar += _read;
                }
                catch (Exception) { }


                if (_read == 0)
                {
                    // connection was broken
                    try
                    {
                        socket.Shutdown(SocketShutdown.Both);
                        socket.Close();
                    }
                    catch (Exception) { }
                    return;
                }
            }

            _size = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(_buffer, 0));

            _buffer = new byte[_size];
            _readSoFar = 0;
            while (_readSoFar < _size)
            {
                int _read = 0;
                try
                {
                    _read = socket.Receive(_buffer, _readSoFar, _size - _readSoFar, SocketFlags.None);
                    _readSoFar += _read;
                }
                catch (Exception) { }

                if (_read == 0)
                {
                    // connection was broken
                    try
                    {
                        socket.Shutdown(SocketShutdown.Both);
                        socket.Close();
                    }
                    catch (Exception) { }
                    return;
                }
            }

            _message = System.Text.Encoding.ASCII.GetString(_buffer);

            //didn't start with the given greeting
            if (_message.Substring(0, _prefix.Length) != _prefix)
            {
                try
                {
                    socket.Shutdown(SocketShutdown.Both);
                    socket.Close();
                }
                catch (Exception) { }
                return;
            }

            //no nickname sent
            _message = _message.Remove(0, _prefix.Length);
            if (_message.Length == 0)
            {
                try
                {
                    socket.Shutdown(SocketShutdown.Both);
                    socket.Close();
                }
                catch (Exception) { }
                return;
            }

            string _clientSocketRemoteEndPointString;
            string _socketRemoteEndPointString = socket.RemoteEndPoint.ToString();

            //Check if already connected to the client
            foreach (ConnectedPeerDataContainer _client in connectedPeersList)
            {
                try
                {
                    _clientSocketRemoteEndPointString = _client.socket.RemoteEndPoint.ToString();
                    if (_clientSocketRemoteEndPointString.Remove(_clientSocketRemoteEndPointString.LastIndexOf(':')) == _socketRemoteEndPointString.Remove(_socketRemoteEndPointString.LastIndexOf(':')))
                    {
                        WriteToLogbox("Already Connected to Client " + _socketRemoteEndPointString.Remove(_socketRemoteEndPointString.LastIndexOf(':')));
                        MessageBox.Show("Already Connected to Client");
                        try
                        {
                            socket.Shutdown(SocketShutdown.Both);
                            socket.Close();
                        }
                        catch (Exception) { }
                        return;
                    }
                } catch (ObjectDisposedException) { }
            }

            ConnectedPeerDataContainer obj = new ConnectedPeerDataContainer();
            int _indexOfDelimiter = _message.IndexOf(':');
            if (_indexOfDelimiter == -1)
            {
                try
                {
                    socket.Shutdown(SocketShutdown.Both);
                    socket.Close();
                }
                catch (Exception) { }
                return;
            }
            obj.nick = _message.Substring(0,_indexOfDelimiter);
            string _remainingMessage = _message.Substring(_indexOfDelimiter + 1);

            if (serverOrClient)
            {
                if(password != null)
                {                    
                    _indexOfDelimiter = _remainingMessage.IndexOf(':');

                    if (_indexOfDelimiter == -1)
                    {
                        //No password Provided
                        int _sendSoFar;

                        _buffer = new byte[4];
                        _buffer = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(2));
                        _sendSoFar = 0;
                        _size = 4;
                        while (_sendSoFar < _size)
                        {
                            int _sent = socket.Send(_buffer, _sendSoFar, _size - _sendSoFar, SocketFlags.None);
                            _sendSoFar += _sent;
                            if (_sent == 0)
                            {
                                // connection was broken
                                try
                                {
                                    socket.Shutdown(SocketShutdown.Both);
                                    socket.Close();
                                }
                                catch (Exception) { }
                                return;
                            }
                        }

                        try
                        {
                            socket.Shutdown(SocketShutdown.Both);
                            socket.Close();
                        }
                        catch (Exception) { }
                        return;
                    }

                    string _providedPassword = _remainingMessage.Substring(_indexOfDelimiter + 1);
                    
                    _remainingMessage = _remainingMessage.Substring(0, _indexOfDelimiter);
                    
                    if (_providedPassword != password)
                    {
                        int  _sendSoFar;
                        _buffer = new byte[4];
                        _buffer = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(3));
                        _sendSoFar = 0;
                        _size = 4;
                        while (_sendSoFar < _size)
                        {
                            int _sent = socket.Send(_buffer, _sendSoFar, _size - _sendSoFar, SocketFlags.None);
                            _sendSoFar += _sent;
                            if (_sent == 0)
                            {
                                // connection was broken
                                try
                                {
                                    socket.Shutdown(SocketShutdown.Both);
                                    socket.Close();
                                }
                                catch (Exception) { }
                                return;
                            }
                        }
                        try
                        {
                            socket.Shutdown(SocketShutdown.Both);
                            socket.Close();
                        }
                        catch (Exception) { }
                        return;
                    }
                }
            }

            obj.encodedMachineName = _remainingMessage;
            obj.socket = socket;
            connectedPeersList.Add(obj);

            Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => { AddNewTab(obj.nick, _socketRemoteEndPointString.Remove(_socketRemoteEndPointString.LastIndexOf(':'))); }));
            Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => { WriteToTab(_socketRemoteEndPointString.Remove(_socketRemoteEndPointString.LastIndexOf(':')), "Connected", obj.nick, 0); }));

            ProcessClient(obj);
        }

        private void ProcessClient(ConnectedPeerDataContainer client)
        {
            int _messageType = 0;
            string _clientSocketRemoteEndPointString = client.socket.RemoteEndPoint.ToString();
            string _ip = _clientSocketRemoteEndPointString.Remove(_clientSocketRemoteEndPointString.LastIndexOf(':'));
            string _nick = client.nick;
            Socket _peerSocket = client.socket;
            Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => { AddNewTab(client.nick, _clientSocketRemoteEndPointString.Remove(_clientSocketRemoteEndPointString.LastIndexOf(':'))); }));
            WriteToLogbox("Connected to- " + _ip);
            try
            {
                bool _continue = true;
                string _message;
                byte[] _buffer;
                int _size, _readSoFar;
                while (_peerSocket.Connected)
                {

                    _buffer = new byte[4];
                    _readSoFar = 0;
                    _size = 4;
                    while (_readSoFar < _size)
                    {
                        int _read = _peerSocket.Receive(_buffer, _readSoFar, _size - _readSoFar, SocketFlags.None);
                        _readSoFar += _read;
                        if (_read == 0)
                        {
                            // connection was broken
                            _continue = false;
                            break;
                        }
                    }
                    if (!_continue)
                    {
                        break;
                    }

                    _messageType = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(_buffer, 0));
                    /// MessageType details
                    /// 1- Normal sending and receive message
                    /// 2- Password Request
                    /// 3- Incorrect Password


                    switch (_messageType)
                    {
                        case 1:
                            _buffer = new byte[4];
                            _readSoFar = 0;
                            _size = 4;
                            while (_readSoFar < _size)
                            {
                                int _read = _peerSocket.Receive(_buffer, _readSoFar, _size - _readSoFar, SocketFlags.None);
                                _readSoFar += _read;
                                if (_read == 0)
                                {
                                    // connection was broken
                                    _continue = false;
                                    break;
                                }
                            }
                            if (!_continue)
                            {
                                break;
                            }
                            _size = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(_buffer, 0));

                            _buffer = new byte[_size];
                            _readSoFar = 0;
                            while (_readSoFar < _size)
                            {
                                int _read = _peerSocket.Receive(_buffer, _readSoFar, _size - _readSoFar, SocketFlags.None);
                                _readSoFar += _read;
                                if (_read == 0)
                                {
                                    // connection was broken
                                    _continue = false;
                                    break;
                                }
                            }
                            if (!_continue)
                            {
                                break;
                            }
                            _message = Encoding.ASCII.GetString(_buffer);

                            Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => { WriteToTab(_ip, _message, _nick, 1); }));
                            WriteToLogbox("Message Received- " + _nick + " (" + _clientSocketRemoteEndPointString + ") : " + _message);
                            break;

                        case 2:
                            WriteToLogbox("Password Required for " + _ip);
                            WriteToLogbox("Disconnected- " + _ip);
                            Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => { WriteToTab(_ip, "Password Required", nick, 0); }));
                            Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => { WriteToTab(_ip, "Disconnected", nick, 0); }));
                            try
                            {
                                _peerSocket.Shutdown(SocketShutdown.Both);
                                _peerSocket.Close();
                            }
                            catch (Exception) { }
                            Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => {  
                            ManuallyConnectDialog _dialog = new ManuallyConnectDialog(_ip, _nick, "Password Required");
                            _dialog.ShowInTaskbar = false;
                            _dialog.Owner = this;
                            if (_dialog.ShowDialog() == false)
                            {
                                return;
                            }
                            else
                            {
                                string _address = _dialog.IP;

                                byte[] hash = ((HashAlgorithm)CryptoConfig.CreateFromName("MD5")).ComputeHash(new UTF8Encoding().GetBytes(_dialog.password));
                                string _encodedPassword = BitConverter.ToString(hash).Replace("-", string.Empty).ToLower();

                                Thread _thread = new Thread(() => ConnectToPeerByIP(_address, _encodedPassword));
                                _thread.Name = _address + " handler";
                                _thread.IsBackground = true;
                                _thread.Start();
                            }
                            }));                            
                            _continue = false;                            
                            break;

                        case 3:
                            WriteToLogbox("Incorrect password provided for " + _ip);
                            WriteToLogbox("Disconnected- " + _ip);
                            Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => { WriteToTab(_ip, "Incorrect Password", nick, 0); }));
                            Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => { WriteToTab(_ip, "Disconnected", nick, 0); }));
                            try
                            {
                                _peerSocket.Shutdown(SocketShutdown.Both);
                                _peerSocket.Close();
                            }
                            catch (Exception) { }
                            Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => {
                                ManuallyConnectDialog _dialog = new ManuallyConnectDialog(_ip, _nick, "Incorrect Password");
                                _dialog.ShowInTaskbar = false;
                                _dialog.Owner = this;
                                if (_dialog.ShowDialog() == false)
                                {
                                    return;
                                }
                                else
                                {
                                    string _address = _dialog.IP;

                                    byte[] hash = ((HashAlgorithm)CryptoConfig.CreateFromName("MD5")).ComputeHash(new UTF8Encoding().GetBytes(_dialog.password));
                                    string _encodedPassword = BitConverter.ToString(hash).Replace("-", string.Empty).ToLower();

                                    Thread _thread = new Thread(() => ConnectToPeerByIP(_address, _encodedPassword));
                                    _thread.Name = _address + " handler";
                                    _thread.IsBackground = true;
                                    _thread.Start();
                                }
                            }));
                            _continue = false;
                            break;

                        default:
                            _continue = false;
                            break;
                    }
                    if(!_continue)
                    {
                        break;
                    }                   

                }
                connectedPeersList.Remove(client);
            }
            catch (Exception)
            {
                try
                {
                    connectedPeersList.Remove(client);
                }
                catch (Exception) { }
            }
            if (_messageType != 2 && _messageType != 3)
            {
                Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => { WriteToTab(_ip, "Disconnected", nick, 0); }));
                WriteToLogbox("Disconnected- " + _ip);
            }            
        }

        private void ReceiveBroadcasts()
        {
            try
            {
                broadcastReceiver = new UdpClient(15069);
                broadcastReceiver.BeginReceive(EndReceiveBroadcasts, null);
            }
            catch (Exception)
            {
                MessageBox.Show("   Failed to start broadcast receiver\nsome other app listening on port 15069");
                WriteToLogbox("Failed to receive broadcasts");
            }
        }

        private void SendMessage(string msg, ConnectedPeerDataContainer client)
        {
            Socket _peerSocket = client.socket;
            byte[] _buffer;
            int _size, _sendSoFar;

            _buffer = new byte[4];
            _buffer = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(1));
            _sendSoFar = 0;
            _size = 4;
            while (_sendSoFar < _size)
            {
                int _sent = _peerSocket.Send(_buffer, _sendSoFar, _size - _sendSoFar, SocketFlags.None);
                _sendSoFar += _sent;
                if (_sent == 0)
                {
                    // connection was broken
                    return;
                }
            }

            _buffer = new byte[4];
            _buffer = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(msg.Length));
            _sendSoFar = 0;
            _size = 4;
            while (_sendSoFar < _size)
            {
                int _sent = _peerSocket.Send(_buffer, _sendSoFar, _size - _sendSoFar, SocketFlags.None);
                _sendSoFar += _sent;
                if (_sent == 0)
                {
                    // connection was broken
                    return;
                }
            }

            _buffer = System.Text.Encoding.ASCII.GetBytes(msg);
            _sendSoFar = 0;
            _size = _buffer.Length;
            while (_sendSoFar < _size)
            {
                int _sent = _peerSocket.Send(_buffer, _sendSoFar, _size - _sendSoFar, SocketFlags.None);
                _sendSoFar += _sent;
                if (_sent == 0)
                {
                    // connection was broken
                    return;
                }
            }

            WriteToLogbox("Message Sent to " + client.nick + " (" + _peerSocket.RemoteEndPoint.ToString() + "): " + msg);

        }

        private void Broadcast(object state)
        {
            isBroadcasting = true;

            if (numberOfBroadcastsSinceListUpdate % 4 == 0)
            {
                IPAddress[] _localIPs = Dns.GetHostAddresses(Dns.GetHostName());
                broadcastIPs.Clear();
                broadcastIPs.Add(IPAddress.Broadcast);
                numberOfBroadcastsSinceListUpdate = 0;

                foreach (IPAddress ipaddress in _localIPs)
                {
                    if (ipaddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        IPAddress _subnetMask = null;
                        foreach (System.Net.NetworkInformation.NetworkInterface _adapter in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
                        {
                            foreach (System.Net.NetworkInformation.UnicastIPAddressInformation _unicastIPAddressInformation in _adapter.GetIPProperties().UnicastAddresses)
                            {
                                if (_unicastIPAddressInformation.Address.AddressFamily == AddressFamily.InterNetwork)
                                {
                                    if (ipaddress.Equals(_unicastIPAddressInformation.Address))
                                    {
                                        _subnetMask = _unicastIPAddressInformation.IPv4Mask;
                                    }
                                }
                            }
                        }
                        if (_subnetMask == null)
                        {
                            WriteToLogbox("Failed to get subnetmask for- " + ipaddress.ToString());
                        }
                        else
                        {
                            byte[] _ipAdressBytes = ipaddress.GetAddressBytes();
                            byte[] _subnetMaskBytes = _subnetMask.GetAddressBytes();

                            if (_ipAdressBytes.Length != _subnetMaskBytes.Length)
                                throw new ArgumentException("Lengths of IP address and subnet mask do not match.");

                            byte[] broadcastAddress = new byte[_ipAdressBytes.Length];
                            for (int i = 0; i < broadcastAddress.Length; i++)
                            {
                                broadcastAddress[i] = (byte)(_ipAdressBytes[i] | (_subnetMaskBytes[i] ^ 255));
                            }
                            broadcastIPs.Add(new IPAddress(broadcastAddress));
                        }
                    }
                }
            }

            foreach (IPAddress address in broadcastIPs)
            {
                UdpClient client = new UdpClient();
                IPEndPoint ip = new IPEndPoint(address, 15069);
                client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);
                client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontRoute, 1);
                byte[] bytes = Encoding.ASCII.GetBytes("Hello everybody- " + nick + ":" + encodedMachineName);

                try
                {
                    client.Send(bytes, bytes.Length, ip);
                    client.Close();
                }
                catch (Exception)
                {
                    WriteToLogbox("Broadcasts Failed");
                }

            }
            numberOfBroadcastsSinceListUpdate++;
            broadcastTimer.Change(10000, Timeout.Infinite);
        }

        private void StopBroadcasts()
        {
            WriteToLogbox("Stopping Status Broadcasts");
            isBroadcasting = false;
            Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => { ToggleBroadcast.IsChecked = false; }));
        }

        private void StartServer()
        {

            bool _isbinded = false;
            int _pos = 0;
            while (!_isbinded)
            {
                try
                {
                    IPEndPoint server_endpoint = new IPEndPoint(IPAddress.Any, TCPPorts[_pos]);
                    serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    serverSocket.Bind(server_endpoint);
                    _isbinded = true;
                }
                catch (Exception)
                {
                    if (_pos == (TCPPorts.Length - 1))
                    {
                        WriteToLogbox("Failed to start server all ports taken\n");
                        isServerRunning = false;
                        return;
                    }
                    else
                    {
                        _pos++;
                    }
                }

            }

            serverSocket.Listen(5);

            serverSocket.BeginAccept(new AsyncCallback(EndAcceptConnection), null);

            isServerRunning = true;

            WriteToLogbox("Server is up and running on port " + TCPPorts[_pos]);
        }

        private void StopServer()
        {
            if (serverSocket != null)
            {
                isServerRunning = false;
                serverSocket.Close();
                WriteToLogbox("Server Shutting Down");
                return;
            }
        }

        /*--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------*/
        //Commands
        /*--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------*/

        private void ExitCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            foreach (ConnectedPeerDataContainer client in connectedPeersList)
            {
                try
                {
                    client.socket.Shutdown(SocketShutdown.Both);
                    client.socket.Close();
                }
                catch (Exception) { };
            }
            StopServer();
            isBroadcasting = false;
            Application.Current.Shutdown();
        }

        private void ManuallyConnectCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            ManuallyConnectDialog _dialog = new ManuallyConnectDialog();
            _dialog.ShowInTaskbar = false;
            _dialog.Owner = this;
            if (_dialog.ShowDialog() == false)
            {
                return;
            }
            else
            {
                string _address = _dialog.IP;
                string _encodedPassword = _dialog.password;
                if (_encodedPassword != null)
                {
                    byte[] hash = ((HashAlgorithm)CryptoConfig.CreateFromName("MD5")).ComputeHash(new UTF8Encoding().GetBytes(_encodedPassword));
                    _encodedPassword = BitConverter.ToString(hash).Replace("-", string.Empty).ToLower();
                }

                Thread _thread = new Thread(() => ConnectToPeerByIP(_address, _encodedPassword));
                _thread.Name = _address + " handler";
                _thread.IsBackground = true;
                _thread.Start();
            }            
        }

        /*--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------*/
        //Inline functions
        /*--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------*/

        private void CloseAllClientConnections()
        {
            //**Make this function inline!!**
            throw new Exception("Make CloseClientConnections Inline");
            //**Make this function inline!!**

            foreach (ConnectedPeerDataContainer client in connectedPeersList)
            {
                client.socket.Shutdown(SocketShutdown.Both);
                client.socket.Close();
            }
        }

        public void Exit()
        {
            //**Make this function inline!!**
            throw new Exception("Make Exit Inline");
            //**Make this function inline!!**

            foreach (ConnectedPeerDataContainer client in connectedPeersList)
            {
                try
                {
                    client.socket.Shutdown(SocketShutdown.Both);
                    client.socket.Close();
                }
                catch (Exception) { };
            }
            StopServer();
            isBroadcasting = false;
            Application.Current.Shutdown();
        }

        private void GetMachineName()
        {
            //**Make this function inline!!**
            throw new Exception("Make GetMachineName Inline");
            //**Make this function inline!!**

            byte[] hash = ((HashAlgorithm)CryptoConfig.CreateFromName("MD5")).ComputeHash(new UTF8Encoding().GetBytes(Environment.MachineName));
            encodedMachineName = BitConverter.ToString(hash).Replace("-", string.Empty).ToLower();
        }

        private IPAddress GetSubnetMask(IPAddress address)
        {
            //**Make this function inline!!**
            throw new Exception("Make GetSubnetMask Inline");
            //**Make this function inline!!**

            foreach (System.Net.NetworkInformation.NetworkInterface _adapter in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
            {
                foreach (System.Net.NetworkInformation.UnicastIPAddressInformation _unicastIPAddressInformation in _adapter.GetIPProperties().UnicastAddresses)
                {
                    if (_unicastIPAddressInformation.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        if (address.Equals(_unicastIPAddressInformation.Address))
                        {
                            return _unicastIPAddressInformation.IPv4Mask;
                        }
                    }
                }
            }
            throw new ArgumentException(string.Format("Can't find subnetmask for IP address '{0}'", address));
        }

        public IPAddress GetBroadcastAddress(IPAddress address, IPAddress subnetMask)
        {
            //**Make this function inline!!**
            throw new Exception("Make GetBroadcastAddress Inline");
            //**Make this function inline!!**

            byte[] _ipAdressBytes = address.GetAddressBytes();
            byte[] _subnetMaskBytes = subnetMask.GetAddressBytes();

            if (_ipAdressBytes.Length != _subnetMaskBytes.Length)
                throw new ArgumentException("Lengths of IP address and subnet mask do not match.");

            byte[] broadcastAddress = new byte[_ipAdressBytes.Length];
            for (int i = 0; i < broadcastAddress.Length; i++)
            {
                broadcastAddress[i] = (byte)(_ipAdressBytes[i] | (_subnetMaskBytes[i] ^ 255));
            }
            return new IPAddress(broadcastAddress);
        }
    }
}



namespace Converters
{
    public class SubtractAConstant : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            double _size;
            if (parameter == null || value == null)
            {
                return 0;
            }
            else
            {
                try
                {
                    _size = System.Convert.ToDouble(value) - System.Convert.ToDouble(parameter);
                    if (_size < 0)
                    {
                        return 0;
                    }
                    return _size;
                }
                catch
                {
                    return 0;
                }

            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            double _size;
            if (parameter == null || value == null)
            {
                return 0;
            }
            else
            {
                try
                {
                    _size = System.Convert.ToDouble(value) + System.Convert.ToDouble(parameter);
                    return _size;
                }
                catch
                {
                    return 0;
                }
            }
        }
    }

    public class MultiplyAConstant : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            double _val;
            if (parameter == null || value == null)
            {
                return 0;
            }
            else
            {
                try
                {
                    _val = System.Convert.ToDouble(value) * System.Convert.ToDouble(parameter);
                    return _val;
                }
                catch
                {
                    return 0;
                }

            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            double _val;
            if (parameter == null || value == null)
            {
                return 0;
            }
            else
            {
                try
                {
                    _val = System.Convert.ToDouble(value) / System.Convert.ToDouble(parameter);
                    return _val;
                }
                catch
                {
                    return 0;
                }
            }
        }
    }

    public class MultiplyAndThenSubtractTwoConstants : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            double _val;
            if (parameter == null || value == null)
            {
                return 0;
            }
            else
            {
                try
                {
                    string[] _parameters = ((string)parameter).Split(new char[] { '-' });
                    double multiplier = double.Parse(_parameters[0]);
                    int subtractval = int.Parse(_parameters[1]);
                    _val = (System.Convert.ToDouble(value) * multiplier) - subtractval;
                    if (_val < 0)
                    {
                        return 0;
                    }
                    return _val;
                }
                catch
                {
                    return 0;
                }

            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            double _val;
            if (parameter == null || value == null)
            {
                return 0;
            }
            else
            {
                try
                {
                    string[] _parameters = ((string)parameter).Split(new char[] { '-' });
                    double multiplier = double.Parse(_parameters[0]);
                    int subtractval = int.Parse(_parameters[1]);
                    _val = (System.Convert.ToDouble(value) + subtractval) / multiplier;
                    return _val;
                }
                catch
                {
                    return 0;
                }
            }
        }
    }

    public class SubtractAndThenMultiplyTwoConstants : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            double _val;
            if (parameter == null || value == null)
            {
                return 0;
            }
            else
            {
                try
                {
                    string _param = parameter.ToString();
                    double _multiplier = double.Parse(_param.Substring(0, _param.IndexOf('-')));
                    int subtractval = int.Parse(_param.Remove(0, _param.IndexOf('-') + 1));
                    _val = (System.Convert.ToDouble(value) - subtractval) * _multiplier;
                    if (_val < 0)
                    {
                        return 0;
                    }
                    return _val;
                }
                catch
                {
                    return 0;
                }

            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            double _val;
            if (parameter == null || value == null)
            {
                return 0;
            }
            else
            {
                try
                {
                    string _param = parameter.ToString();
                    double multiplier = double.Parse(_param.Substring(0, _param.IndexOf('-')));
                    int subtractval = int.Parse(_param.Remove(0, _param.IndexOf('-') + 1));
                    _val = (System.Convert.ToDouble(value) / multiplier) + subtractval;
                    return _val;
                }
                catch
                {
                    return 0;
                }
            }
        }
    }

    public class TabWidthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            double _val;
            if (parameter == null || value == null)
            {
                return 0;
            }
            else
            {
                try
                {
                    string[] _parameters = ((string)parameter).Split(new char[] { '-' });
                    double multiplier = double.Parse(_parameters[0]);
                    double max = double.Parse(_parameters[1]);
                    _val = System.Convert.ToDouble(value);
                    if (((1 - multiplier) * _val) > max)
                    {
                        _val = (_val - max) - 20;
                        if (_val < 0)
                        {
                            return 0;
                        }
                        return _val;
                    }
                    else
                    {
                        _val = (multiplier * _val) - 20;
                        if (_val < 0)
                        {
                            return 0;
                        }
                        return _val;
                    }
                }
                catch
                {
                    return 0;
                }

            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class MessageboxWidthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            double _val;
            if (parameter == null || value == null)
            {
                return 0;
            }
            else
            {
                try
                {
                    string[] _parameters = ((string)parameter).Split(new char[] { '-' });
                    double multiplier = double.Parse(_parameters[0]);
                    double max = double.Parse(_parameters[1]);
                    double sub = double.Parse(_parameters[2]);
                    _val = System.Convert.ToDouble(value);
                    if (((1 - multiplier) * _val) > max)
                    {
                        return (_val - max) - (33 + sub);
                    }
                    else
                    {
                        return (multiplier * _val) - (33 + sub);
                    }
                }
                catch
                {
                    return 0;
                }

            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class SubtractAConstantFromLeftMargin : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            double _size;
            if (parameter == null || value == null)
            {
                return ("0,0,0,0");
            }
            else
            {
                try
                {
                    _size = System.Convert.ToDouble(value) - System.Convert.ToDouble(parameter);
                    return (_size.ToString() + ",0,0,0");
                }
                catch
                {
                    return ("0,0,0,0");
                }

            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            double _size;
            if (parameter == null || value == null)
            {
                return ("0,0,0,0");
            }
            else
            {
                try
                {
                    _size = System.Convert.ToDouble(value) + System.Convert.ToDouble(parameter);
                    return (_size.ToString() + ",0,0,0");
                }
                catch
                {
                    return ("0,0,0,0");
                }
            }
        }
    }

    public class SubtractAConstantFromRightMargin : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            double _size;
            if (parameter == null || value == null)
            {
                return ("0,0,0,0");
            }
            else
            {
                try
                {
                    _size = System.Convert.ToDouble(value) - System.Convert.ToDouble(parameter);
                    return ("0,0," + _size.ToString() + ",0");
                }
                catch
                {
                    return ("0,0,0,0");
                }

            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            double _size;
            if (parameter == null || value == null)
            {
                return ("0,0,0,0");
            }
            else
            {
                try
                {
                    _size = System.Convert.ToDouble(value) + System.Convert.ToDouble(parameter);
                    return ("0,0," + _size.ToString() + ",0");
                }
                catch
                {
                    return ("0,0,0,0");
                }
            }
        }
    }

    public class JoinNickAndIP : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return (((string)values[0]) + ":" + ((string)values[1]));
        }
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException("Cannot convert back");
        }

    }

    public class SubtractTwoMultiBindings : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return (((double)values[0] - (double)values[1]) - double.Parse((string)parameter));
        }
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException("Cannot convert back");
        }

    }

    public class StatusBarText : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string _status;

            if (parameter == null || value == null)
            {
                return "";
            }

            if ((bool)value == true)
            {
                _status = "Yes";
            }
            else
            {
                _status = "No";
            }

            //For Server Status
            if (int.Parse((string)parameter) == 0)
            {
                return "Server Running: " + _status;
            }

            //For Broadcasting status
            else
            {
                return "Status Broadcasting: " + _status;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}
        
namespace CustomCommands
{
    public static class CustomCommands
    {
        public static readonly RoutedUICommand Exit = new RoutedUICommand(
                        "Exit",
                        "Exit",
                        typeof(CustomCommands),
                        new InputGestureCollection()
                        {
                                        new KeyGesture(Key.F4, ModifierKeys.Alt)
                        }
                );

        public static readonly RoutedUICommand ManuallyConnectDialog = new RoutedUICommand(
                        "Connect by IP address",
                        "Connect",
                        typeof(CustomCommands),
                        new InputGestureCollection()
                        {
                                        new KeyGesture(Key.N, ModifierKeys.Control)
                        }
                );
    }
}