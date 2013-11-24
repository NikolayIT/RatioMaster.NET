using System;
using System.Windows.Forms;
using System.Diagnostics;

namespace RatioMaster_source
{
    internal partial class AboutForm : Form
    {
        internal string version;
        internal AboutForm(string Ver)
        {
            InitializeComponent();
            version = Ver;
        }
        private void button1_Click(object sender, EventArgs e)
        {
            Close();
        }
        private void AboutForm_Load(object sender, EventArgs e)
        {
            Text += " version " + version;
        }
        private void linkEMail2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("mailto:nrpg666@yahoo.com");
        }
        private void linkForum_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("http://nrpg.16.forumer.com/");
        }
        private void linkAuthorWebSite_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("http://nikolay.it");
        }
        private void linkEMail_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("mailto:admin@nikolay.it");
        }
        private void linkWebSite_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("http://ratiomaster.net");
        }
        private void pictureBox1_Click(object sender, EventArgs e)
        {
            Process.Start("http://nikolay.it/");
        }
        private void linkMDsite_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("http://www.moofdev.org/");
        }
        private void linkMDforums_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("http://www.moofdev.org/forums/");
        }
        private void linkRMmail_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("mailto:ratiomaster_06@yahoo.com");
        }
        private void linkJTSmail_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("mailto:ratiomaster_06@yahoo.com");
        }
    }
}
