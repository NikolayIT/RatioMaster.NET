namespace RatioMaster_source
{
    partial class AboutForm
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
            this.lblProgramName = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.linkGitHub = new System.Windows.Forms.LinkLabel();
            this.lblGitHubAdress = new System.Windows.Forms.Label();
            this.linkWebSite = new System.Windows.Forms.LinkLabel();
            this.lblWebSite = new System.Windows.Forms.Label();
            this.linkEMail = new System.Windows.Forms.LinkLabel();
            this.linkAuthorWebSite = new System.Windows.Forms.LinkLabel();
            this.lblInfoEMail = new System.Windows.Forms.Label();
            this.lblInfoAuthorWebSite = new System.Windows.Forms.Label();
            this.button1 = new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // lblProgramName
            // 
            this.lblProgramName.AutoSize = true;
            this.lblProgramName.Font = new System.Drawing.Font("Microsoft Sans Serif", 28F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.lblProgramName.Location = new System.Drawing.Point(71, 27);
            this.lblProgramName.Name = "lblProgramName";
            this.lblProgramName.Size = new System.Drawing.Size(327, 44);
            this.lblProgramName.TabIndex = 0;
            this.lblProgramName.Text = "RatioMaster.NET";
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.linkGitHub);
            this.groupBox1.Controls.Add(this.lblGitHubAdress);
            this.groupBox1.Controls.Add(this.linkWebSite);
            this.groupBox1.Controls.Add(this.lblWebSite);
            this.groupBox1.Controls.Add(this.linkEMail);
            this.groupBox1.Controls.Add(this.linkAuthorWebSite);
            this.groupBox1.Controls.Add(this.lblInfoEMail);
            this.groupBox1.Controls.Add(this.lblInfoAuthorWebSite);
            this.groupBox1.Location = new System.Drawing.Point(12, 98);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(443, 65);
            this.groupBox1.TabIndex = 2;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Communication";
            // 
            // linkGitHub
            // 
            this.linkGitHub.AutoSize = true;
            this.linkGitHub.Cursor = System.Windows.Forms.Cursors.Help;
            this.linkGitHub.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.linkGitHub.Location = new System.Drawing.Point(272, 44);
            this.linkGitHub.Name = "linkGitHub";
            this.linkGitHub.Size = new System.Drawing.Size(104, 13);
            this.linkGitHub.TabIndex = 5;
            this.linkGitHub.TabStop = true;
            this.linkGitHub.Text = "RatioMaster.NET";
            this.linkGitHub.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkGitHubPage_LinkClicked);
            // 
            // lblGitHubAdress
            // 
            this.lblGitHubAdress.AutoSize = true;
            this.lblGitHubAdress.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.lblGitHubAdress.Location = new System.Drawing.Point(221, 44);
            this.lblGitHubAdress.Name = "lblGitHubAdress";
            this.lblGitHubAdress.Size = new System.Drawing.Size(50, 13);
            this.lblGitHubAdress.TabIndex = 4;
            this.lblGitHubAdress.Text = "GitHub:";
            // 
            // linkWebSite
            // 
            this.linkWebSite.AutoSize = true;
            this.linkWebSite.Cursor = System.Windows.Forms.Cursors.Help;
            this.linkWebSite.Location = new System.Drawing.Point(50, 22);
            this.linkWebSite.Name = "linkWebSite";
            this.linkWebSite.Size = new System.Drawing.Size(107, 13);
            this.linkWebSite.TabIndex = 1;
            this.linkWebSite.TabStop = true;
            this.linkWebSite.Text = "http://ratiomaster.net";
            this.linkWebSite.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkWebSite_LinkClicked);
            // 
            // lblWebSite
            // 
            this.lblWebSite.AutoSize = true;
            this.lblWebSite.Location = new System.Drawing.Point(6, 22);
            this.lblWebSite.Name = "lblWebSite";
            this.lblWebSite.Size = new System.Drawing.Size(33, 13);
            this.lblWebSite.TabIndex = 0;
            this.lblWebSite.Text = "Web:";
            // 
            // linkEMail
            // 
            this.linkEMail.AutoSize = true;
            this.linkEMail.Cursor = System.Windows.Forms.Cursors.Help;
            this.linkEMail.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.linkEMail.Location = new System.Drawing.Point(50, 44);
            this.linkEMail.Name = "linkEMail";
            this.linkEMail.Size = new System.Drawing.Size(110, 13);
            this.linkEMail.TabIndex = 7;
            this.linkEMail.TabStop = true;
            this.linkEMail.Text = "ratiomaster@nikolay.it";
            this.linkEMail.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkEMail_LinkClicked);
            // 
            // linkAuthorWebSite
            // 
            this.linkAuthorWebSite.AutoSize = true;
            this.linkAuthorWebSite.Cursor = System.Windows.Forms.Cursors.Help;
            this.linkAuthorWebSite.Location = new System.Drawing.Point(272, 22);
            this.linkAuthorWebSite.Name = "linkAuthorWebSite";
            this.linkAuthorWebSite.Size = new System.Drawing.Size(79, 13);
            this.linkAuthorWebSite.TabIndex = 3;
            this.linkAuthorWebSite.TabStop = true;
            this.linkAuthorWebSite.Text = "http://nikolay.it";
            this.linkAuthorWebSite.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkAuthorWebSite_LinkClicked);
            // 
            // lblInfoEMail
            // 
            this.lblInfoEMail.AutoSize = true;
            this.lblInfoEMail.Location = new System.Drawing.Point(6, 44);
            this.lblInfoEMail.Name = "lblInfoEMail";
            this.lblInfoEMail.Size = new System.Drawing.Size(38, 13);
            this.lblInfoEMail.TabIndex = 6;
            this.lblInfoEMail.Text = "E-mail:";
            // 
            // lblInfoAuthorWebSite
            // 
            this.lblInfoAuthorWebSite.AutoSize = true;
            this.lblInfoAuthorWebSite.Location = new System.Drawing.Point(221, 22);
            this.lblInfoAuthorWebSite.Name = "lblInfoAuthorWebSite";
            this.lblInfoAuthorWebSite.Size = new System.Drawing.Size(41, 13);
            this.lblInfoAuthorWebSite.TabIndex = 2;
            this.lblInfoAuthorWebSite.Text = "Author:";
            // 
            // button1
            // 
            this.button1.Cursor = System.Windows.Forms.Cursors.Hand;
            this.button1.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.button1.Location = new System.Drawing.Point(12, 169);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(443, 27);
            this.button1.TabIndex = 5;
            this.button1.Text = "OK";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // AboutForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(467, 209);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.lblProgramName);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "AboutForm";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "About RatioMaster.NET";
            this.Load += new System.EventHandler(this.AboutForm_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Label lblProgramName;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.LinkLabel linkGitHub;
        private System.Windows.Forms.Label lblGitHubAdress;
        private System.Windows.Forms.LinkLabel linkWebSite;
        private System.Windows.Forms.Label lblWebSite;
        private System.Windows.Forms.LinkLabel linkEMail;
        private System.Windows.Forms.LinkLabel linkAuthorWebSite;
        private System.Windows.Forms.Label lblInfoEMail;
        private System.Windows.Forms.Label lblInfoAuthorWebSite;
        private System.Windows.Forms.Button button1;
    }
}