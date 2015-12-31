namespace ApiControllerGenerator
{
    partial class MainDialog
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainDialog));
            this.Logo = new System.Windows.Forms.PictureBox();
            this.Header1 = new System.Windows.Forms.Label();
            this.Header2 = new System.Windows.Forms.Label();
            this.GitHubLink = new System.Windows.Forms.PictureBox();
            this.LinkedInLink = new System.Windows.Forms.PictureBox();
            this.TwitterLink = new System.Windows.Forms.PictureBox();
            this.GenerateBtn = new System.Windows.Forms.Button();
            this.SettingsBtn = new System.Windows.Forms.PictureBox();
            this.GoBackBtn = new System.Windows.Forms.PictureBox();
            this.TestLabel = new System.Windows.Forms.Label();
            this.ProjectName = new System.Windows.Forms.TextBox();
            this.projectNameLabel = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.Logo)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.GitHubLink)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.LinkedInLink)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.TwitterLink)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.SettingsBtn)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.GoBackBtn)).BeginInit();
            this.SuspendLayout();
            // 
            // Logo
            // 
            this.Logo.Image = ((System.Drawing.Image)(resources.GetObject("Logo.Image")));
            this.Logo.Location = new System.Drawing.Point(318, -20);
            this.Logo.Name = "Logo";
            this.Logo.Size = new System.Drawing.Size(200, 200);
            this.Logo.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.Logo.TabIndex = 1;
            this.Logo.TabStop = false;
            // 
            // Header1
            // 
            this.Header1.AutoSize = true;
            this.Header1.Font = new System.Drawing.Font("Segoe UI", 21F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Header1.Location = new System.Drawing.Point(256, 151);
            this.Header1.Name = "Header1";
            this.Header1.Size = new System.Drawing.Size(321, 38);
            this.Header1.TabIndex = 3;
            this.Header1.Text = "API Controller Generator";
            // 
            // Header2
            // 
            this.Header2.AutoSize = true;
            this.Header2.Font = new System.Drawing.Font("Segoe UI", 13F);
            this.Header2.Location = new System.Drawing.Point(192, 189);
            this.Header2.Name = "Header2";
            this.Header2.Size = new System.Drawing.Size(435, 25);
            this.Header2.TabIndex = 4;
            this.Header2.Text = "Developed and designed by Francisco López Sánchez";
            // 
            // GitHubLink
            // 
            this.GitHubLink.AccessibleDescription = "GitHub";
            this.GitHubLink.Cursor = System.Windows.Forms.Cursors.Hand;
            this.GitHubLink.Image = ((System.Drawing.Image)(resources.GetObject("GitHubLink.Image")));
            this.GitHubLink.Location = new System.Drawing.Point(456, 526);
            this.GitHubLink.Name = "GitHubLink";
            this.GitHubLink.Size = new System.Drawing.Size(72, 57);
            this.GitHubLink.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.GitHubLink.TabIndex = 13;
            this.GitHubLink.TabStop = false;
            this.GitHubLink.Click += new System.EventHandler(this.GitHubLink_Click);
            // 
            // LinkedInLink
            // 
            this.LinkedInLink.AccessibleDescription = "LinkedIn";
            this.LinkedInLink.Cursor = System.Windows.Forms.Cursors.Hand;
            this.LinkedInLink.Image = ((System.Drawing.Image)(resources.GetObject("LinkedInLink.Image")));
            this.LinkedInLink.Location = new System.Drawing.Point(378, 526);
            this.LinkedInLink.Name = "LinkedInLink";
            this.LinkedInLink.Size = new System.Drawing.Size(72, 57);
            this.LinkedInLink.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.LinkedInLink.TabIndex = 12;
            this.LinkedInLink.TabStop = false;
            this.LinkedInLink.Click += new System.EventHandler(this.LinkedInLink_Click);
            // 
            // TwitterLink
            // 
            this.TwitterLink.AccessibleDescription = "Twitter";
            this.TwitterLink.Cursor = System.Windows.Forms.Cursors.Hand;
            this.TwitterLink.Image = ((System.Drawing.Image)(resources.GetObject("TwitterLink.Image")));
            this.TwitterLink.Location = new System.Drawing.Point(301, 526);
            this.TwitterLink.Name = "TwitterLink";
            this.TwitterLink.Size = new System.Drawing.Size(71, 57);
            this.TwitterLink.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.TwitterLink.TabIndex = 11;
            this.TwitterLink.TabStop = false;
            this.TwitterLink.Click += new System.EventHandler(this.TwitterLink_Click);
            // 
            // GenerateBtn
            // 
            this.GenerateBtn.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(33)))), ((int)(((byte)(33)))), ((int)(((byte)(33)))));
            this.GenerateBtn.Cursor = System.Windows.Forms.Cursors.Hand;
            this.GenerateBtn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.GenerateBtn.Font = new System.Drawing.Font("Segoe UI", 14.25F);
            this.GenerateBtn.ForeColor = System.Drawing.Color.White;
            this.GenerateBtn.Location = new System.Drawing.Point(318, 327);
            this.GenerateBtn.Name = "GenerateBtn";
            this.GenerateBtn.Size = new System.Drawing.Size(200, 86);
            this.GenerateBtn.TabIndex = 14;
            this.GenerateBtn.Text = "Generate";
            this.GenerateBtn.UseVisualStyleBackColor = false;
            this.GenerateBtn.Click += new System.EventHandler(this.GenerateBtn_Click);
            // 
            // SettingsBtn
            // 
            this.SettingsBtn.AccessibleDescription = "Settings";
            this.SettingsBtn.Cursor = System.Windows.Forms.Cursors.Hand;
            this.SettingsBtn.Image = ((System.Drawing.Image)(resources.GetObject("SettingsBtn.Image")));
            this.SettingsBtn.Location = new System.Drawing.Point(766, 12);
            this.SettingsBtn.Name = "SettingsBtn";
            this.SettingsBtn.Size = new System.Drawing.Size(46, 38);
            this.SettingsBtn.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.SettingsBtn.TabIndex = 15;
            this.SettingsBtn.TabStop = false;
            this.SettingsBtn.Click += new System.EventHandler(this.SettingsBtn_Click);
            // 
            // GoBackBtn
            // 
            this.GoBackBtn.Cursor = System.Windows.Forms.Cursors.Hand;
            this.GoBackBtn.Image = ((System.Drawing.Image)(resources.GetObject("GoBackBtn.Image")));
            this.GoBackBtn.Location = new System.Drawing.Point(766, 12);
            this.GoBackBtn.Name = "GoBackBtn";
            this.GoBackBtn.Size = new System.Drawing.Size(46, 38);
            this.GoBackBtn.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.GoBackBtn.TabIndex = 16;
            this.GoBackBtn.TabStop = false;
            this.GoBackBtn.Click += new System.EventHandler(this.GoBackBtn_Click);
            // 
            // TestLabel
            // 
            this.TestLabel.AutoSize = true;
            this.TestLabel.Location = new System.Drawing.Point(418, 452);
            this.TestLabel.Name = "TestLabel";
            this.TestLabel.Size = new System.Drawing.Size(0, 13);
            this.TestLabel.TabIndex = 17;
            this.TestLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // ProjectName
            // 
            this.ProjectName.Location = new System.Drawing.Point(318, 294);
            this.ProjectName.Name = "ProjectName";
            this.ProjectName.Size = new System.Drawing.Size(200, 22);
            this.ProjectName.TabIndex = 18;
            this.ProjectName.Text = "RPGTestProject";
            this.ProjectName.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.ProjectName_KeyPress);
            this.ProjectName.KeyUp += new System.Windows.Forms.KeyEventHandler(this.ProjectName_KeyUp);
            // 
            // projectNameLabel
            // 
            this.projectNameLabel.AutoSize = true;
            this.projectNameLabel.Location = new System.Drawing.Point(375, 278);
            this.projectNameLabel.Name = "projectNameLabel";
            this.projectNameLabel.Size = new System.Drawing.Size(93, 13);
            this.projectNameLabel.TabIndex = 19;
            this.projectNameLabel.Text = "API project name";
            // 
            // MainDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(824, 586);
            this.Controls.Add(this.projectNameLabel);
            this.Controls.Add(this.ProjectName);
            this.Controls.Add(this.TestLabel);
            this.Controls.Add(this.SettingsBtn);
            this.Controls.Add(this.GoBackBtn);
            this.Controls.Add(this.GenerateBtn);
            this.Controls.Add(this.GitHubLink);
            this.Controls.Add(this.LinkedInLink);
            this.Controls.Add(this.TwitterLink);
            this.Controls.Add(this.Header1);
            this.Controls.Add(this.Header2);
            this.Controls.Add(this.Logo);
            this.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximumSize = new System.Drawing.Size(840, 625);
            this.MinimumSize = new System.Drawing.Size(840, 625);
            this.Name = "MainDialog";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "ACG";
            this.Load += new System.EventHandler(this.MainDialog_Load);
            ((System.ComponentModel.ISupportInitialize)(this.Logo)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.GitHubLink)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.LinkedInLink)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.TwitterLink)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.SettingsBtn)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.GoBackBtn)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox Logo;
        private System.Windows.Forms.Label Header2;
        private System.Windows.Forms.Label Header1;
        private System.Windows.Forms.PictureBox GitHubLink;
        private System.Windows.Forms.PictureBox LinkedInLink;
        private System.Windows.Forms.PictureBox TwitterLink;
        private System.Windows.Forms.Button GenerateBtn;
        private System.Windows.Forms.PictureBox SettingsBtn;
        private System.Windows.Forms.PictureBox GoBackBtn;
        private System.Windows.Forms.Label TestLabel;
        private System.Windows.Forms.TextBox ProjectName;
        private System.Windows.Forms.Label projectNameLabel;
    }
}