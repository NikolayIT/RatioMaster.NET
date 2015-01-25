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
            Process.Start(Links.MailToNrpg666YahooCom);
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
        private void pictureBox1_Click(object sender, EventArgs e)
        {
            Process.Start(Links.AuthorPage);
        }
        private void linkMDsite_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start(Links.MoofDev);
        }
        private void linkMDforums_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start(Links.MoofDevForums);
        }

        //ToDo Links.MailToRatiomaster06YahooCom dublicate
        private void linkRMmail_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start(Links.MailToRatiomaster06YahooCom);
        }

        //ToDo Links.MailToRatiomaster06YahooCom dublicate
        private void linkJTSmail_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start(Links.MailToRatiomaster06YahooCom);
        }
    }
}
