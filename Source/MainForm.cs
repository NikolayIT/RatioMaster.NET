using System;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;
using System.Xml;
using Microsoft.Win32;
using System.IO;

namespace RatioMaster_source
{
    public partial class MainForm : Form
    {
        readonly RMCollection<RM> data = new RMCollection<RM>();
        //RM current;
        int items = 0, allit = 0;
        bool trayIconBaloonIsUp = false;
        private VersionChecker versionChecker = new VersionChecker("");
        internal NewVersionForm verform = new NewVersionForm();
        string Log = "";
        internal MainForm()
        {
            InitializeComponent();
            Text = "RatioMaster.NET " + versionChecker.PublicVersion;
        }
        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Add("");
        }
        private void MainForm_Load(object sender, EventArgs e)
        {
            versionChecker = new VersionChecker(Log);
            if (versionChecker.CheckNewVersion()) verform.ShowDialog();
            txtVersion.Text = versionChecker.PublicVersion;
            txtRemote.Text = versionChecker.RemoteVersion;
            txtLocal.Text = versionChecker.LocalVersion;
            txtReleaseDate.Text = versionChecker.ReleaseDate;
            Log += versionChecker.Log;
            //lblSize.Text = this.Width + "x" + this.Height;
            LoadSettings();
            //trayIcon.Text += versionChecker.PublicVersion;
            //trayIcon.BalloonTipTitle += " " + versionChecker.PublicVersion;
            Add("");
            lblIp.Text = Functions.GetIp();
            tab_TabIndexChanged(null, null);
        }
        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AboutForm About = new AboutForm(versionChecker.PublicVersion);
            About.ShowDialog();
        }
        private void winRestore()
        {
            if (WindowState == FormWindowState.Minimized)
            {
                Show();
                WindowState = FormWindowState.Normal;
                trayIcon.Visible = false;
            }
            // Activate the form.
            Activate();
            Focus();
        }
        private void MainForm_Move(object sender, EventArgs e)
        {
            if (this == null)
            { //This happen on create.
                return;
            }
            //If we are minimizing the form then hide it so it doesn't show up on the task bar
            if (WindowState == FormWindowState.Minimized && chkMinimize.Checked)
            {
                Hide();
                trayIcon.Visible = true;
            }
            else
            {
                //any other windows state show it.
                Show();
            }
            //lblLocation.Text = this.Location.X + "x" + this.Location.Y;
        }
        private void Exit()
        {
            if (GetTabType(tab.SelectedTab) == TabType.RatioMaster) SaveSettings((RM)tab.SelectedTab.Controls[0]);
            foreach (TabPage tabb in tab.TabPages)
            {
                if (GetTabType(tabb) == TabType.RatioMaster) ((RM)tabb.Controls[0]).StopButton_Click(null, null);
            }
            Application.Exit();
            Process.GetCurrentProcess().Kill();
        }
        private static TabType GetTabType(TabPage page)
        {
            //string name = page.Controls[0].ToString();
            return TabType.RatioMaster;
        }
        #region Tray items
        private void trayIcon_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
            {
                winRestore();
            }
        }
        private void restoreToolStripMenuItem_Click(object sender, EventArgs e)
        {
            winRestore();
        }
        private void trayIcon_MouseMove(object sender, MouseEventArgs e)
        {
            if (checkShowTrayBaloon.Checked && trayIconBaloonIsUp == false)
            {
                trayIcon.BalloonTipText = "";
                foreach (TabPage tabb in tab.TabPages)
                {
                    try
                    {
                        if (GetTabType(tabb) == TabType.RatioMaster) trayIcon.BalloonTipText += tabb.Text + " - " + ((RM)tabb.Controls[0]).currentTorrentFile.Name + "\n";
                    }
                    catch
                    {
                        trayIcon.BalloonTipText += tabb.Text + " - Not opened!" + "\n";
                    }
                }
                trayIcon.ShowBalloonTip(0);
            }
        }
        private void goToProgramSiteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start("http://ratiomaster.net");
        }
        private void goToProgramForumToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start("http://nrpg.forumer.com/");
        }
        private void toolStripMenuItem4_Click(object sender, EventArgs e)
        {
            Exit();
        }
        private void trayIcon_BalloonTipClicked(object sender, EventArgs e)
        {
            trayIconBaloonIsUp = false;
        }
        private void trayIcon_BalloonTipClosed(object sender, EventArgs e)
        {
            trayIconBaloonIsUp = false;
        }
        private void trayIcon_BalloonTipShown(object sender, EventArgs e)
        {
            trayIconBaloonIsUp = true;
        }
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Exit();
        }
        #endregion
        #region Tabs
        private void EditCurrent(string FileName)
        {
            ((RM)tab.SelectedTab.Controls[0]).loadTorrentFileInfo(FileName);
        }
        private void Add(string FileName)
        {
            items++;
            allit++;
            RM rm1 = new RM();
            data.Add(rm1);
            //current = rm1;
            TabPage page1 = new TabPage("RM " + allit.ToString());
            page1.Name = "RM" + items.ToString();
            page1.Controls.Add(rm1);
            //page1.Enter += new EventHandler(this.TabPage_Enter);
            //page1.BorderStyle = BorderStyle.FixedSingle;
            //page1.BackColor = Color.White;
            tab.Controls.Add(page1);
            tab.SelectedTab = page1;
            lblTabs.Text = allit.ToString();
            if (FileName != "")
            {
                ((RM)tab.SelectedTab.Controls[0]).loadTorrentFileInfo(FileName);
            }
        }
        private void Remove()
        {
            if (tab.TabPages.Count < 2) return;
            int last = tab.SelectedIndex;
            if (GetTabType(tab.SelectedTab) == TabType.RatioMaster)
            {
                ((RM)tab.SelectedTab.Controls[0]).StopButton_Click(null, null);
                allit--;
            }
            tab.TabPages.Remove(tab.SelectedTab);
            tab.SelectedIndex = last;
            lblTabs.Text = allit.ToString();
        }
        private void RenameTabs()
        {
            int curr = 0;
            foreach (TabPage thetab in tab.TabPages)
            {
                if (thetab.Text.IndexOf("RM ") > -1)
                {
                    curr++;
                    thetab.Text = "RM " + curr;
                }
            }
        }
        private void renameCurrentToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Prompt prompt = new Prompt("Please select new tab name", "Type new tab name:", tab.SelectedTab.Text);
            if (prompt.ShowDialog() == DialogResult.OK) tab.SelectedTab.Text = prompt.Result;
        }
        private void removeCurrentToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Remove();
            RenameTabs();
        }
        #endregion
        private void MainForm_Resize(object sender, EventArgs e)
        {
            tab.Size = new Size(Width - 8, Height - 80);
            //lblSize.Text = this.Width + "x" + this.Height;
        }
        private void goToProgramPageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start("http://ratiomaster.net");
        }
        private void goToSupportForumToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start("http://nrpg.16.forumer.com/");
        }
        private void goToAuthorPageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start("http://nikolay.it");
        }
        private void jOINToOurForumPleaseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start("https://www.paypal.com/cgi-bin/webscr?cmd=_donations&business=PG6NUT5YWYF82&lc=BG&item_name=RatioMaster%2eNET&item_number=RatioMaster%2eNET&currency_code=USD&bn=PP%2dDonationsBF%3abtn_donateCC_LG%2egif%3aNonHosted");
        }
        private void lblCodedBy_Click(object sender, EventArgs e)
        {
            Process.Start("http://nikolay.it/");
        }
        private void LoadSettings()
        {
            RegistryKey reg = Registry.CurrentUser.OpenSubKey("Software\\RatioMaster.NET", true);
            if (reg == null)
            {
                // The key doesn't exist; create it / open it
                Registry.CurrentUser.CreateSubKey("Software\\RatioMaster.NET");
                return;
            }
            try
            {
                checkShowTrayBaloon.Checked = ItoB((int) reg.GetValue("BallonTip", false));
                chkMinimize.Checked = ItoB((int) reg.GetValue("MinimizeToTray", true));
                closeToTrayToolStripMenuItem.Checked = ItoB((int) reg.GetValue("CloseToTray", true));
            }
            catch { }
        }
        private void SaveSettings(RM RMdata)
        {
            try
            {
                RegistryKey reg = Registry.CurrentUser.OpenSubKey("Software\\RatioMaster.NET", true);
                if (reg == null)
                {
                    // The key doesn't exist; create it / open it
                    reg = Registry.CurrentUser.CreateSubKey("Software\\RatioMaster.NET");
                }
                reg.SetValue("Version", versionChecker.PublicVersion, RegistryValueKind.String);
                reg.SetValue("NewValues", BtoI(RMdata.chkNewValues.Checked), RegistryValueKind.DWord);
                reg.SetValue("BallonTip", BtoI(checkShowTrayBaloon.Checked), RegistryValueKind.DWord);
                reg.SetValue("MinimizeToTray", BtoI(chkMinimize.Checked), RegistryValueKind.DWord);
                reg.SetValue("CloseToTray", BtoI(closeToTrayToolStripMenuItem.Checked), RegistryValueKind.DWord);
                reg.SetValue("Client", RMdata.cmbClient.SelectedItem, RegistryValueKind.String);
                reg.SetValue("ClientVersion", RMdata.cmbVersion.SelectedItem, RegistryValueKind.String);
                reg.SetValue("UploadRate", RMdata.uploadRate.Text, RegistryValueKind.String);
                reg.SetValue("DownloadRate", RMdata.downloadRate.Text, RegistryValueKind.String);
                reg.SetValue("Interval", RMdata.interval.Text, RegistryValueKind.String);
                reg.SetValue("fileSize", RMdata.fileSize.Text, RegistryValueKind.String);
                reg.SetValue("Directory", RMdata.DefaultDirectory, RegistryValueKind.String);
                reg.SetValue("TCPlistener", BtoI(RMdata.checkTCPListen.Checked), RegistryValueKind.DWord);
                reg.SetValue("ScrapeInfo", BtoI(RMdata.checkRequestScrap.Checked), RegistryValueKind.DWord);
                reg.SetValue("EnableLog", BtoI(RMdata.checkLogEnabled.Checked), RegistryValueKind.DWord);
                // Radnom value
                reg.SetValue("GetRandUp", BtoI(RMdata.chkRandUP.Checked), RegistryValueKind.DWord);
                reg.SetValue("GetRandDown", BtoI(RMdata.chkRandDown.Checked), RegistryValueKind.DWord);
                reg.SetValue("MinRandUp", RMdata.txtRandUpMin.Text, RegistryValueKind.String);
                reg.SetValue("MaxRandUp", RMdata.txtRandUpMax.Text, RegistryValueKind.String);
                reg.SetValue("MinRandDown", RMdata.txtRandDownMin.Text, RegistryValueKind.String);
                reg.SetValue("MaxRandDown", RMdata.txtRandDownMax.Text, RegistryValueKind.String);
                // Custom values
                reg.SetValue("CustomKey", RMdata.customKey.Text, RegistryValueKind.String);
                reg.SetValue("CustomPeerID", RMdata.customPeerID.Text, RegistryValueKind.String);
                reg.SetValue("CustomPeers", RMdata.customPeersNum.Text, RegistryValueKind.String);
                reg.SetValue("CustomPort", RMdata.customPort.Text, RegistryValueKind.String);
                // Stop after...
                reg.SetValue("StopWhen", RMdata.cmbStopAfter.SelectedItem, RegistryValueKind.String);
                reg.SetValue("StopAfter", RMdata.txtStopValue.Text, RegistryValueKind.String);
                // Proxy
                reg.SetValue("ProxyType", RMdata.comboProxyType.SelectedItem, RegistryValueKind.String);
                reg.SetValue("ProxyAdress", RMdata.textProxyHost.Text, RegistryValueKind.String);
                reg.SetValue("ProxyUser", RMdata.textProxyUser.Text, RegistryValueKind.String);
                reg.SetValue("ProxyPass", RMdata.textProxyPass.Text, RegistryValueKind.String);
                reg.SetValue("ProxyPort", RMdata.textProxyPort.Text, RegistryValueKind.String);
                // Radnom value on next
                reg.SetValue("GetRandUpNext", BtoI(RMdata.checkRandomUpload.Checked), RegistryValueKind.DWord);
                reg.SetValue("GetRandDownNext", BtoI(RMdata.checkRandomDownload.Checked), RegistryValueKind.DWord);
                reg.SetValue("MinRandUpNext", RMdata.RandomUploadFrom.Text, RegistryValueKind.String);
                reg.SetValue("MaxRandUpNext", RMdata.RandomUploadTo.Text, RegistryValueKind.String);
                reg.SetValue("MinRandDownNext", RMdata.RandomDownloadFrom.Text, RegistryValueKind.String);
                reg.SetValue("MaxRandDownNext", RMdata.RandomDownloadTo.Text, RegistryValueKind.String);
                reg.SetValue("IgnoreFailureReason", BtoI(RMdata.checkIgnoreFailureReason.Checked), RegistryValueKind.DWord);
            }
            catch (Exception e)
            {
                Log += "Error in SetSettings(): " + e.Message + "\n";
            }
        }
        internal static Int64 parseValidInt64(string str, Int64 defVal)
        {
            try
            { return Int64.Parse(str); }
            catch (Exception)
            { return defVal; }
        }
        internal static int ParseValidInt(string str, int defVal)
        {
            try
            { return int.Parse(str); }
            catch (Exception)
            { return defVal; }
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
        internal void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (closeToTrayToolStripMenuItem.Checked && chkMinimize.Checked)
            {
                e.Cancel = true;
                WindowState = FormWindowState.Minimized;
                Hide();
                trayIcon.Visible = true;
            }
            else Exit();
        }
        internal void aboutToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            AboutForm About = new AboutForm(versionChecker.PublicVersion);
            About.ShowDialog();
        }
        private void startToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (GetTabType(tab.SelectedTab) == TabType.RatioMaster) ((RM)tab.SelectedTab.Controls[0]).StartButton_Click(null, null);
        }
        private void manualUpdateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (GetTabType(tab.SelectedTab) == TabType.RatioMaster) ((RM)tab.SelectedTab.Controls[0]).manualUpdateButton_Click(null, null);
        }
        private void stopToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (GetTabType(tab.SelectedTab) == TabType.RatioMaster) ((RM)tab.SelectedTab.Controls[0]).StopButton_Click(null, null);
        }
        #region Sessions
        bool startthem = false;
        bool stopthem = false;
        private static void AppendItem(XmlDocument aXmlDoc, XmlElement aXmlElement, string Value, string Name)
        {
            XmlElement itemElement = aXmlDoc.CreateElement(Name);
            itemElement.InnerText = Value;
            aXmlElement.AppendChild(itemElement);
        }
        private static void NewMainItem(XmlDocument aXmlDoc, XmlElement aXmlElement, RM data, string name)
        {
            AppendItem(aXmlDoc, aXmlElement, name, "Name");
            if (data.currentTorrent.filename != null) AppendItem(aXmlDoc, aXmlElement, data.currentTorrent.filename, "Address");
            else AppendItem(aXmlDoc, aXmlElement, data.torrentFile.Text, "Address");
            AppendItem(aXmlDoc, aXmlElement, data.trackerAddress.Text, "Tracker");
            AppendItem(aXmlDoc, aXmlElement, data.uploadRate.Text, "UploadSpeed");
            AppendItem(aXmlDoc, aXmlElement, data.chkRandUP.Checked.ToString(), "UploadRandom");
            AppendItem(aXmlDoc, aXmlElement, data.txtRandUpMin.Text, "UploadRandMin");
            AppendItem(aXmlDoc, aXmlElement, data.txtRandUpMax.Text, "UploadRandMax");
            AppendItem(aXmlDoc, aXmlElement, data.downloadRate.Text, "DownloadSpeed");
            AppendItem(aXmlDoc, aXmlElement, data.chkRandDown.Checked.ToString(), "DownloadRandom");
            AppendItem(aXmlDoc, aXmlElement, data.txtRandDownMin.Text, "DownloadRandMin");
            AppendItem(aXmlDoc, aXmlElement, data.txtRandDownMax.Text, "DownloadRandMax");
            AppendItem(aXmlDoc, aXmlElement, data.cmbClient.SelectedItem.ToString(), "Client");
            AppendItem(aXmlDoc, aXmlElement, data.cmbVersion.SelectedItem.ToString(), "Version");
            AppendItem(aXmlDoc, aXmlElement, data.fileSize.Text, "Finished");
            AppendItem(aXmlDoc, aXmlElement, data.cmbStopAfter.SelectedItem.ToString(), "StopType");
            AppendItem(aXmlDoc, aXmlElement, data.txtStopValue.Text, "StopValue");
            AppendItem(aXmlDoc, aXmlElement, data.customPort.Text, "Port");
            AppendItem(aXmlDoc, aXmlElement, data.checkTCPListen.Checked.ToString(), "UseTCP");
            AppendItem(aXmlDoc, aXmlElement, data.checkRequestScrap.Checked.ToString(), "UseScrape");
            AppendItem(aXmlDoc, aXmlElement, data.comboProxyType.SelectedItem.ToString(), "ProxyType");
            AppendItem(aXmlDoc, aXmlElement, data.textProxyUser.Text, "ProxyUser");
            AppendItem(aXmlDoc, aXmlElement, data.textProxyPass.Text, "ProxyPass");
            AppendItem(aXmlDoc, aXmlElement, data.textProxyHost.Text, "ProxyHost");
            AppendItem(aXmlDoc, aXmlElement, data.textProxyPort.Text, "ProxyPort");
            AppendItem(aXmlDoc, aXmlElement, data.checkRandomUpload.Checked.ToString(), "NextUpdateUpload");
            AppendItem(aXmlDoc, aXmlElement, data.RandomUploadFrom.Text, "NextUpdateUploadFrom");
            AppendItem(aXmlDoc, aXmlElement, data.RandomUploadTo.Text, "NextUpdateUploadTo");
            AppendItem(aXmlDoc, aXmlElement, data.checkRandomDownload.Checked.ToString(), "NextUpdateDownload");
            AppendItem(aXmlDoc, aXmlElement, data.RandomDownloadFrom.Text, "NextUpdateDownloadFrom");
            AppendItem(aXmlDoc, aXmlElement, data.RandomDownloadTo.Text, "NextUpdateDownloadTo");
            AppendItem(aXmlDoc, aXmlElement, data.checkIgnoreFailureReason.Checked.ToString(), "IgnoreFailureReason");
        }
        private void saveCurrentSessionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            stopthem = true;
            saveSession.ShowDialog();
        }
        private void loadSessionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            startthem = false;
            loadSession.ShowDialog();
        }
        private void loadSessionAndStartToolStripMenuItem_Click(object sender, EventArgs e)
        {
            startthem = true;
            loadSession.ShowDialog();
        }
        private void saveCurrentSessionToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            stopthem = false;
            saveSession.ShowDialog();
        }
        private void SaveSession(string Path)
        {
            XmlDocument doc = new XmlDocument();
            XmlElement main = doc.CreateElement("main");
            doc.AppendChild(main);
            foreach (TabPage tabb in tab.TabPages)
            {
                if (GetTabType(tabb) == TabType.RatioMaster)
                {
                    if (stopthem) ((RM)tabb.Controls[0]).StopButton_Click(null, null);
                    XmlElement child = doc.CreateElement("RatioMaster");
                    main.AppendChild(child);
                    NewMainItem(doc, child, (RM)tabb.Controls[0], tabb.Text);
                }
            }
            doc.Save(Path);
        }
        private void LoadSession(string Path)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(Path);
            XmlNode root = doc.DocumentElement;
            foreach (XmlNode node in root.ChildNodes)
            {
                Add("");
                tab.SelectedTab.Text = node["Name"].InnerText;
                ((RM)tab.SelectedTab.Controls[0]).torrentFile.Text = node["Address"].InnerText;
                ((RM)tab.SelectedTab.Controls[0]).openFileDialog1.FileName = node["Address"].InnerText;
                ((RM)tab.SelectedTab.Controls[0]).openFileDialog1_FileOk(null, null);
                ((RM)tab.SelectedTab.Controls[0]).trackerAddress.Text = node["Tracker"].InnerText;
                ((RM)tab.SelectedTab.Controls[0]).uploadRate.Text = node["UploadSpeed"].InnerText;
                ((RM)tab.SelectedTab.Controls[0]).txtRandUpMin.Text = node["UploadRandMin"].InnerText;
                ((RM)tab.SelectedTab.Controls[0]).txtRandUpMax.Text = node["UploadRandMax"].InnerText;
                ((RM)tab.SelectedTab.Controls[0]).downloadRate.Text = node["DownloadSpeed"].InnerText;
                ((RM)tab.SelectedTab.Controls[0]).txtRandDownMin.Text = node["DownloadRandMin"].InnerText;
                ((RM)tab.SelectedTab.Controls[0]).txtRandDownMax.Text = node["DownloadRandMax"].InnerText;
                ((RM)tab.SelectedTab.Controls[0]).chkRandUP.Checked = bool.Parse(node["UploadRandom"].InnerText);
                ((RM)tab.SelectedTab.Controls[0]).chkRandDown.Checked = bool.Parse(node["DownloadRandom"].InnerText);
                ((RM)tab.SelectedTab.Controls[0]).cmbClient.SelectedItem = node["Client"].InnerText;
                ((RM)tab.SelectedTab.Controls[0]).cmbVersion.SelectedItem = node["Version"].InnerText;
                ((RM)tab.SelectedTab.Controls[0]).fileSize.Text = node["Finished"].InnerText;
                ((RM)tab.SelectedTab.Controls[0]).cmbStopAfter.SelectedItem = node["StopType"].InnerText;
                ((RM)tab.SelectedTab.Controls[0]).txtStopValue.Text = node["StopValue"].InnerText;
                ((RM)tab.SelectedTab.Controls[0]).customPort.Text = node["Port"].InnerText;
                ((RM)tab.SelectedTab.Controls[0]).checkTCPListen.Checked = bool.Parse(node["UseTCP"].InnerText);
                ((RM)tab.SelectedTab.Controls[0]).checkRequestScrap.Checked = bool.Parse(node["UseScrape"].InnerText);
                ((RM)tab.SelectedTab.Controls[0]).comboProxyType.SelectedItem = node["ProxyType"].InnerText;
                ((RM)tab.SelectedTab.Controls[0]).textProxyUser.Text = node["ProxyUser"].InnerText;
                ((RM)tab.SelectedTab.Controls[0]).textProxyPass.Text = node["ProxyPass"].InnerText;
                ((RM)tab.SelectedTab.Controls[0]).textProxyHost.Text = node["ProxyHost"].InnerText;
                ((RM)tab.SelectedTab.Controls[0]).textProxyPort.Text = node["ProxyPort"].InnerText;
                ((RM)tab.SelectedTab.Controls[0]).checkRandomUpload.Checked = bool.Parse(node["NextUpdateUpload"].InnerText);
                ((RM)tab.SelectedTab.Controls[0]).checkRandomDownload.Checked = bool.Parse(node["NextUpdateDownload"].InnerText);
                ((RM)tab.SelectedTab.Controls[0]).RandomUploadFrom.Text = node["NextUpdateUploadFrom"].InnerText;
                ((RM)tab.SelectedTab.Controls[0]).RandomUploadTo.Text = node["NextUpdateUploadTo"].InnerText;
                ((RM)tab.SelectedTab.Controls[0]).RandomDownloadFrom.Text = node["NextUpdateDownloadFrom"].InnerText;
                ((RM)tab.SelectedTab.Controls[0]).RandomDownloadTo.Text = node["NextUpdateDownloadTo"].InnerText;
                ((RM)tab.SelectedTab.Controls[0]).checkIgnoreFailureReason.Checked = bool.Parse(node["IgnoreFailureReason"].InnerText);
                if (startthem) ((RM)tab.SelectedTab.Controls[0]).StartButton_Click(null, null);
            }
            RenameTabs();
        }
        private void saveSession_FileOk(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveSession(saveSession.FileName);
        }
        private void loadSession_FileOk(object sender, System.ComponentModel.CancelEventArgs e)
        {
            LoadSession(loadSession.FileName);
        }
        #endregion
        #region All RatioMasters menu
        private void startToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            foreach (TabPage tabb in tab.TabPages)
            {
                if (GetTabType(tabb) == TabType.RatioMaster) ((RM)tabb.Controls[0]).StartButton_Click(null, null);
            }
        }
        private void stopToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            foreach (TabPage tabb in tab.TabPages)
            {
                if (GetTabType(tabb) == TabType.RatioMaster) ((RM)tabb.Controls[0]).StopButton_Click(null, null);
            }
        }
        private void updateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (TabPage tabb in tab.TabPages)
            {
                if (GetTabType(tabb) == TabType.RatioMaster) ((RM)tabb.Controls[0]).manualUpdateButton_Click(null, null);
            }
        }
        private void clearAllLogsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (TabPage tabb in tab.TabPages)
            {
                if (GetTabType(tabb) == TabType.RatioMaster) ((RM)tabb.Controls[0]).clearLogButton_Click(null, null);
            }
        }
        private void setUploadSpeedToToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Prompt prompt = new Prompt("Please type valid integer value", "Type new upload speed for all tabs:", "100");
            if (prompt.ShowDialog() == DialogResult.OK)
            {
                int value;
                try
                {
                    value = int.Parse(prompt.Result);
                }
                catch
                {
                    MessageBox.Show("Invalid value!\nTry again!", "Error");
                    return;
                }
                foreach (TabPage tabb in tab.TabPages)
                {
                    if (GetTabType(tabb) == TabType.RatioMaster) ((RM)tabb.Controls[0]).updateTextBox(((RM)tabb.Controls[0]).uploadRate, value.ToString());
                }
            }
        }
        private void setDownloadSpeedToToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Prompt prompt = new Prompt("Please type valid integer value", "Type new download speed for all tabs:", "100");
            if (prompt.ShowDialog() == DialogResult.OK)
            {
                int value;
                try
                {
                    value = int.Parse(prompt.Result);
                }
                catch
                {
                    MessageBox.Show("Invalid value!\nTry again!", "Error");
                    return;
                }
                foreach (TabPage tabb in tab.TabPages)
                {
                    if (GetTabType(tabb) == TabType.RatioMaster) ((RM)tabb.Controls[0]).updateTextBox(((RM)tabb.Controls[0]).downloadRate, value.ToString());
                }
            }
        }
        #endregion
        private void saveSettingsFromCurrentTabToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (GetTabType(tab.SelectedTab) == TabType.RatioMaster) SaveSettings((RM)tab.SelectedTab.Controls[0]);
        }
        private void tab_TabIndexChanged(object sender, EventArgs e)
        {
            /*
            if (GetTabType(tab.SelectedTab) == TabType.RatioMaster)
            {
                currentToolStripMenuItem.Enabled = true;
                allRatioMastersToolStripMenuItem.Enabled = true;
                saveSettingsFromCurrentTabToolStripMenuItem.Enabled = true;
                newToolStripMenuItem.Enabled = true;
                browserToolStripMenuItem.Enabled = false;
            }
            else if (GetTabType(tab.SelectedTab) == TabType.Browser)
            {
                currentToolStripMenuItem.Enabled = false;
                allRatioMastersToolStripMenuItem.Enabled = false;
                saveSettingsFromCurrentTabToolStripMenuItem.Enabled = false;
                newToolStripMenuItem.Enabled = false;
                browserToolStripMenuItem.Enabled = true;
            }
             */
        }
        private static string GetFileExtenstion(string File)
        {
            FileInfo info = new FileInfo(File);
            return info.Extension;
        }
        private void MainForm_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop, false))
            {
                e.Effect = DragDropEffects.All;
            }
        }
        private void MainForm_DragDrop(object sender, DragEventArgs e)
        {
            foreach (string fileName in (string[])e.Data.GetData(DataFormats.FileDrop))
            {
                //MessageBox.Show(fileName + "\n" + GetFileExtenstion(fileName), "Debug");
                if (GetFileExtenstion(fileName) == ".torrent")
                {
                    if (MessageBox.Show("You have successfuly loaded this torrent file:\n" + fileName + "\nDo you want to load this torrent file in a new tab?", "File loaded!", MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.Yes) Add(fileName);
                    else EditCurrent(fileName);
                }
                else if (GetFileExtenstion(fileName) == ".session")
                {
                    MessageBox.Show("You have successfuly loaded this session file:\n" + fileName, "File loaded!");
                    startthem = false;
                    LoadSession(fileName);
                }
            }
        }
    }
    internal enum TabType
    {
        RatioMaster = 1,
        Unknown = 3,
    }
}