using System;
using System.Windows.Forms;
using System.Diagnostics;

namespace RatioMaster_source
{
    public partial class NewVersionForm : Form
    {
        public string name;
        public NewVersionForm()
        {
            InitializeComponent();
        }
        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start(Links.ProgramPage);
        }
        private void btnOK_Click(object sender, EventArgs e)
        {
            Close();
        }
        private void linkLabel1_LinkClicked_1(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start(Links.SupportForum);
        }
    }
}