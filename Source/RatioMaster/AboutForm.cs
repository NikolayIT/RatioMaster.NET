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

        private void linkGitHubPage_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start(Links.GitHubPage);
        }

        private void linkAuthorWebSite_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start(Links.AuthorPage);
        }

        private void linkEMail_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start(Links.MailToAdminNikolayIt);
        }

        private void linkWebSite_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start(Links.ProgramPage);
        }
    }
}
