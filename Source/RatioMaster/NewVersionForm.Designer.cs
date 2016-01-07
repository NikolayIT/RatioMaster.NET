namespace RatioMaster_source
{
    partial class NewVersionForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.lblInfo = new System.Windows.Forms.Label();
            this.linkSite = new System.Windows.Forms.LinkLabel();
            this.lblGetItFrom = new System.Windows.Forms.Label();
            this.btnOK = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // lblInfo
            // 
            this.lblInfo.AutoSize = true;
            this.lblInfo.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.lblInfo.Location = new System.Drawing.Point(8, 9);
            this.lblInfo.Name = "lblInfo";
            this.lblInfo.Size = new System.Drawing.Size(275, 13);
            this.lblInfo.TabIndex = 0;
            this.lblInfo.Text = "A new version of RatioMaster.NET is available!";
            // 
            // linkSite
            // 
            this.linkSite.AutoSize = true;
            this.linkSite.Location = new System.Drawing.Point(112, 30);
            this.linkSite.Name = "linkSite";
            this.linkSite.Size = new System.Drawing.Size(107, 13);
            this.linkSite.TabIndex = 2;
            this.linkSite.TabStop = true;
            this.linkSite.Text = "http://ratiomaster.net";
            this.linkSite.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabel1_LinkClicked);
            // 
            // lblGetItFrom
            // 
            this.lblGetItFrom.AutoSize = true;
            this.lblGetItFrom.Location = new System.Drawing.Point(48, 30);
            this.lblGetItFrom.Name = "lblGetItFrom";
            this.lblGetItFrom.Size = new System.Drawing.Size(58, 13);
            this.lblGetItFrom.TabIndex = 1;
            this.lblGetItFrom.Text = "Get it from:";
            // 
            // btnOK
            // 
            this.btnOK.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnOK.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnOK.Location = new System.Drawing.Point(12, 52);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(268, 23);
            this.btnOK.TabIndex = 3;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // NewVersionForm
            // 
            this.ClientSize = new System.Drawing.Size(292, 86);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.lblGetItFrom);
            this.Controls.Add(this.linkSite);
            this.Controls.Add(this.lblInfo);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "NewVersionForm";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "New version released!";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblInfo;
        private System.Windows.Forms.LinkLabel linkSite;
        private System.Windows.Forms.Label lblGetItFrom;
        private System.Windows.Forms.Button btnOK;
    }
}