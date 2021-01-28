namespace RatioMaster_source
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Drawing;
    using System.IO;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Windows.Forms;

    using BitTorrent;

    using BytesRoad.Net.Sockets;

    using Microsoft.Win32;

    internal partial class RM : UserControl
    {
        // Variables
        #region Variables
        private bool getnew = true;
        private readonly Random rand = new Random(((int)DateTime.Now.Ticks));
        private int remWork = 0;
        internal string DefaultDirectory = "";
        private const string DefaultClient = "uTorrent";
        private const string DefaultClientVersion = "3.3.2";

        // internal delegate SocketEx createSocketCallback();
        internal delegate void SetTextCallback(string logLine);

        internal delegate void updateScrapCallback(string seedStr, string leechStr, string finishedStr);

        private TorrentClient currentClient;
        private ProxyInfo currentProxy;
        internal TorrentInfo currentTorrent = new TorrentInfo();
        internal Torrent currentTorrentFile = new Torrent();
        internal TcpListener localListen;
        private bool seedMode = false;
        private bool updateProcessStarted = false;
        private bool requestScrap;
        private bool scrapStatsUpdated;
        private int temporaryIntervalCounter = 0;
        bool IsExit = false;
        private readonly string version = "";

        #endregion

        // Methods
        #region Methods
        #region Main Form Events
        internal RM()
        {
            InitializeComponent();
            deployDefaultValues();
            GetPCinfo();
            ReadSettings();
        }

        internal void Form1_DragDrop(object sender, DragEventArgs e)
        {
            string[] s = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (s == null)
            {
                s = (string[])e.Data.GetData("System.String[]", true);
                if (s == null)
                {
                    return;
                }
            }

            loadTorrentFileInfo(s[0]);
        }

        internal void Form1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop) || e.Data.GetDataPresent(DataFormats.Text) || e.Data.GetFormats().ToString().Equals("System.String[]"))
            {
                e.Effect = DragDropEffects.All;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        internal void ExitRatioMaster()
        {
            IsExit = true;
            if (updateProcessStarted)
            {
                @StopButton_Click(null, null);
            }

            // this.Close();
            // Process.GetCurrentProcess().Kill();
            // Application.Exit();
        }

        internal void deployDefaultValues()
        {
            TorrentInfo torrent = new TorrentInfo(0, 0);
            trackerAddress.Text = torrent.tracker;
            shaHash.Text = torrent.hash;
            long num1 = torrent.uploadRate / 1024;
            uploadRate.Text = num1.ToString();
            long num2 = torrent.downloadRate / 1024;
            downloadRate.Text = num2.ToString();
            interval.Text = torrent.interval.ToString();
            comboProxyType.SelectedItem = "None";
        }

        #endregion
        #region Log code
        internal void AddClientInfo()
        {
            // Add log info
            AddLogLine("CLIENT EMULATION INFO:");
            AddLogLine("Name: " + currentClient.Name);
            AddLogLine("HttpProtocol: " + currentClient.HttpProtocol);
            AddLogLine("HashUpperCase: " + currentClient.HashUpperCase);
            AddLogLine("Key: " + currentClient.Key);
            AddLogLine("Headers:......");
            AddLog(currentClient.Headers);
            AddLogLine("PeerID: " + currentClient.PeerID);
            AddLogLine("Query: " + currentClient.Query + "\n" + "\n");
        }

        internal void AddLog(string logLine)
        {
            if (logWindow.InvokeRequired)
            {
                SetTextCallback d = AddLogLine;
                Invoke(d, new object[] { logLine });
            }
            else
            {
                if (checkLogEnabled.Checked && IsExit != true)
                {
                    try
                    {
                        logWindow.AppendText(logLine);

                        // logWindow.SelectionStart = logWindow.Text.Length;
                        logWindow.ScrollToCaret();
                    }
                    catch (Exception) { }
                }
            }
        }

        internal void AddLogLine(string logLine)
        {
            if (logWindow.InvokeRequired && IsExit != true)
            {
                SetTextCallback d = AddLogLine;
                Invoke(d, new object[] { logLine });
            }
            else
            {
                if (checkLogEnabled.Checked)
                {
                    try
                    {
                        DateTime dtNow = DateTime.Now;
                        string dateString;

                        if (!MainForm._24h_format_enabled)
                            dateString = "[" + String.Format("{0:hh:mm:ss}", dtNow) + "]";
                        else 
                            dateString = "[" + String.Format("{0:HH:mm:ss}", dtNow) + "]";

                        logWindow.AppendText(dateString + " " + logLine + "\r\n");

                        // logWindow.SelectionStart = logWindow.Text.Length;
                        logWindow.ScrollToCaret();
                    }
                    catch (Exception) { }
                }
            }
        }

        internal void ClearLog()
        {
            logWindow.Clear();
        }

        internal void GetPCinfo()
        {
            try
            {
                AddLogLine("CurrentDirectory: " + Environment.CurrentDirectory);
                AddLogLine("HasShutdownStarted: " + Environment.HasShutdownStarted);
                AddLogLine("MachineName: " + Environment.MachineName);
                AddLogLine("OSVersion: " + Environment.OSVersion);
                AddLogLine("ProcessorCount: " + Environment.ProcessorCount);
                AddLogLine("UserDomainName: " + Environment.UserDomainName);
                AddLogLine("UserInteractive: " + Environment.UserInteractive);
                AddLogLine("UserName: " + Environment.UserName);
                AddLogLine("Version: " + Environment.Version);
                AddLogLine("WorkingSet: " + Environment.WorkingSet);
                AddLogLine("");
            }
            catch (Exception) { }
        }

        internal void SaveLog_FileOk(object sender, CancelEventArgs e)
        {
            string file = SaveLog.FileName;
            StreamWriter sw = new StreamWriter(file);
            sw.Write(logWindow.Text);
            sw.Close();
        }

        #endregion
        #region Tcp Listener code
        private void OpenTcpListener()
        {
            try
            {
                if (checkTCPListen.Checked && localListen == null && currentProxy.ProxyType == ProxyType.None)
                {
                    localListen = new TcpListener(IPAddress.Any, int.Parse(currentTorrent.port));
                    try
                    {
                        localListen.Start();
                        AddLogLine("Started TCP listener on port " + currentTorrent.port);
                    }
                    catch
                    {
                        AddLogLine("TCP listener is alredy started from other torrent or from your torrent client");
                        return;
                    }

                    Thread myThread = new Thread(AcceptTcpConnection);
                    myThread.Name = "AcceptTcpConnection() Thread";
                    myThread.Start();
                }
            }
            catch (Exception e)
            {
                AddLogLine("Error in OpenTcpListener(): " + e.Message);
                if (localListen != null)
                {
                    localListen.Stop();
                    localListen = null;
                }

                return;
            }

            AddLogLine("OpenTcpListener() successfully finished!");
        }

        private void AcceptTcpConnection()
        {
            Socket socket1 = null;
            try
            {
                Encoding encoding1 = Encoding.GetEncoding(0x6faf);
                string text1;
                while (true)
                {
                    socket1 = localListen.AcceptSocket();
                    byte[] buffer1 = new byte[0x43];
                    if ((socket1 != null) && socket1.Connected)
                    {
                        NetworkStream stream1 = new NetworkStream(socket1);
                        stream1.ReadTimeout = 0x3e8;
                        try
                        {
                            stream1.Read(buffer1, 0, buffer1.Length);
                        }
                        catch (Exception)
                        {
                        }

                        text1 = encoding1.GetString(buffer1, 0, buffer1.Length);
                        if ((text1.IndexOf("BitTorrent protocol") >= 0) && (text1.IndexOf(encoding1.GetString(this.currentTorrentFile.InfoHash)) >= 0))
                        {
                            byte[] buffer2 = createHandshakeResponse();
                            stream1.Write(buffer2, 0, buffer2.Length);
                        }

                        socket1.Close();
                        stream1.Close();
                        stream1.Dispose();
                    }
                }
            }
            catch (Exception exception1)
            {
                AddLogLine("Error in AcceptTcpConnection(): " + exception1.Message);
                return;
            }
            finally
            {
                if (socket1 != null)
                {
                    socket1.Close();
                    AddLogLine("Closed socket");
                }

                CloseTcpListener();
            }
        }

        private Socket createRegularSocket()
        {
            Socket socket1 = null;
            try
            {
                socket1 = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            }
            catch (Exception exception1)
            {
                AddLogLine("createSocket error: " + exception1.Message);
            }

            return socket1;
        }

        private byte[] createChokeResponse()
        {
            byte[] buffer2 = new byte[5];
            buffer2[3] = 1;
            return buffer2;
        }

        private byte[] createHandshakeResponse()
        {
            int num1 = 0;
            Encoding encoding1 = Encoding.GetEncoding(0x6faf);
            new StringBuilder();
            string text1 = "BitTorrent protocol";
            byte[] buffer1 = new byte[0x100];
            buffer1[num1++] = (byte)text1.Length;
            encoding1.GetBytes(text1, 0, text1.Length, buffer1, num1);
            num1 += text1.Length;
            for (int num2 = 0; num2 < 8; num2++)
            {
                buffer1[num1++] = 0;
            }

            Buffer.BlockCopy(currentTorrentFile.InfoHash, 0, buffer1, num1, currentTorrentFile.InfoHash.Length);
            num1 += currentTorrentFile.InfoHash.Length;
            encoding1.GetBytes(currentTorrent.peerID.ToCharArray(), 0, currentTorrent.peerID.Length, buffer1, num1);
            num1 += encoding1.GetByteCount(currentTorrent.peerID);
            return buffer1;
        }

        internal void CloseTcpListener()
        {
            if (localListen != null)
            {
                localListen.Stop();
                localListen = null;
                AddLogLine("TCP Listener closed");
            }
        }

        #endregion
        #region Get client
        internal string GetClientName()
        {
            return cmbClient.SelectedItem + " " + cmbVersion.SelectedItem;
        }

        internal void cmbClient_SelectedIndexChanged(object sender, EventArgs e)
        {
            cmbVersion.Items.Clear();
            switch (cmbClient.SelectedItem.ToString())
            {
                case "BitComet":
                    {
                        cmbVersion.Items.Add("1.20");
                        cmbVersion.Items.Add("1.03");
                        cmbVersion.Items.Add("0.98");
                        cmbVersion.Items.Add("0.96");
                        cmbVersion.Items.Add("0.93");
                        cmbVersion.Items.Add("0.92");
                        cmbVersion.SelectedItem = "1.20";
                        if (customPeersNum.Text == "0" || customPeersNum.Text == "") customPeersNum.Text = "200";
                        break;
                    }

                case "Vuze":
                    {
                        cmbVersion.Items.Add("4.2.0.8");
                        cmbVersion.SelectedItem = "4.2.0.8";
                        if (customPeersNum.Text == "0" || customPeersNum.Text == "") customPeersNum.Text = "50";
                        break;
                    }

                case "Azureus":
                    {
                        cmbVersion.Items.Add("3.1.1.0");
                        cmbVersion.Items.Add("3.0.5.0");
                        cmbVersion.Items.Add("3.0.4.2");
                        cmbVersion.Items.Add("3.0.3.4");
                        cmbVersion.Items.Add("3.0.2.2");
                        cmbVersion.Items.Add("2.5.0.4");
                        cmbVersion.SelectedItem = "3.1.1.0";
                        if (customPeersNum.Text == "0" || customPeersNum.Text == "") customPeersNum.Text = "50";
                        break;
                    }

                case "uTorrent":
                    {
                        cmbVersion.Items.Add("3.3.2");
                        cmbVersion.Items.Add("3.3.0");
                        cmbVersion.Items.Add("3.2.0");
                        cmbVersion.Items.Add("2.0.1 (build 19078)");
                        cmbVersion.Items.Add("1.8.5 (build 17414)");
                        cmbVersion.Items.Add("1.8.1-beta(11903)");
                        cmbVersion.Items.Add("1.8.0");
                        cmbVersion.Items.Add("1.7.7");
                        cmbVersion.Items.Add("1.7.6");
                        cmbVersion.Items.Add("1.7.5");
                        cmbVersion.Items.Add("1.6.1");
                        cmbVersion.Items.Add("1.6");
                        cmbVersion.SelectedItem = "3.3.2";
                        if (customPeersNum.Text == "0" || customPeersNum.Text == "") customPeersNum.Text = "200";
                        break;
                    }

                case "BitTorrent":
                    {
                        cmbVersion.Items.Add("6.0.3 (8642)");
                        cmbVersion.SelectedItem = "6.0.3 (8642)";
                        if (customPeersNum.Text == "0" || customPeersNum.Text == "") customPeersNum.Text = "200";
                        break;
                    }

		case "Transmission":
		    {
			cmbVersion.Items.Add("2.82 (14160)");
			cmbVersion.Items.Add("2.92 (14714)");
			cmbVersion.SelectedItem = "2.92 (14714)";
			if (customPeersNum.Text == "0" || customPeersNum.Text == "") customPeersNum.Text = "200";
                        break;
		    }

                case "BitLord":
                    {
                        cmbVersion.Items.Add("1.1");
                        cmbVersion.SelectedItem = "1.1";
                        if (customPeersNum.Text == "0" || customPeersNum.Text == "") customPeersNum.Text = "200";
                        break;
                    }

                case "ABC":
                    {
                        cmbVersion.Items.Add("3.1");
                        cmbVersion.SelectedItem = "3.1";
                        if (customPeersNum.Text == "0" || customPeersNum.Text == "") customPeersNum.Text = "200";
                        break;
                    }

                case "BTuga":
                    {
                        cmbVersion.Items.Add("2.1.8");
                        cmbVersion.SelectedItem = "2.1.8";
                        if (customPeersNum.Text == "0" || customPeersNum.Text == "") customPeersNum.Text = "200";
                        break;
                    }

                case "BitTornado":
                    {
                        cmbVersion.Items.Add("0.3.17");
                        cmbVersion.SelectedItem = "0.3.17";
                        if (customPeersNum.Text == "0" || customPeersNum.Text == "") customPeersNum.Text = "200";
                        break;
                    }

                case "Burst":
                    {
                        cmbVersion.Items.Add("3.1.0b");
                        cmbVersion.SelectedItem = "3.1.0b";
                        if (customPeersNum.Text == "0" || customPeersNum.Text == "") customPeersNum.Text = "200";
                        break;
                    }

                case "BitTyrant":
                    {
                        cmbVersion.Items.Add("1.1");
                        cmbVersion.SelectedItem = "1.1";
                        if (customPeersNum.Text == "0" || customPeersNum.Text == "") customPeersNum.Text = "50";
                        break;
                    }

                case "BitSpirit":
                    {
                        cmbVersion.Items.Add("3.6.0.200");
                        cmbVersion.Items.Add("3.1.0.077");
                        cmbVersion.SelectedItem = "3.6.0.200";
                        if (customPeersNum.Text == "0" || customPeersNum.Text == "") customPeersNum.Text = "200";
                        break;
                    }

                case "Deluge":
                    {
                        cmbVersion.Items.Add("1.2.0");
                        cmbVersion.Items.Add("0.5.8.7");
                        cmbVersion.Items.Add("0.5.8.6");
                        cmbVersion.SelectedItem = "1.2.0";
                        if (customPeersNum.Text == "0" || customPeersNum.Text == "") customPeersNum.Text = "200";
                        break;
                    }

                case "KTorrent":
                    {
                        cmbVersion.Items.Add("2.2.1");
                        cmbVersion.SelectedItem = "2.2.1";
                        if (customPeersNum.Text == "0" || customPeersNum.Text == "") customPeersNum.Text = "100";
                        break;
                    }

                case "Gnome BT":
                    {
                        cmbVersion.Items.Add("0.0.28-1");
                        cmbVersion.SelectedItem = "0.0.28-1";
                        if (customPeersNum.Text == "0" || customPeersNum.Text == "") customPeersNum.Text = "200";
                        break;
                    }

                default:
                    {
                        cmbClient.SelectedItem = DefaultClient;
                        if (customPeersNum.Text == "0" || customPeersNum.Text == "") customPeersNum.Text = "200";
                        break;
                    }
            }

            // getCurrentClient(GetClientName());
        }

        private void cmbVersion_SelectedValueChanged(object sender, EventArgs e)
        {
            if (getnew == false)
            {
                getnew = true;
                return;
            }

            if (chkNewValues.Checked)
            {
                SetCustomValues();
            }
        }

        #endregion
        #region Get(open) torrent
        internal void loadTorrentFileInfo(string torrentFilePath)
        {
            try
            {
                currentTorrentFile = new Torrent(torrentFilePath);
                torrentFile.Text = torrentFilePath;
                trackerAddress.Text = currentTorrentFile.Announce;
                shaHash.Text = ToHexString(currentTorrentFile.InfoHash);

                // text.Text = currentTorrentFile.totalLength.ToString();
                txtTorrentSize.Text = FormatFileSize((currentTorrentFile.totalLength));
            }
            catch (Exception ex)
            {
                AddLogLine(ex.ToString());
            }
        }

        private TorrentInfo getCurrentTorrent()
        {
            Uri trackerUri;
            TorrentInfo torrent = new TorrentInfo(0, 0);
            try
            {
                trackerUri = new Uri(trackerAddress.Text);
            }
            catch (Exception exception1)
            {
                AddLogLine(exception1.Message);
                return torrent;
            }

            torrent.tracker = trackerAddress.Text;
            torrent.trackerUri = trackerUri;
            torrent.hash = shaHash.Text;
            torrent.uploadRate = (Int64)(uploadRate.Text.ParseValidFloat(50) * 1024);

            // uploadRate.Text = (torrent.uploadRate / (float)1024).ToString();
            torrent.downloadRate = (Int64)(downloadRate.Text.ParseValidFloat(10) * 1024);

            // downloadRate.Text = (torrent.downloadRate / (float)1024).ToString();
            torrent.interval = interval.Text.ParseValidInt(torrent.interval);
            interval.Text = torrent.interval.ToString();
            double finishedPercent = fileSize.Text.ParseDouble(0);
            if (finishedPercent < 0 || finishedPercent > 100)
            {
                AddLogLine("Finished value is invalid: " + fileSize.Text + ", assuming 0 as default value");
                finishedPercent = 0;
            }

            if (finishedPercent >= 100)
            {
                seedMode = true;
                finishedPercent = 100;
            }

            fileSize.Text = finishedPercent.ToString();
            long size = (long)currentTorrentFile.totalLength;
            if (currentTorrentFile != null)
            {
                if (finishedPercent == 0)
                {
                    torrent.totalsize = (long)currentTorrentFile.totalLength;
                }
                else if (finishedPercent == 100)
                {
                    torrent.totalsize = 0;
                }
                else
                {
                    torrent.totalsize = (long)((currentTorrentFile.totalLength * (100 - finishedPercent)) / 100);
                }
            }
            else
            {
                torrent.totalsize = 0;
            }

            torrent.left = torrent.totalsize;
            torrent.filename = torrentFile.Text;

            // deploy custom values
            torrent.port = customPort.Text.GetValueDefault(torrent.port);
            customPort.Text = torrent.port;
            torrent.key = customKey.Text.GetValueDefault(currentClient.Key);
            torrent.numberOfPeers = customPeersNum.Text.GetValueDefault(torrent.numberOfPeers);
            currentClient.Key = customKey.Text.GetValueDefault(currentClient.Key);
            torrent.peerID = customPeerID.Text.GetValueDefault(currentClient.PeerID);
            currentClient.PeerID = customPeerID.Text.GetValueDefault(currentClient.PeerID);

            // Add log info
            AddLogLine("TORRENT INFO:");
            AddLogLine("Torrent name: " + currentTorrentFile.Name);
            AddLogLine("Tracker address: " + torrent.tracker);
            AddLogLine("Hash code: " + torrent.hash);
            AddLogLine("Upload rate: " + torrent.uploadRate / 1024);
            AddLogLine("Download rate: " + torrent.downloadRate / 1024);
            AddLogLine("Update interval: " + torrent.interval);
            AddLogLine("Size: " + size / 1024);
            AddLogLine("Left: " + torrent.totalsize / 1024);
            AddLogLine("Finished: " + finishedPercent);
            AddLogLine("Filename: " + torrent.filename);
            AddLogLine("Number of peers: " + torrent.numberOfPeers);
            AddLogLine("Port: " + torrent.port);
            AddLogLine("Key: " + torrent.key);
            AddLogLine("PeerID: " + torrent.peerID + "\n" + "\n");
            return torrent;
        }

        internal void openFileDialog1_FileOk(object sender, CancelEventArgs e)
        {
            try
            {
                if (openFileDialog1.FileName == "") return;
                loadTorrentFileInfo(openFileDialog1.FileName);
                FileInfo file = new FileInfo(openFileDialog1.FileName);
                DefaultDirectory = file.DirectoryName;
            }
            catch { return; }
        }

        #endregion
        #region Buttons
        internal void closeButton_Click(object sender, EventArgs e)
        {
            ExitRatioMaster();
        }

        internal void StartButton_Click(object sender, EventArgs e)
        {
            if (!StartButton.Enabled) return;
            Seeders = -1;
            Leechers = -1;
            if (trackerAddress.Text == "" || shaHash.Text == "" || txtTorrentSize.Text == "")
            {
                MessageBox.Show("Please select valid torrent file!", "RatioMaster.NET " + version + " - ERROR");
                return;
            }

            // Check rem work
            if ((string)cmbStopAfter.SelectedItem == "After time:")
            {
                int res;
                bool bCheck = int.TryParse(txtStopValue.Text, out res);
                if (bCheck == false)
                {
                    MessageBox.Show("Please select valid number for Remaning Work\n\r- 0 - default - never stop\n\r- positive number (greater than 1000)", "RatioMaster.NET " + version + " - ERROR");
                    return;
                }
                else
                {
                    if (res < 1000 && res != 0)
                    {
                        MessageBox.Show("Please select valid number for Remaning Work\n\r- 0 - default - never stop\n\r- positive number (greater than 1000)", "RatioMaster.NET " + version + " - ERROR");
                        return;
                    }
                }
            }

            updateScrapStats("", "", "");
            totalRunningTimeCounter = 0;
            timerValue.Text = "updating...";

            // txtStopValue.Text = res.ToString();
            updateProcessStarted = true;
            seedMode = false;
            requestScrap = checkRequestScrap.Checked;
            updateScrapStats("", "", "");
            StartButton.Enabled = false;
            StartButton.BackColor = SystemColors.Control;
            StopButton.Enabled = true;
            StopButton.BackColor = Color.Silver;
            manualUpdateButton.Enabled = true;
            manualUpdateButton.BackColor = Color.Silver;
            btnDefault.Enabled = false;
            interval.ReadOnly = true;
            fileSize.ReadOnly = true;
            cmbClient.Enabled = false;
            cmbVersion.Enabled = false;
            trackerAddress.ReadOnly = true;
            browseButton.Enabled = false;
            txtStopValue.Enabled = false;
            cmbStopAfter.Enabled = false;
            customPeersNum.Enabled = false;
            customPort.Enabled = false;
            currentClient = TorrentClientFactory.GetClient(GetClientName());
            currentTorrent = getCurrentTorrent();
            currentProxy = getCurrentProxy();
            AddClientInfo();
            OpenTcpListener();
            Thread myThread = new Thread(startProcess);
            myThread.Name = "startProcess() Thread";
            myThread.Start();
            serverUpdateTimer.Start();
            remWork = 0;
            if ((string)cmbStopAfter.SelectedItem == "After time:") RemaningWork.Start();
            requestScrapeFromTracker(currentTorrent);
        }

        private void stopTimerAndCounters()
        {
            if (StartButton.InvokeRequired)
            {
                stopTimerAndCountersCallback callback1 = stopTimerAndCounters;
                Invoke(callback1, new object[0]);
            }
            else
            {
                Seeders = -1;
                Leechers = -1;
                totalRunningTimeCounter = 0;
                lblTotalTime.Text = "00:00";
                if (StartButton.Enabled) return;
                StartButton.Enabled = true;
                StopButton.Enabled = false;
                manualUpdateButton.Enabled = false;
                StartButton.BackColor = Color.Silver;
                StopButton.BackColor = SystemColors.Control;
                manualUpdateButton.BackColor = SystemColors.Control;
                btnDefault.Enabled = true;
                interval.ReadOnly = false;
                fileSize.ReadOnly = false;
                cmbClient.Enabled = true;
                cmbVersion.Enabled = true;
                trackerAddress.ReadOnly = false;
                browseButton.Enabled = true;
                txtStopValue.Enabled = true;
                cmbStopAfter.Enabled = true;
                customPeersNum.Enabled = true;
                customPort.Enabled = true;
                serverUpdateTimer.Stop();
                CloseTcpListener();
                temporaryIntervalCounter = 0;
                timerValue.Text = "stopped";
                currentTorrent.numberOfPeers = "0";
                updateProcessStarted = false;
                RemaningWork.Stop();
                remWork = 0;
            }
        }

        internal void StopButton_Click(object sender, EventArgs e)
        {
            if (!StopButton.Enabled) return;
            stopTimerAndCounters();
            Thread thread1 = new Thread(stopProcess);
            thread1.Name = "stopProcess() Thread";
            thread1.Start();
        }

        internal void manualUpdateButton_Click(object sender, EventArgs e)
        {
            if (!manualUpdateButton.Enabled) return;
            if (updateProcessStarted)
            {
                OpenTcpListener();
                temporaryIntervalCounter = currentTorrent.interval;
            }
        }

        internal void browseButton_Click(object sender, EventArgs e)
        {
            openFileDialog1.InitialDirectory = DefaultDirectory;
            openFileDialog1.ShowDialog();
        }

        internal void clearLogButton_Click(object sender, EventArgs e)
        {
            ClearLog();
        }

        internal void btnDefault_Click(object sender, EventArgs e)
        {
            getnew = false;                
            cmbClient.SelectedItem = DefaultClient;
            cmbVersion.SelectedItem = DefaultClientVersion;

            // custom
            chkNewValues.Checked = true;
            SetCustomValues();
            customPort.Text = "";
            customPeersNum.Text = "";

            // proxy
            comboProxyType.SelectedItem = "None";
            textProxyHost.Text = "";
            textProxyPass.Text = "";
            textProxyPort.Text = "";
            textProxyUser.Text = "";

            // check
            checkRequestScrap.Checked = true;
            checkTCPListen.Checked = true;

            // Options
            TorrentInfo torrent = new TorrentInfo(0, 0);
            int defup = (int)(torrent.uploadRate / 1024);
            int defd = (int)(torrent.downloadRate / 1024);
            uploadRate.Text = defup.ToString();
            downloadRate.Text = defd.ToString();
            fileSize.Text = "0";
            interval.Text = torrent.interval.ToString();

            // Log
            checkLogEnabled.Checked = true;

            // Random speeds
            chkRandUP.Checked = true;
            chkRandDown.Checked = true;
            txtRandUpMin.Text = "1";
            txtRandUpMax.Text = "10";
            txtRandDownMin.Text = "1";
            txtRandDownMax.Text = "10";

            // Random in next update
            checkRandomDownload.Checked = false;
            checkRandomUpload.Checked = false;
            RandomUploadFrom.Text = "10";
            RandomUploadTo.Text = "50";
            RandomDownloadFrom.Text = "10";
            RandomDownloadTo.Text = "100";

            // Other
            txtStopValue.Text = "0";
        }

        internal void btnSaveLog_Click(object sender, EventArgs e)
        {
            SaveLog.ShowDialog();
        }

        #endregion
        #region Send Event To Tracker
        private bool haveInitialPeers;

        private bool sendEventToTracker(TorrentInfo torrentInfo, string eventType)
        {
            scrapStatsUpdated = false;
            currentTorrent = torrentInfo;
            string urlString = getUrlString(torrentInfo, eventType);
            ValueDictionary dictionary1;
            try
            {
                Uri uri = new Uri(urlString);
                TrackerResponse trackerResponse = MakeWebRequestEx(uri);
                if (trackerResponse != null && trackerResponse.Dict != null)
                {
                    dictionary1 = trackerResponse.Dict;
                    string failure = BEncode.String(dictionary1["failure reason"]);
                    if (failure.Length > 0)
                    {
                        AddLogLine("Tracker Error: " + failure);
                        if (!checkIgnoreFailureReason.Checked)
                        {
                            StopButton_Click(null, null);
                            AddLogLine("Stopped because of tracker error!!!");
                            return false;
                        }
                    }
                    else
                    {
                        foreach (string key in trackerResponse.Dict.Keys)
                        {
                            if (key != "failure reason" && key != "peers")
                            {
                                AddLogLine(key + ": " + BEncode.String(trackerResponse.Dict[key]));
                            }
                        }

                        if (dictionary1.Contains("interval"))
                        {
                            updateInterval(BEncode.String(dictionary1["interval"]));
                        }

                        if (dictionary1.Contains("complete") && dictionary1.Contains("incomplete"))
                        {
                            if (dictionary1.Contains("complete") && dictionary1.Contains("incomplete"))
                            {
                                updateScrapStats(BEncode.String(dictionary1["complete"]), BEncode.String(dictionary1["incomplete"]), "");

                                decimal leechers = BEncode.String(dictionary1["incomplete"]).ParseValidInt(0);
                                if (leechers == 0 && noLeechers.Checked)
                                {
                                    AddLogLine("Min number of leechers reached... setting upload speed to 0");
                                    updateTextBox(uploadRate, "0");
                                    chkRandUP.Checked = false;
                                }
                            }
                        }

                        if (dictionary1.Contains("peers"))
                        {
                            haveInitialPeers = true;
                            string text4;
                            if (dictionary1["peers"] is ValueString)
                            {
                                text4 = BEncode.String(dictionary1["peers"]);
                                Encoding encoding1 = Encoding.GetEncoding(0x6faf);
                                byte[] buffer1 = encoding1.GetBytes(text4);
                                BinaryReader reader1 = new BinaryReader(new MemoryStream(encoding1.GetBytes(text4)));
                                PeerList list1 = new PeerList();
                                for (int num1 = 0; num1 < buffer1.Length; num1 += 6)
                                {
                                    list1.Add(new Peer(reader1.ReadBytes(4), reader1.ReadInt16()));
                                }

                                reader1.Close();
                                AddLogLine("peers: " + list1);
                            }
                            else if (dictionary1["peers"] is ValueList)
                            {
                                // text4 = "";
                                ValueList list2 = (ValueList)dictionary1["peers"];
                                PeerList list3 = new PeerList();
                                foreach (object obj1 in list2)
                                {
                                    if (obj1 is ValueDictionary)
                                    {
                                        ValueDictionary dictionary2 = (ValueDictionary)obj1;
                                        list3.Add(new Peer(BEncode.String(dictionary2["ip"]), BEncode.String(dictionary2["port"]), BEncode.String(dictionary2["peer id"])));
                                    }
                                }

                                AddLogLine("peers: " + list3);
                            }
                            else
                            {
                                text4 = BEncode.String(dictionary1["peers"]);
                                AddLogLine("peers(x): " + text4);
                            }
                        }
                    }

                    return false;
                }
                else
                {
                    AddLogLine("No connection in sendEventToTracker() !!!");
                    return false;
                }
            }
            catch (Exception ex)
            {
                AddLogLine("Error in sendEventToTracker(): " + ex.Message);
                return false;
            }
        }

        private delegate void stopTimerAndCountersCallback();

        delegate void SetIntervalCallback(string param);

        internal void updateInterval(string param)
        {
            if (interval.InvokeRequired)
            {
                SetIntervalCallback del = updateInterval;
                Invoke(del, new object[] { param });
            }
            else
            {
                if (updateProcessStarted)
                {
                    int temp;
                    bool bParse = int.TryParse(param, out temp);
                    if (bParse)
                    {
                        if (temp > 3600) temp = 3600;
                        if (temp < 60) temp = 60;
                        currentTorrent.interval = temp;
                        AddLogLine("Updating Interval: " + temp);
                        interval.ReadOnly = false;
                        interval.Text = temp.ToString();
                        interval.ReadOnly = true;
                    }
                }
            }
        }

        private static long RoundByDenominator(long value, long denominator)
        {
            return (denominator * (value / denominator));
        }

        private string getUrlString(TorrentInfo torrentInfo, string eventType)
        {
            // Random random = new Random();
            string uploaded = "0";
            if (torrentInfo.uploaded > 0)
            {
                torrentInfo.uploaded = RoundByDenominator(torrentInfo.uploaded, 0x4000);
                uploaded = torrentInfo.uploaded.ToString();

                // uploaded = Convert.ToString(torrentInfo.uploaded + random.Next(1, 1023));
            }

            string downloaded = "0";
            if (torrentInfo.downloaded > 0)
            {
                torrentInfo.downloaded = RoundByDenominator(torrentInfo.downloaded, 0x10);
                downloaded = torrentInfo.downloaded.ToString();

                // downloaded = Convert.ToString(torrentInfo.downloaded + random.Next(1, 1023));
            }

            if (torrentInfo.left > 0)
            {
                torrentInfo.left = torrentInfo.totalsize - torrentInfo.downloaded;
            }

            string left = torrentInfo.left.ToString();
            string key = torrentInfo.key;
            string port = torrentInfo.port;
            string peerID = torrentInfo.peerID;
            string urlString;
            urlString = torrentInfo.tracker;
            if (urlString.Contains("?"))
            {
                urlString += "&";
            }
            else
            {
                urlString += "?";
            }

            if (eventType.Contains("started")) urlString = urlString.Replace("&natmapped=1&localip={localip}", "");
            if (!eventType.Contains("stopped")) urlString = urlString.Replace("&trackerid=48", "");
            urlString += currentClient.Query;
            urlString = urlString.Replace("{infohash}", HashUrlEncode(torrentInfo.hash, currentClient.HashUpperCase));
            urlString = urlString.Replace("{peerid}", peerID);
            urlString = urlString.Replace("{port}", port);
            urlString = urlString.Replace("{uploaded}", uploaded);
            urlString = urlString.Replace("{downloaded}", downloaded);
            urlString = urlString.Replace("{left}", left);
            urlString = urlString.Replace("{event}", eventType);
            if ((torrentInfo.numberOfPeers == "0") && !eventType.ToLower().Contains("stopped")) torrentInfo.numberOfPeers = "200";
            urlString = urlString.Replace("{numwant}", torrentInfo.numberOfPeers);
            urlString = urlString.Replace("{key}", key);
            urlString = urlString.Replace("{localip}", Functions.GetIp());
            return urlString;
        }

        #endregion
        #region Scrape
        private void requestScrapeFromTracker(TorrentInfo torrentInfo)
        {
            Seeders = -1;
            Leechers = -1;
            if (checkRequestScrap.Checked && !scrapStatsUpdated)
            {
                try
                {
                    string text1 = getScrapeUrlString(torrentInfo);
                    if (text1 == "")
                    {
                        AddLogLine("This tracker doesnt seem to support scrape");
                    }

                    Uri uri1 = new Uri(text1);
                    TrackerResponse response1 = MakeWebRequestEx(uri1);
                    if ((response1 != null) && (response1.Dict != null))
                    {
                        string text2 = BEncode.String(response1.Dict["failure reason"]);
                        if (text2.Length > 0)
                        {
                            AddLogLine("Tracker Error: " + text2);
                        }
                        else
                        {
                            AddLogLine("---------- Scrape Info -----------");
                            ValueDictionary dictionary1 = (ValueDictionary)response1.Dict["files"];
                            string text3 = Encoding.GetEncoding(0x4e4).GetString(currentTorrentFile.InfoHash);
                            if (dictionary1[text3].GetType() == typeof(ValueDictionary))
                            {
                                ValueDictionary dictionary2 = (ValueDictionary)dictionary1[text3];
                                AddLogLine("complete: " + BEncode.String(dictionary2["complete"]));
                                AddLogLine("downloaded: " + BEncode.String(dictionary2["downloaded"]));
                                AddLogLine("incomplete: " + BEncode.String(dictionary2["incomplete"]));
                                updateScrapStats(BEncode.String(dictionary2["complete"]), BEncode.String(dictionary2["incomplete"]), BEncode.String(dictionary2["downloaded"]));
                                decimal leechers = BEncode.String(dictionary2["incomplete"]).ParseValidInt(-1);
                                if (Leechers != -1  && (leechers == 0) && noLeechers.Checked)
                                {
                                    AddLogLine("Min number of leechers reached... setting upload speed to 0");
                                    updateTextBox(uploadRate, "0");
                                    chkRandUP.Checked = false;
                                }
                            }
                            else
                            {
                                AddLogLine("Scrape returned : '" + ((ValueString)dictionary1[text3]).String + "'");
                            }
                        }
                    }
                }
                catch (Exception exception1)
                {
                    AddLogLine("Scrape Error: " + exception1.Message);
                }
            }
        }

        internal string getScrapeUrlString(TorrentInfo torrentInfo)
        {
            string urlString;
            urlString = torrentInfo.tracker;
            int index = urlString.LastIndexOf("/");
            if (urlString.Substring(index + 1, 8).ToLower() != "announce")
            {
                return "";
            }

            urlString = urlString.Substring(0, index + 1) + "scrape" + urlString.Substring(index + 9);
            string hash = HashUrlEncode(torrentInfo.hash, currentClient.HashUpperCase);
            if (urlString.Contains("?"))
            {
                urlString = urlString + "&";
            }
            else
            {
                urlString = urlString + "?";
            }

            return (urlString + "info_hash=" + hash);
        }

        #endregion
        #region Update Counters
        delegate void SetCountersCallback(TorrentInfo torrentInfo);

        private void updateCounters(TorrentInfo torrentInfo)
        {
            try
            {
                // Random random = new Random();
                // modify Upload Rate
                uploadCount.Text = FormatFileSize((ulong)torrentInfo.uploaded);
                Int64 uploadedR = torrentInfo.uploadRate + RandomSP(txtRandUpMin.Text, txtRandUpMax.Text, chkRandUP.Checked);

                // Int64 uploadedR = torrentInfo.uploadRate + (Int64)random.Next(10 * 1024) - 5 * 1024;
                if (uploadedR < 0) { uploadedR = 0; }
                torrentInfo.uploaded += uploadedR;

                // modify Download Rate
                downloadCount.Text = FormatFileSize((ulong)torrentInfo.downloaded);
                if (!seedMode && torrentInfo.downloadRate > 0)    // dont update download stats
                {
                    Int64 downloadedR = torrentInfo.downloadRate + RandomSP(txtRandDownMin.Text, txtRandDownMax.Text, chkRandDown.Checked);

                    // Int64 downloadedR = torrentInfo.downloadRate + (Int64)random.Next(10 * 1024) - 5 * 1024;
                    if (downloadedR < 0) { downloadedR = 0; }
                    torrentInfo.downloaded += downloadedR;
                    torrentInfo.left = torrentInfo.totalsize - torrentInfo.downloaded;
                }

                if (torrentInfo.left <= 0) // either seedMode or start seed mode
                {
                    torrentInfo.downloaded = torrentInfo.totalsize;
                    torrentInfo.left = 0;
                    torrentInfo.downloadRate = 0;
                    if (!seedMode)
                    {
                        currentTorrent = torrentInfo;
                        seedMode = true;
                        temporaryIntervalCounter = 0;
                        Thread myThread = new Thread(completedProcess);
                        myThread.Name = "completedProcess() Thread";
                        myThread.Start();
                    }
                }

                torrentInfo.interval = int.Parse(interval.Text);
                currentTorrent = torrentInfo;
                double finishedPercent;
                if (torrentInfo.totalsize == 0)
                {
                    fileSize.Text = "100";
                }
                else
                {
                    // finishedPercent = (((((float)currentTorrentFile.totalLength - (float)torrentInfo.totalsize) + (float)torrentInfo.downloaded) / (float)currentTorrentFile.totalLength) * 100);
                    finishedPercent = (((currentTorrentFile.totalLength - (float)torrentInfo.left)) / ((float)currentTorrentFile.totalLength)) * 100.0;
                    fileSize.Text = (finishedPercent >= 100) ? "100" : SetPrecision(finishedPercent.ToString(), 2);
                }

                downloadCount.Text = FormatFileSize((ulong)torrentInfo.downloaded);

                // modify Ratio Lable
                if (torrentInfo.downloaded / 1024 < 100)
                {
                    lblTorrentRatio.Text = "NaN";
                }
                else
                {
                    float data = torrentInfo.uploaded / (float)torrentInfo.downloaded;
                    lblTorrentRatio.Text = SetPrecision(data.ToString(), 2);
                }
            }
            catch (Exception e)
            {
                AddLogLine(e.Message);
                SetCountersCallback d = updateCounters;
                Invoke(d, new object[] { torrentInfo });
            }
        }

        private static string SetPrecision(string data, int prec)
        {
            float pow = (float)Math.Pow(10, prec);
            float wdata = float.Parse(data);
            wdata = wdata * pow;
            int curr = (int)wdata;
            wdata = curr / pow;
            return wdata.ToString();
        }

        int Seeders = -1;
        int Leechers = -1;

        internal void updateScrapStats(string seedStr, string leechStr, string finishedStr)
        {
            try
            {
                Seeders = int.Parse(seedStr);
                Leechers = int.Parse(leechStr);
            }
            catch(Exception)
            {
                Seeders = -1;
                Leechers = -1;
            }

            // if (seedLabel.InvokeRequired)
            // {
            //    updateScrapCallback d = new updateScrapCallback(updateScrapStats);
            //    Invoke(d, new object[] { seedStr, leechStr, finishedStr });
            // }
            // else
            // {
                seedLabel.Text = "Seeders: " + seedStr;
                leechLabel.Text = "Leechers: " + leechStr;
                scrapStatsUpdated = true;

                // AddLogLine("Scrap Stats Updated" + "\n" + "\n");
            // }
        }

        internal void StopModule()
        {
            try
            {
                if ((string)cmbStopAfter.SelectedItem == "When seeders <")
                {
                    if (Seeders > -1 && Seeders < int.Parse(txtStopValue.Text)) StopButton_Click(null, null);
                }

                if ((string)cmbStopAfter.SelectedItem == "When leechers <")
                {
                    if (Leechers > -1 && Leechers < int.Parse(txtStopValue.Text)) StopButton_Click(null, null);
                }

                if ((string)cmbStopAfter.SelectedItem == "When uploaded >")
                {
                    if (currentTorrent.uploaded > long.Parse(txtStopValue.Text) * 1024 * 1024) StopButton_Click(null, null);
                }

                if ((string)cmbStopAfter.SelectedItem == "When downloaded >")
                {
                    if (currentTorrent.downloaded > int.Parse(txtStopValue.Text) * 1024 * 1024) StopButton_Click(null, null);
                }

                if ((string)cmbStopAfter.SelectedItem == "When leechers/seeders <")
                {
                    if ((Leechers / (double)Seeders) < double.Parse(txtStopValue.Text)) StopButton_Click(null, null);
                }
            }
            catch (Exception ex)
            {
                AddLogLine("Error in stopping module!!!: " + ex.Message);
                return;
            }
        }

        internal int totalRunningTimeCounter;

        internal void serverUpdateTimer_Tick(object sender, EventArgs e)
        {
            if (updateProcessStarted)
            {
                if (haveInitialPeers)
                {
                    updateCounters(currentTorrent);
                }

                int num1 = currentTorrent.interval - temporaryIntervalCounter;
                totalRunningTimeCounter++;
                lblTotalTime.Text = ConvertToTime(totalRunningTimeCounter);
                StopModule();
                if (num1 > 0)
                {
                    temporaryIntervalCounter++;
                    timerValue.Text = ConvertToTime(num1);
                }
                else
                {
                    randomiseSpeeds();
                    OpenTcpListener();
                    Thread thread1 = new Thread(continueProcess);
                    temporaryIntervalCounter = 0;
                    timerValue.Text = "0";
                    thread1.Name = "continueProcess() Thread";
                    thread1.Start();
                }
            }
        }

        internal void randomiseSpeeds()
        {
            try
            {
                if (checkRandomUpload.Checked)
                {
                    uploadRate.Text = (RandomSP(RandomUploadFrom.Text, RandomUploadTo.Text, true) / 1024).ToString();

                    // uploadRate.Text = ((int)random1.Next(int.Parse(RandomUploadFrom.Text), int.Parse(RandomUploadTo.Text)) + (int)single1).ToString();
                }

                if (checkRandomDownload.Checked)
                {
                    downloadRate.Text = (RandomSP(RandomDownloadFrom.Text, RandomDownloadTo.Text, true) / 1024).ToString();

                    // downloadRate.Text = ((int)random1.Next(int.Parse(RandomDownloadFrom.Text), int.Parse(RandomDownloadTo.Text)) + (int)single2).ToString();
                }
            }
            catch (Exception exception1)
            {
                AddLogLine("Failed to randomise upload/download speeds: " + exception1.Message);
            }
        }

        internal int RandomSP(string min, string max, bool ret)
        {
            if (ret == false) return rand.Next(10);
            int minn = int.Parse(min);
            int maxx = int.Parse(max);
            int rett = rand.Next(GetMin(minn, maxx), GetMax(minn, maxx)) * 1024;
            return rett;
        }

        internal static int GetMin(int p1, int p2)
        {
            if (p1 < p2) return p1;
            else return p2;
        }

        internal static int GetMax(int p1, int p2)
        {
            if (p1 > p2) return p1;
            else return p2;
        }

        #endregion
        #region Help functions
        private delegate void updateTextBoxCallback(TextBox textbox, string text);

        internal void updateTextBox(TextBox textbox, string text)
        {
            if (textbox.InvokeRequired)
            {
                updateTextBoxCallback callback1 = updateTextBox;
                Invoke(callback1, new object[] { textbox, text });
            }
            else
            {
                textbox.Text = text;
            }
        }

        private delegate void updateLabelCallback(Label textbox, string text);

        private void updateTextBox(Label textbox, string text)
        {
            if (textbox.InvokeRequired)
            {
                updateLabelCallback callback1 = updateTextBox;
                Invoke(callback1, new object[] { textbox, text });
            }
            else
            {
                textbox.Text = text;
            }
        }

        internal static string FormatFileSize(ulong fileSize)
        {
            if (fileSize < 0)
            {
                throw new ArgumentOutOfRangeException("fileSize");
            }

            if (fileSize >= 0x40000000)
            {
                return string.Format("{0:########0.00} GB", ((double)fileSize) / 1073741824);
            }

            if (fileSize >= 0x100000)
            {
                return string.Format("{0:####0.00} MB", ((double)fileSize) / 1048576);
            }

            if (fileSize >= 0x400)
            {
                return string.Format("{0:####0.00} KB", ((double)fileSize) / 1024);
            }

            return string.Format("{0} bytes", fileSize);
        }

        internal static string ToHexString(byte[] bytes)
        {
            char[] hexDigits = {
                    '0', '1', '2', '3', '4', '5', '6', '7',
                    '8', '9', 'A', 'B', 'C', 'D', 'E', 'F'
                    };

            char[] chars = new char[bytes.Length * 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                int b = bytes[i];
                chars[i * 2] = hexDigits[b >> 4];
                chars[i * 2 + 1] = hexDigits[b & 0xF];
            }

            return new string(chars);
        }

        internal static string ConvertToTime(int seconds)
        {
            string ret;
            if (seconds < 60 * 60)
            {
                ret = (seconds / 60).ToString("00") + ":" + (seconds % 60).ToString("00");
            }
            else
            {
                ret = (seconds / (60 * 60)).ToString("00") + ":" + ((seconds % (60 * 60)) / 60).ToString("00") + ":" + (seconds % 60).ToString("00");
            }

            return ret;
        }

        internal string HashUrlEncode(string decoded, bool upperCase)
        {
            StringBuilder ret = new StringBuilder();
            RandomStringGenerator stringGen = new RandomStringGenerator();
            try
            {
                for (int i = 0; i < decoded.Length; i = i + 2)
                {
                    char tempChar;

                    // the only case in which something should not be escaped, is when it is alphanum,
                    // or it's in marks
                    // in all other cases, encode it.
                    tempChar = (char)Convert.ToUInt16(decoded.Substring(i, 2), 16);
                    ret.Append(tempChar);
                }
            }
            catch (Exception ex)
            {
                AddLogLine(ex.ToString());
            }

            return stringGen.Generate(ret.ToString(), upperCase);
        }

        #endregion
        internal SocketEx createSocket()
        {
            // create SocketEx object according to proxy settings
            SocketEx sock = null;
            try
            {
                sock = new SocketEx(currentProxy.ProxyType, currentProxy.ProxyServer, currentProxy.ProxyPort, currentProxy.ProxyUser, currentProxy.ProxyPassword);
                sock.SetTimeout(0x30d40);
            }
            catch (Exception sockError)
            {
                AddLogLine("createSocket error: " + sockError.Message);
            }

            return sock;
        }

        private ProxyInfo getCurrentProxy()
        {
            Encoding _usedEnc = Encoding.GetEncoding(0x4e4);
            ProxyInfo curProxy = new ProxyInfo();
            switch (comboProxyType.SelectedIndex)
            {
                case 0:
                    curProxy.ProxyType = ProxyType.None;
                    break;
                case 1:
                    curProxy.ProxyType = ProxyType.HttpConnect;
                    break;
                case 2:
                    curProxy.ProxyType = ProxyType.Socks4;
                    break;
                case 3:
                    curProxy.ProxyType = ProxyType.Socks4a;
                    break;
                case 4:
                    curProxy.ProxyType = ProxyType.Socks5;
                    break;
                default:
                    curProxy.ProxyType = ProxyType.None;
                    break;
            }

            curProxy.ProxyServer = textProxyHost.Text;
            curProxy.ProxyPort = textProxyPort.Text.ParseValidInt(0);
            curProxy.ProxyUser = _usedEnc.GetBytes(textProxyUser.Text);
            curProxy.ProxyPassword = _usedEnc.GetBytes(textProxyPass.Text);

            // Add log info
            Encoding enc = System.Text.Encoding.ASCII;
            AddLogLine("PROXY INFO:");
            AddLogLine("proxyType = " + curProxy.ProxyType);
            AddLogLine("proxyServer = " + curProxy.ProxyServer);
            AddLogLine("proxyPort = " + curProxy.ProxyPort);
            AddLogLine("proxyUser = " + enc.GetString(curProxy.ProxyUser));
            AddLogLine("proxyPassword = " + enc.GetString(curProxy.ProxyPassword) + "\n" + "\n");
            return curProxy;
        }

        private TrackerResponse MakeWebRequestEx(Uri reqUri)
        {
            Encoding _usedEnc = Encoding.GetEncoding(0x4e4);
            SocketEx sock = null;
            TrackerResponse trackerResponse;
            try
            {
                string host = reqUri.Host;
                int port = reqUri.Port;
                string path = reqUri.PathAndQuery;
                AddLogLine("Connecting to tracker (" + host + ") in port " + port);
                sock = createSocket();
                sock.PreAuthenticate = false;
                int num2 = 0;
                bool flag1 = false;
                while ((num2 < 5) && !flag1)
                {
                    try
                    {
                        sock.Connect(host, port);
                        flag1 = true;
                        AddLogLine("Connected Successfully");
                        continue;
                    }
                    catch (Exception exception1)
                    {
                        AddLogLine("Exception: " + exception1.Message + "; Type: " + exception1.GetType());
                        AddLogLine("Failed connection attempt: " + num2);
                        num2++;
                        continue;
                    }
                }

                string cmd = "GET " + path + " " + currentClient.HttpProtocol + "\r\n" + currentClient.Headers.Replace("{host}", host) + "\r\n";
                AddLogLine("======== Sending Command to Tracker ========");
                AddLogLine(cmd);
                sock.Send(_usedEnc.GetBytes(cmd));

                // simple reading loop
                // read while have the data
                try
                {
                    byte[] data = new byte[32 * 1024];
                    MemoryStream memStream = new MemoryStream();
                    while (true)
                    {
                        int dataLen = sock.Receive(data);
                        if (0 == dataLen)
                            break;
                        memStream.Write(data, 0, dataLen);
                    }

                    if (memStream.Length == 0)
                    {
                        AddLogLine("Error : Tracker Response is empty");
                        return null;
                    }

                    trackerResponse = new TrackerResponse(memStream);
                    if (trackerResponse.doRedirect)
                    {
                        return MakeWebRequestEx(new Uri(trackerResponse.RedirectionURL));
                    }

                    AddLogLine("======== Tracker Response ========");
                    AddLogLine(trackerResponse.Headers);
                    if (trackerResponse.Dict == null)
                    {
                        AddLogLine("*** Failed to decode tracker response :");
                        AddLogLine(trackerResponse.Body);
                    }

                    memStream.Dispose();
                    return trackerResponse;
                }
                catch (Exception ex)
                {
                    sock.Close();
                    AddLogLine(Environment.NewLine + ex.Message);
                    return null;
                }
            }
            catch (Exception ex)
            {
                if (null != sock) sock.Close();
                AddLogLine("Exception:" + ex.Message);
                return null;
            }

            // if (null != sock) sock.Close();
            // else return null;
        }

        internal void RemaningWork_Tick(object sender, EventArgs e)
        {
            if (txtStopValue.Text == "0")
            {
                return;
            }
            else
            {
                remWork++;
                int RW = int.Parse(txtStopValue.Text);
                int diff = RW - remWork;
                txtRemTime.Text = ConvertToTime(diff);
                if (remWork >= RW)
                {
                    txtRemTime.Text = "0";
                    RemaningWork.Stop();
                    StopButton_Click(null, null);
                }
            }
        }

        #region Process
        internal void stopProcess()
        {
            sendEventToTracker(currentTorrent, "&event=stopped");
        }

        internal void completedProcess()
        {
            sendEventToTracker(currentTorrent, "&event=completed");
            requestScrapeFromTracker(currentTorrent);
        }

        internal void continueProcess()
        {
            sendEventToTracker(currentTorrent, "");
            requestScrapeFromTracker(currentTorrent);
        }

        internal void startProcess()
        {
            if (sendEventToTracker(currentTorrent, "&event=started"))
            {
                updateProcessStarted = true;
                requestScrapeFromTracker(currentTorrent);
            }
        }

        #endregion
        #region Change Speeds
        internal void uploadRate_TextChanged(object sender, EventArgs e)
        {
            if (uploadRate.Text == "")
            {
                currentTorrent.uploadRate = 0;
            }
            else
            {
                TorrentInfo torrent = new TorrentInfo(0, 0);
                currentTorrent.uploadRate = uploadRate.Text.ParseValidInt64(torrent.uploadRate / 1024) * 1024;
            }

            AddLogLine("Upload rate changed to " + (currentTorrent.uploadRate / 1024));
        }

        internal void downloadRate_TextChanged(object sender, EventArgs e)
        {
            if (downloadRate.Text == "")
            {
                currentTorrent.downloadRate = 0;
            }
            else
            {
                TorrentInfo torrent = new TorrentInfo(0, 0);
                currentTorrent.downloadRate = downloadRate.Text.ParseValidInt64(torrent.downloadRate / 1024) * 1024;
            }

            AddLogLine("Download rate changed to " + (currentTorrent.downloadRate / 1024));
        }

        #endregion
        #region Settings
        internal void ReadSettings()
        {
            try
            {
                RegistryKey reg = Registry.CurrentUser.OpenSubKey("Software\\RatioMaster.NET", true);

                // TorrentInfo torrent = new TorrentInfo(0, 0);
                if (reg == null)
                {
                    // The key doesn't exist; create it / open it
                    Registry.CurrentUser.CreateSubKey("Software\\RatioMaster.NET");
                    return;
                }

                string Version = (string)reg.GetValue("Version", "none");
                if (Version == "none")
                {
                    btnDefault_Click(null, null);
                    return;
                }

                chkNewValues.Checked = ItoB((int)reg.GetValue("NewValues", true));
                getnew = false;
                cmbClient.SelectedItem = reg.GetValue("Client", DefaultClient);
                getnew = false;
                cmbVersion.SelectedItem = reg.GetValue("ClientVersion", DefaultClientVersion);
                uploadRate.Text = ((string)reg.GetValue("UploadRate", uploadRate.Text));
                downloadRate.Text = ((string)reg.GetValue("DownloadRate", downloadRate.Text));
                fileSize.Text = (string)reg.GetValue("fileSize", "0");

                // fileSize.Text = "0";
                interval.Text = (reg.GetValue("Interval", interval.Text)).ToString();
                DefaultDirectory = (string)reg.GetValue("Directory", DefaultDirectory);
                checkTCPListen.Checked = ItoB((int)reg.GetValue("TCPlistener", BtoI(checkTCPListen.Checked)));
                checkRequestScrap.Checked = ItoB((int)reg.GetValue("ScrapeInfo", BtoI(checkRequestScrap.Checked)));
                checkLogEnabled.Checked = ItoB((int)reg.GetValue("EnableLog", BtoI(checkLogEnabled.Checked)));

                // Radnom value
                chkRandUP.Checked = ItoB((int)reg.GetValue("GetRandUp", BtoI(chkRandUP.Checked)));
                chkRandDown.Checked = ItoB((int)reg.GetValue("GetRandDown", BtoI(chkRandDown.Checked)));
                txtRandUpMin.Text = (string)reg.GetValue("MinRandUp", txtRandUpMin.Text);
                txtRandUpMax.Text = (string)reg.GetValue("MaxRandUp", txtRandUpMax.Text);
                txtRandDownMin.Text = (string)reg.GetValue("MinRandDown", txtRandDownMin.Text);
                txtRandDownMax.Text = (string)reg.GetValue("MaxRandDown", txtRandDownMax.Text);

                // Custom values
                if (chkNewValues.Checked == false)
                {
                    customKey.Text = (string)reg.GetValue("CustomKey", customKey.Text);
                    customPeerID.Text = (string)reg.GetValue("CustomPeerID", customPeerID.Text);
                    lblGenStatus.Text = "Generation status: " + "using last saved values";
                }
                else
                {
                    SetCustomValues();
                }

                customPort.Text = (string)reg.GetValue("CustomPort", customPort.Text);
                customPeersNum.Text = (string)reg.GetValue("CustomPeers", customPeersNum.Text);

                // Radnom value on next
                checkRandomUpload.Checked = ItoB((int)reg.GetValue("GetRandUpNext", BtoI(checkRandomUpload.Checked)));
                checkRandomDownload.Checked = ItoB((int)reg.GetValue("GetRandDownNext", BtoI(checkRandomDownload.Checked)));
                RandomUploadFrom.Text = (string)reg.GetValue("MinRandUpNext", RandomUploadFrom.Text);
                RandomUploadTo.Text = (string)reg.GetValue("MaxRandUpNext", RandomUploadTo.Text);
                RandomDownloadFrom.Text = (string)reg.GetValue("MinRandDownNext", RandomDownloadFrom.Text);
                RandomDownloadTo.Text = (string)reg.GetValue("MaxRandDownNext", RandomDownloadTo.Text);

                // Stop after...
                cmbStopAfter.SelectedItem = reg.GetValue("StopWhen", "Never");
                txtStopValue.Text = (string)reg.GetValue("StopAfter", txtStopValue.Text);

                // Proxy
                comboProxyType.SelectedItem = reg.GetValue("ProxyType", comboProxyType.SelectedItem);
                textProxyHost.Text = (string)reg.GetValue("ProxyAdress", textProxyHost.Text);
                textProxyUser.Text = (string)reg.GetValue("ProxyUser", textProxyUser.Text);
                textProxyPass.Text = (string)reg.GetValue("ProxyPass", textProxyPass.Text);
                textProxyPort.Text = (string)reg.GetValue("ProxyPort", textProxyPort.Text);
                checkIgnoreFailureReason.Checked = ItoB((int)reg.GetValue("IgnoreFailureReason", BtoI(checkIgnoreFailureReason.Checked)));
            }
            catch (Exception e)
            {
                AddLogLine("Error in ReadSettings(): " + e.Message);
            }
        }

        internal static int BtoI(bool b)
        {
            if (b) return 1;
            else return 0;
        }

        internal static bool ItoB(int param)
        {
            if (param == 0) return false;
            if (param == 1) return true;
            return true;
        }

        #endregion
        #region Custom values
        internal void chkNewValues_CheckedChanged(object sender, EventArgs e)
        {
            if (chkNewValues.Checked)
            {
                SetCustomValues();
            }
        }

        internal void GetRandCustVal()
        {
            string clientname = GetClientName();
            currentClient = TorrentClientFactory.GetClient(clientname);
            customKey.Text = currentClient.Key;
            customPeerID.Text = currentClient.PeerID;
            currentTorrent.port = rand.Next(1025, 65535).ToString();
            customPort.Text = currentTorrent.port;
            currentTorrent.numberOfPeers = currentClient.DefNumWant.ToString();
            customPeersNum.Text = currentTorrent.numberOfPeers;
            lblGenStatus.Text = "Generation status: " + "generated new values for " + clientname;
        }

        internal void SetCustomValues()
        {
            string clientname = GetClientName();
            currentClient = TorrentClientFactory.GetClient(clientname);
            AddLogLine("Client changed: " + clientname);
            if (!currentClient.Parse) GetRandCustVal();
            else
            {
                string searchstring = currentClient.SearchString;
                long maxoffset = currentClient.MaxOffset;
                long startoffset = currentClient.StartOffset;
                string process = currentClient.ProcessName;
                string pversion = cmbVersion.SelectedItem.ToString();
                if (GETDATA(process, pversion, searchstring, startoffset, maxoffset))
                {
                    customKey.Text = currentClient.Key;
                    customPeerID.Text = currentClient.PeerID;
                    customPort.Text = currentTorrent.port;
                    customPeersNum.Text = currentTorrent.numberOfPeers;
                    lblGenStatus.Text = "Generation status: " + clientname + " found! Parsed all values!";
                }
                else
                {
                    GetRandCustVal();
                }
            }
        }

        internal bool GETDATA(string client, string pversion, string SearchString, long startoffset, long maxoffset)
        {
            try
            {
                ProcessMemoryReader pReader;
                long absoluteEndOffset = maxoffset;
                long absoluteStartOffset = startoffset;
                string clientSearchString = SearchString;
                uint bufferSize = 0x10000;
                string currentClientProcessName = client.ToLower();
                long currentOffset;
                Encoding enc = Encoding.ASCII;
                Process process1 = FindProcessByName(currentClientProcessName);
                if (process1 == null)
                {
                    return false;
                }

                currentOffset = absoluteStartOffset;
                pReader = new ProcessMemoryReader();
                pReader.ReadProcess = process1;
                bool flag1 = false;

                // AddLogLine("Debug: before pReader.OpenProcess();");
                pReader.OpenProcess();

                // AddLogLine("Debug: pReader.OpenProcess();");
                while (currentOffset < absoluteEndOffset)
                {
                    long num2;

                    // AddLogLine("Debug: " + currentOffset.ToString());
                    int num1;
                    byte[] buffer1 = pReader.ReadProcessMemory((IntPtr)currentOffset, bufferSize, out num1);

                    // pReader.saveArrayToFile(buffer1, @"D:\Projects\NRPG Ratio\NRPG RatioMaster MULTIPLE\RatioMaster source\bin\Release\tests\test" + currentOffset.ToString() + ".txt");
                    num2 = getStringOffsetInsideArray(buffer1, enc, clientSearchString);
                    if (num2 >= 0)
                    {
                        flag1 = true;
                        string text1 = enc.GetString(buffer1);
                        Match match1 = new Regex("&peer_id=(.+?)(&| )", RegexOptions.Compiled).Match(text1);
                        if (match1.Success)
                        {
                            currentClient.PeerID = match1.Groups[1].ToString();
                            AddLogLine("====> PeerID = " + currentClient.PeerID);
                        }

                        match1 = new Regex("&key=(.+?)(&| )", RegexOptions.Compiled).Match(text1);
                        if (match1.Success)
                        {
                            currentClient.Key = match1.Groups[1].ToString();
                            AddLogLine("====> Key = " + currentClient.Key);
                        }

                        match1 = new Regex("&port=(.+?)(&| )", RegexOptions.Compiled).Match(text1);
                        if (match1.Success)
                        {
                            currentTorrent.port = match1.Groups[1].ToString();
                            AddLogLine("====> Port = " + currentTorrent.port);
                        }

                        match1 = new Regex("&numwant=(.+?)(&| )", RegexOptions.Compiled).Match(text1);
                        if (match1.Success)
                        {
                            currentTorrent.numberOfPeers = match1.Groups[1].ToString();
                            AddLogLine("====> NumWant = " + currentTorrent.numberOfPeers);
                            int res;
                            if (!int.TryParse(currentTorrent.numberOfPeers, out res)) currentTorrent.numberOfPeers = currentClient.DefNumWant.ToString();
                        }

                        num2 += currentOffset;
                        AddLogLine("currentOffset = " + currentOffset);
                        break;
                    }

                    currentOffset += (int)bufferSize;
                }

                pReader.CloseHandle();
                if (flag1)
                {
                    AddLogLine("Search finished successfully!");
                    return true;
                }
                else
                {
                    AddLogLine("Search failed. Make sure that torrent client {" + GetClientName() + "} is running and that at least one torrent is working.");
                    return false;
                }
            }
            catch(Exception ex)
            {
                AddLogLine("Error when parsing: " + ex.Message);
                return false;
            }
        }

        private Process FindProcessByName(string processName)
        {
            AddLogLine("Looking for " + processName + " process...");
            Process[] processArray1 = Process.GetProcessesByName(processName);
            if (processArray1.Length == 0)
            {
                string text1 = "No " + processName + " process found. Make sure that torrent client is running.";
                AddLogLine(text1);
                return null;
            }

            AddLogLine(processName + " process found! ");
            return processArray1[0];
        }

        private static int getStringOffsetInsideArray(byte[] memory, Encoding enc, string clientSearchString)
        {
            return enc.GetString(memory).IndexOf(clientSearchString);
        }

        #endregion
        private void cmbStopAfter_SelectedIndexChanged(object sender, EventArgs e)
        {
            if ((string)cmbStopAfter.SelectedItem == "Never")
            {
                lblStopAfter.Text = "";
                txtStopValue.Text = "";
                txtStopValue.Visible = false;
            }

            if ((string)cmbStopAfter.SelectedItem == "After time:")
            {
                lblStopAfter.Text = "seconds";
                txtStopValue.Text = "3600";
                txtStopValue.Visible = true;
            }

            if ((string)cmbStopAfter.SelectedItem == "When seeders <")
            {
                lblStopAfter.Text = "";
                txtStopValue.Text = "10";
                txtStopValue.Visible = true;
            }

            if ((string)cmbStopAfter.SelectedItem == "When leechers <")
            {
                lblStopAfter.Text = "";
                txtStopValue.Text = "10";
                txtStopValue.Visible = true;
            }

            if ((string)cmbStopAfter.SelectedItem == "When uploaded >")
            {
                lblStopAfter.Text = "Mb";
                txtStopValue.Text = "1024";
                txtStopValue.Visible = true;
            }

            if ((string)cmbStopAfter.SelectedItem == "When downloaded >")
            {
                lblStopAfter.Text = "Mb";
                txtStopValue.Text = "1024";
                txtStopValue.Visible = true;
            }

            if ((string)cmbStopAfter.SelectedItem == "When leechers/seeders <")
            {
                lblStopAfter.Text = "";
                txtStopValue.Text = "1,000";
                txtStopValue.Visible = true;
            }
        }

        #endregion
        public override string ToString()
        {
            return "RatioMaster";
        }

        private void fileSize_TextChanged(object sender, EventArgs e)
        {
            // fileSize.Text = fileSize.Text.Replace('.', ',');
            // fileSize.Select(fileSize.Text.Length, 0);
        }

        private void txtStopValue_TextChanged(object sender, EventArgs e)
        {
            // txtStopValue.Text = txtStopValue.Text.Replace('.', ',');
            // txtStopValue.Select(txtStopValue.Text.Length, 0);
        }
    }
}
