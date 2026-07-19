namespace AVEraser
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.pnlHeader = new System.Windows.Forms.Panel();
            this.pictureBox3 = new System.Windows.Forms.PictureBox();
            this.pictureBox2 = new System.Windows.Forms.PictureBox();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.pnlWarn = new System.Windows.Forms.Panel();
            this.pictureBox4 = new System.Windows.Forms.PictureBox();
            this.lblWarnText = new System.Windows.Forms.Label();
            this.pnlFooter = new System.Windows.Forms.Panel();
            this.pnlFooterLine = new System.Windows.Forms.Panel();
            this.pnlContent = new System.Windows.Forms.Panel();
            this.lblStatus = new System.Windows.Forms.Label();
            this.pnlSelectBar = new System.Windows.Forms.Panel();
            this.avProgress = new AVEraser.ProgressBar();
            this.btnSelectAll = new AVEraser.RoundBtn();
            this.btnDeselectAll = new AVEraser.RoundBtn();
            this.avGrid = new AVEraser.AVGrid();
            this.btnReport = new AVEraser.RoundBtn();
            this.btnScan = new AVEraser.RoundBtn();
            this.btnDelete = new AVEraser.RoundBtn();
            this.pnlHeader.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox3)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.pnlWarn.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox4)).BeginInit();
            this.pnlFooter.SuspendLayout();
            this.pnlContent.SuspendLayout();
            this.pnlSelectBar.SuspendLayout();
            this.SuspendLayout();
            // 
            // pnlHeader
            // 
            this.pnlHeader.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(13)))), ((int)(((byte)(15)))), ((int)(((byte)(18)))));
            this.pnlHeader.Controls.Add(this.pictureBox3);
            this.pnlHeader.Controls.Add(this.pictureBox2);
            this.pnlHeader.Controls.Add(this.pictureBox1);
            this.pnlHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlHeader.Location = new System.Drawing.Point(0, 0);
            this.pnlHeader.Name = "pnlHeader";
            this.pnlHeader.Padding = new System.Windows.Forms.Padding(20, 0, 0, 0);
            this.pnlHeader.Size = new System.Drawing.Size(840, 43);
            this.pnlHeader.TabIndex = 2;
            // 
            // pictureBox3
            // 
            this.pictureBox3.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox3.Image")));
            this.pictureBox3.Location = new System.Drawing.Point(773, 5);
            this.pictureBox3.Name = "pictureBox3";
            this.pictureBox3.Size = new System.Drawing.Size(29, 30);
            this.pictureBox3.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBox3.TabIndex = 4;
            this.pictureBox3.TabStop = false;
            this.pictureBox3.Click += new System.EventHandler(this.Minimize_Click);
            // 
            // pictureBox2
            // 
            this.pictureBox2.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox2.Image")));
            this.pictureBox2.Location = new System.Drawing.Point(808, 5);
            this.pictureBox2.Name = "pictureBox2";
            this.pictureBox2.Size = new System.Drawing.Size(29, 30);
            this.pictureBox2.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBox2.TabIndex = 4;
            this.pictureBox2.TabStop = false;
            this.pictureBox2.Click += new System.EventHandler(this.Exit_Click);
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox1.Image")));
            this.pictureBox1.Location = new System.Drawing.Point(6, 6);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(162, 31);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBox1.TabIndex = 4;
            this.pictureBox1.TabStop = false;
            // 
            // pnlWarn
            // 
            this.pnlWarn.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(47)))), ((int)(((byte)(119)))), ((int)(((byte)(243)))));
            this.pnlWarn.Controls.Add(this.pictureBox4);
            this.pnlWarn.Controls.Add(this.lblWarnText);
            this.pnlWarn.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlWarn.Location = new System.Drawing.Point(0, 43);
            this.pnlWarn.Name = "pnlWarn";
            this.pnlWarn.Size = new System.Drawing.Size(840, 38);
            this.pnlWarn.TabIndex = 1;
            // 
            // pictureBox4
            // 
            this.pictureBox4.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox4.Image")));
            this.pictureBox4.Location = new System.Drawing.Point(12, 4);
            this.pictureBox4.Name = "pictureBox4";
            this.pictureBox4.Size = new System.Drawing.Size(34, 29);
            this.pictureBox4.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBox4.TabIndex = 2;
            this.pictureBox4.TabStop = false;
            // 
            // lblWarnText
            // 
            this.lblWarnText.AutoSize = true;
            this.lblWarnText.BackColor = System.Drawing.Color.Transparent;
            this.lblWarnText.Font = new System.Drawing.Font("Segoe UI Variable Text", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblWarnText.ForeColor = System.Drawing.Color.White;
            this.lblWarnText.Location = new System.Drawing.Point(52, 10);
            this.lblWarnText.Name = "lblWarnText";
            this.lblWarnText.Size = new System.Drawing.Size(430, 17);
            this.lblWarnText.TabIndex = 1;
            this.lblWarnText.Text = "Always uninstall the antivirus program via Windows Settings → Apps first";
            // 
            // pnlFooter
            // 
            this.pnlFooter.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(13)))), ((int)(((byte)(15)))), ((int)(((byte)(18)))));
            this.pnlFooter.Controls.Add(this.btnReport);
            this.pnlFooter.Controls.Add(this.btnScan);
            this.pnlFooter.Controls.Add(this.btnDelete);
            this.pnlFooter.Controls.Add(this.pnlFooterLine);
            this.pnlFooter.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlFooter.Location = new System.Drawing.Point(0, 516);
            this.pnlFooter.Name = "pnlFooter";
            this.pnlFooter.Size = new System.Drawing.Size(840, 64);
            this.pnlFooter.TabIndex = 3;
            // 
            // pnlFooterLine
            // 
            this.pnlFooterLine.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(44)))), ((int)(((byte)(56)))));
            this.pnlFooterLine.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlFooterLine.Location = new System.Drawing.Point(0, 0);
            this.pnlFooterLine.Name = "pnlFooterLine";
            this.pnlFooterLine.Size = new System.Drawing.Size(840, 1);
            this.pnlFooterLine.TabIndex = 2;
            // 
            // pnlContent
            // 
            this.pnlContent.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(19)))), ((int)(((byte)(22)))), ((int)(((byte)(28)))));
            this.pnlContent.Controls.Add(this.lblStatus);
            this.pnlContent.Controls.Add(this.avProgress);
            this.pnlContent.Controls.Add(this.pnlSelectBar);
            this.pnlContent.Controls.Add(this.avGrid);
            this.pnlContent.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlContent.Location = new System.Drawing.Point(0, 81);
            this.pnlContent.Name = "pnlContent";
            this.pnlContent.Size = new System.Drawing.Size(840, 435);
            this.pnlContent.TabIndex = 0;
            // 
            // lblStatus
            // 
            this.lblStatus.BackColor = System.Drawing.Color.Transparent;
            this.lblStatus.Font = new System.Drawing.Font("Segoe UI Variable Text", 9F);
            this.lblStatus.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(140)))), ((int)(((byte)(150)))), ((int)(((byte)(170)))));
            this.lblStatus.Location = new System.Drawing.Point(9, 14);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(700, 20);
            this.lblStatus.TabIndex = 0;
            this.lblStatus.Text = "Click Scan to search for residues";
            // 
            // pnlSelectBar
            // 
            this.pnlSelectBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pnlSelectBar.BackColor = System.Drawing.Color.Transparent;
            this.pnlSelectBar.Controls.Add(this.btnSelectAll);
            this.pnlSelectBar.Controls.Add(this.btnDeselectAll);
            this.pnlSelectBar.Location = new System.Drawing.Point(12, 48);
            this.pnlSelectBar.Name = "pnlSelectBar";
            this.pnlSelectBar.Size = new System.Drawing.Size(255, 36);
            this.pnlSelectBar.TabIndex = 2;
            this.pnlSelectBar.Visible = false;
            // 
            // avProgress
            // 
            this.avProgress.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.avProgress.BackColor = System.Drawing.Color.Transparent;
            this.avProgress.BarColor = System.Drawing.Color.FromArgb(((int)(((byte)(47)))), ((int)(((byte)(119)))), ((int)(((byte)(243)))));
            this.avProgress.Location = new System.Drawing.Point(12, 37);
            this.avProgress.Name = "avProgress";
            this.avProgress.Size = new System.Drawing.Size(808, 5);
            this.avProgress.TabIndex = 1;
            this.avProgress.TrackColor = System.Drawing.Color.FromArgb(((int)(((byte)(13)))), ((int)(((byte)(15)))), ((int)(((byte)(18)))));
            this.avProgress.Value = 0;
            this.avProgress.Visible = false;
            // 
            // btnSelectAll
            // 
            this.btnSelectAll.BackColor = System.Drawing.Color.Transparent;
            this.btnSelectAll.ButtonIcon = null;
            this.btnSelectAll.CornerRadius = 6;
            this.btnSelectAll.Cursor = System.Windows.Forms.Cursors.Default;
            this.btnSelectAll.Font = new System.Drawing.Font("Segoe UI Variable Text", 8.5F);
            this.btnSelectAll.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(140)))), ((int)(((byte)(150)))), ((int)(((byte)(170)))));
            this.btnSelectAll.HoverColor = System.Drawing.Color.FromArgb(((int)(((byte)(32)))), ((int)(((byte)(37)))), ((int)(((byte)(48)))));
            this.btnSelectAll.IconLeft = true;
            this.btnSelectAll.IconSize = 16;
            this.btnSelectAll.Location = new System.Drawing.Point(0, 3);
            this.btnSelectAll.Name = "btnSelectAll";
            this.btnSelectAll.NormalColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(30)))), ((int)(((byte)(38)))));
            this.btnSelectAll.OutlineColor = System.Drawing.Color.FromArgb(((int)(((byte)(47)))), ((int)(((byte)(119)))), ((int)(((byte)(243)))));
            this.btnSelectAll.Outlined = false;
            this.btnSelectAll.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.btnSelectAll.Size = new System.Drawing.Size(115, 28);
            this.btnSelectAll.TabIndex = 0;
            this.btnSelectAll.Text = "Select all";
            this.btnSelectAll.Click += new System.EventHandler(this.BtnSelectAll_Click);
            // 
            // btnDeselectAll
            // 
            this.btnDeselectAll.BackColor = System.Drawing.Color.Transparent;
            this.btnDeselectAll.ButtonIcon = null;
            this.btnDeselectAll.CornerRadius = 6;
            this.btnDeselectAll.Cursor = System.Windows.Forms.Cursors.Default;
            this.btnDeselectAll.Font = new System.Drawing.Font("Segoe UI Variable Text", 8.5F);
            this.btnDeselectAll.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(140)))), ((int)(((byte)(150)))), ((int)(((byte)(170)))));
            this.btnDeselectAll.HoverColor = System.Drawing.Color.FromArgb(((int)(((byte)(32)))), ((int)(((byte)(37)))), ((int)(((byte)(48)))));
            this.btnDeselectAll.IconLeft = true;
            this.btnDeselectAll.IconSize = 16;
            this.btnDeselectAll.Location = new System.Drawing.Point(121, 3);
            this.btnDeselectAll.Name = "btnDeselectAll";
            this.btnDeselectAll.NormalColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(30)))), ((int)(((byte)(38)))));
            this.btnDeselectAll.OutlineColor = System.Drawing.Color.FromArgb(((int)(((byte)(47)))), ((int)(((byte)(119)))), ((int)(((byte)(243)))));
            this.btnDeselectAll.Outlined = false;
            this.btnDeselectAll.Size = new System.Drawing.Size(130, 28);
            this.btnDeselectAll.TabIndex = 1;
            this.btnDeselectAll.Text = "Deselect all";
            this.btnDeselectAll.Click += new System.EventHandler(this.BtnDeselectAll_Click);
            // 
            // avGrid
            // 
            this.avGrid.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.avGrid.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(19)))), ((int)(((byte)(22)))), ((int)(((byte)(28)))));
            this.avGrid.Location = new System.Drawing.Point(12, 90);
            this.avGrid.Name = "avGrid";
            this.avGrid.Size = new System.Drawing.Size(808, 547);
            this.avGrid.TabIndex = 3;
            this.avGrid.Visible = false;
            // 
            // btnReport
            // 
            this.btnReport.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnReport.BackColor = System.Drawing.Color.Transparent;
            this.btnReport.ButtonIcon = null;
            this.btnReport.CornerRadius = 8;
            this.btnReport.Cursor = System.Windows.Forms.Cursors.Default;
            this.btnReport.Font = new System.Drawing.Font("Segoe UI Variable Text", 9.5F, System.Drawing.FontStyle.Bold);
            this.btnReport.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(140)))), ((int)(((byte)(150)))), ((int)(((byte)(170)))));
            this.btnReport.HoverColor = System.Drawing.Color.FromArgb(((int)(((byte)(32)))), ((int)(((byte)(37)))), ((int)(((byte)(48)))));
            this.btnReport.IconLeft = true;
            this.btnReport.IconSize = 16;
            this.btnReport.Location = new System.Drawing.Point(682, 13);
            this.btnReport.Name = "btnReport";
            this.btnReport.NormalColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(30)))), ((int)(((byte)(38)))));
            this.btnReport.OutlineColor = System.Drawing.Color.FromArgb(((int)(((byte)(47)))), ((int)(((byte)(119)))), ((int)(((byte)(243)))));
            this.btnReport.Outlined = false;
            this.btnReport.Size = new System.Drawing.Size(138, 38);
            this.btnReport.TabIndex = 3;
            this.btnReport.Text = "Report AV";
            this.btnReport.Click += new System.EventHandler(this.BtnReport_Click);
            // 
            // btnScan
            // 
            this.btnScan.BackColor = System.Drawing.Color.Transparent;
            this.btnScan.ButtonIcon = null;
            this.btnScan.CornerRadius = 8;
            this.btnScan.Cursor = System.Windows.Forms.Cursors.Default;
            this.btnScan.Font = new System.Drawing.Font("Segoe UI Variable Text", 9.5F, System.Drawing.FontStyle.Bold);
            this.btnScan.ForeColor = System.Drawing.Color.White;
            this.btnScan.HoverColor = System.Drawing.Color.FromArgb(((int)(((byte)(72)))), ((int)(((byte)(140)))), ((int)(((byte)(255)))));
            this.btnScan.IconLeft = true;
            this.btnScan.IconSize = 16;
            this.btnScan.Location = new System.Drawing.Point(20, 13);
            this.btnScan.Name = "btnScan";
            this.btnScan.NormalColor = System.Drawing.Color.FromArgb(((int)(((byte)(47)))), ((int)(((byte)(119)))), ((int)(((byte)(243)))));
            this.btnScan.OutlineColor = System.Drawing.Color.FromArgb(((int)(((byte)(47)))), ((int)(((byte)(119)))), ((int)(((byte)(243)))));
            this.btnScan.Outlined = false;
            this.btnScan.Size = new System.Drawing.Size(160, 38);
            this.btnScan.TabIndex = 0;
            this.btnScan.Text = "Scan";
            this.btnScan.Click += new System.EventHandler(this.BtnScan_Click);
            // 
            // btnDelete
            // 
            this.btnDelete.BackColor = System.Drawing.Color.Transparent;
            this.btnDelete.ButtonIcon = null;
            this.btnDelete.CornerRadius = 8;
            this.btnDelete.Cursor = System.Windows.Forms.Cursors.Arrow;
            this.btnDelete.Font = new System.Drawing.Font("Segoe UI Variable Text", 9.5F, System.Drawing.FontStyle.Bold);
            this.btnDelete.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(47)))), ((int)(((byte)(119)))), ((int)(((byte)(243)))));
            this.btnDelete.HoverColor = System.Drawing.Color.FromArgb(((int)(((byte)(72)))), ((int)(((byte)(140)))), ((int)(((byte)(255)))));
            this.btnDelete.IconLeft = true;
            this.btnDelete.IconSize = 16;
            this.btnDelete.Location = new System.Drawing.Point(188, 13);
            this.btnDelete.Name = "btnDelete";
            this.btnDelete.NormalColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(30)))), ((int)(((byte)(38)))));
            this.btnDelete.OutlineColor = System.Drawing.Color.FromArgb(((int)(((byte)(47)))), ((int)(((byte)(119)))), ((int)(((byte)(243)))));
            this.btnDelete.Outlined = true;
            this.btnDelete.Size = new System.Drawing.Size(170, 38);
            this.btnDelete.TabIndex = 1;
            this.btnDelete.Text = "Delete selected";
            this.btnDelete.Visible = false;
            this.btnDelete.Click += new System.EventHandler(this.BtnDelete_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(19)))), ((int)(((byte)(22)))), ((int)(((byte)(28)))));
            this.ClientSize = new System.Drawing.Size(840, 580);
            this.Controls.Add(this.pnlContent);
            this.Controls.Add(this.pnlWarn);
            this.Controls.Add(this.pnlHeader);
            this.Controls.Add(this.pnlFooter);
            this.Font = new System.Drawing.Font("Segoe UI Variable Text", 9.5F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "Form1";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "AVEraser";
            this.TransparencyKey = System.Drawing.Color.Magenta;
            this.pnlHeader.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox3)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.pnlWarn.ResumeLayout(false);
            this.pnlWarn.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox4)).EndInit();
            this.pnlFooter.ResumeLayout(false);
            this.pnlContent.ResumeLayout(false);
            this.pnlSelectBar.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        // ── Designer fields ──
        private System.Windows.Forms.Panel pnlHeader;
        private System.Windows.Forms.Panel pnlWarn;
        private System.Windows.Forms.Label lblWarnText;
        private System.Windows.Forms.Panel pnlFooter;
        private System.Windows.Forms.Panel pnlFooterLine;
        private AVEraser.RoundBtn btnScan;
        private AVEraser.RoundBtn btnDelete;
        private AVEraser.RoundBtn btnReport;
        private System.Windows.Forms.Panel pnlContent;
        private System.Windows.Forms.Label lblStatus;
        private AVEraser.ProgressBar avProgress;
        private System.Windows.Forms.Panel pnlSelectBar;
        private AVEraser.RoundBtn btnSelectAll;
        private AVEraser.RoundBtn btnDeselectAll;
        private AVEraser.AVGrid avGrid;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.PictureBox pictureBox2;
        private System.Windows.Forms.PictureBox pictureBox3;
        private System.Windows.Forms.PictureBox pictureBox4;
    }
}