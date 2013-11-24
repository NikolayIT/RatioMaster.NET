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
            this.label2 = new System.Windows.Forms.Label();
            this.lblProgramName = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.linkForum = new System.Windows.Forms.LinkLabel();
            this.lblForumAdress = new System.Windows.Forms.Label();
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
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.label2.ForeColor = System.Drawing.Color.Red;
            this.label2.Location = new System.Drawing.Point(114, 63);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(218, 20);
            this.label2.TabIndex = 1;
            this.label2.Text = "Based on RatioMaster 1.4";
            // 
            // lblProgramName
            // 
            this.lblProgramName.AutoSize = true;
            this.lblProgramName.Font = new System.Drawing.Font("Microsoft Sans Serif", 28F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.lblProgramName.Location = new System.Drawing.Point(71, 19);
            this.lblProgramName.Name = "lblProgramName";
            this.lblProgramName.Size = new System.Drawing.Size(327, 44);
            this.lblProgramName.TabIndex = 0;
            this.lblProgramName.Text = "RatioMaster.NET";
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.linkForum);
            this.groupBox1.Controls.Add(this.lblForumAdress);
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
            // linkForum
            // 
            this.linkForum.AutoSize = true;
            this.linkForum.Cursor = System.Windows.Forms.Cursors.Help;
            this.linkForum.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.linkForum.Location = new System.Drawing.Point(272, 44);
            this.linkForum.Name = "linkForum";
            this.linkForum.Size = new System.Drawing.Size(143, 13);
            this.linkForum.TabIndex = 5;
            this.linkForum.TabStop = true;
            this.linkForum.Text = "http://nrpg.forumer.com";
            this.linkForum.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkForum_LinkClicked);
            // 
            // lblForumAdress
            // 
            this.lblForumAdress.AutoSize = true;
            this.lblForumAdress.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.lblForumAdress.Location = new System.Drawing.Point(221, 44);
            this.lblForumAdress.Name = "lblForumAdress";
            this.lblForumAdress.Size = new System.Drawing.Size(45, 13);
            this.lblForumAdress.TabIndex = 4;
            this.lblForumAdress.Text = "Forum:";
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
            this.linkEMail.Size = new System.Drawing.Size(87, 13);
            this.linkEMail.TabIndex = 7;
            this.linkEMail.TabStop = true;
            this.linkEMail.Text = "admin@nikolay.it";
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
            this.Controls.Add(this.label2);
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

        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label lblProgramName;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.LinkLabel linkForum;
        private System.Windows.Forms.Label lblForumAdress;
        private System.Windows.Forms.LinkLabel linkWebSite;
        private System.Windows.Forms.Label lblWebSite;
        private System.Windows.Forms.LinkLabel linkEMail;
        private System.Windows.Forms.LinkLabel linkAuthorWebSite;
        private System.Windows.Forms.Label lblInfoEMail;
        private System.Windows.Forms.Label lblInfoAuthorWebSite;
        private System.Windows.Forms.Button button1;
    }
}