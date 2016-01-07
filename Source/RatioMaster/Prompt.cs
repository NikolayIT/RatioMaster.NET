using System;
using System.Windows.Forms;

namespace RatioMaster_source
{
    public partial class Prompt : Form
    {
        internal string Result = "";
        internal bool OK = false;
        public Prompt(string text, string labletext, string defvalue)
        {
            InitializeComponent();
            this.Text = text;
            label1.Text = labletext;
            textBox1.Text = defvalue;
        }
        private void button2_Click(object sender, EventArgs e)
        {
            Result = "";
        }
        private void button1_Click(object sender, EventArgs e)
        {
            Result = textBox1.Text;
        }

        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 13)
            {
                button1_Click(null, null);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        }
    }
}