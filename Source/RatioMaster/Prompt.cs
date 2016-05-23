namespace RatioMaster_source
{
    using System;
    using System.Windows.Forms;

    public partial class Prompt : Form
    {
        internal string Result = string.Empty;

        internal bool OK = false;

        public Prompt(string text, string labletext, string defvalue)
        {
            this.InitializeComponent();
            this.Text = text;
            this.label1.Text = labletext;
            this.textBox1.Text = defvalue;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Result = string.Empty;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Result = this.textBox1.Text;
        }

        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 13)
            {
                this.button1_Click(null, null);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        }
    }
}