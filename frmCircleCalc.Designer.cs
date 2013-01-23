namespace Standalone_Circle_Calc
{
    partial class Form1
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
            this.btnOk = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.chkGag = new System.Windows.Forms.CheckBox();
            this.lblSort = new System.Windows.Forms.Label();
            this.cboSort = new System.Windows.Forms.ComboBox();
            this.Normal = new System.Windows.Forms.RadioButton();
            this.lblDisplay = new System.Windows.Forms.Label();
            this.Post200Circle = new System.Windows.Forms.RadioButton();
            this.NextCircle = new System.Windows.Forms.RadioButton();
            this.chkEcho = new System.Windows.Forms.CheckBox();
            this.chkLog = new System.Windows.Forms.CheckBox();
            this.chkParse = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // btnOk
            // 
            this.btnOk.Location = new System.Drawing.Point(12, 222);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(75, 23);
            this.btnOk.TabIndex = 5;
            this.btnOk.Text = "Ok";
            this.btnOk.UseVisualStyleBackColor = true;
            this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(130, 222);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 6;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // chkGag
            // 
            this.chkGag.AutoSize = true;
            this.chkGag.Location = new System.Drawing.Point(12, 198);
            this.chkGag.Name = "chkGag";
            this.chkGag.Size = new System.Drawing.Size(128, 17);
            this.chkGag.TabIndex = 7;
            this.chkGag.Text = "Gag funny messaging";
            this.chkGag.UseVisualStyleBackColor = true;
            // 
            // lblSort
            // 
            this.lblSort.AutoSize = true;
            this.lblSort.Location = new System.Drawing.Point(9, 9);
            this.lblSort.Name = "lblSort";
            this.lblSort.Size = new System.Drawing.Size(68, 13);
            this.lblSort.TabIndex = 8;
            this.lblSort.Text = "Sort Method:";
            // 
            // cboSort
            // 
            this.cboSort.FormattingEnabled = true;
            this.cboSort.Items.AddRange(new object[] {
            "Top",
            "Bottom"});
            this.cboSort.Location = new System.Drawing.Point(83, 6);
            this.cboSort.Name = "cboSort";
            this.cboSort.Size = new System.Drawing.Size(122, 21);
            this.cboSort.TabIndex = 9;
            this.cboSort.Text = "Top";
            // 
            // Normal
            // 
            this.Normal.AutoSize = true;
            this.Normal.Checked = true;
            this.Normal.Location = new System.Drawing.Point(25, 48);
            this.Normal.Name = "Normal";
            this.Normal.Size = new System.Drawing.Size(101, 17);
            this.Normal.TabIndex = 10;
            this.Normal.TabStop = true;
            this.Normal.Text = "Normal (Default)";
            this.Normal.UseVisualStyleBackColor = true;
            // 
            // lblDisplay
            // 
            this.lblDisplay.AutoSize = true;
            this.lblDisplay.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblDisplay.Location = new System.Drawing.Point(9, 32);
            this.lblDisplay.Name = "lblDisplay";
            this.lblDisplay.Size = new System.Drawing.Size(83, 13);
            this.lblDisplay.TabIndex = 11;
            this.lblDisplay.Text = "Display Options:";
            // 
            // Post200Circle
            // 
            this.Post200Circle.AutoSize = true;
            this.Post200Circle.Location = new System.Drawing.Point(25, 71);
            this.Post200Circle.Name = "Post200Circle";
            this.Post200Circle.Size = new System.Drawing.Size(164, 17);
            this.Post200Circle.TabIndex = 12;
            this.Post200Circle.TabStop = true;
            this.Post200Circle.Text = "Show Reqs for skills past 200";
            this.Post200Circle.UseVisualStyleBackColor = true;
            // 
            // NextCircle
            // 
            this.NextCircle.AutoSize = true;
            this.NextCircle.Location = new System.Drawing.Point(25, 96);
            this.NextCircle.Name = "NextCircle";
            this.NextCircle.Size = new System.Drawing.Size(97, 17);
            this.NextCircle.TabIndex = 13;
            this.NextCircle.TabStop = true;
            this.NextCircle.Text = "Next circle only";
            this.NextCircle.UseVisualStyleBackColor = true;
            // 
            // chkEcho
            // 
            this.chkEcho.AutoSize = true;
            this.chkEcho.Location = new System.Drawing.Point(12, 119);
            this.chkEcho.Name = "chkEcho";
            this.chkEcho.Size = new System.Drawing.Size(86, 17);
            this.chkEcho.TabIndex = 14;
            this.chkEcho.Text = "Echo Output";
            this.chkEcho.UseVisualStyleBackColor = true;
            // 
            // chkLog
            // 
            this.chkLog.AutoSize = true;
            this.chkLog.Location = new System.Drawing.Point(12, 142);
            this.chkLog.Name = "chkLog";
            this.chkLog.Size = new System.Drawing.Size(79, 17);
            this.chkLog.TabIndex = 15;
            this.chkLog.Text = "Log Output";
            this.chkLog.UseVisualStyleBackColor = true;
            // 
            // chkParse
            // 
            this.chkParse.AutoSize = true;
            this.chkParse.Location = new System.Drawing.Point(12, 164);
            this.chkParse.Name = "chkParse";
            this.chkParse.Size = new System.Drawing.Size(88, 17);
            this.chkParse.TabIndex = 16;
            this.chkParse.Text = "Parse Output";
            this.chkParse.UseVisualStyleBackColor = true;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(217, 254);
            this.Controls.Add(this.chkParse);
            this.Controls.Add(this.chkLog);
            this.Controls.Add(this.chkEcho);
            this.Controls.Add(this.NextCircle);
            this.Controls.Add(this.Post200Circle);
            this.Controls.Add(this.lblDisplay);
            this.Controls.Add(this.Normal);
            this.Controls.Add(this.cboSort);
            this.Controls.Add(this.lblSort);
            this.Controls.Add(this.chkGag);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOk);
            this.Name = "Form1";
            this.Text = "Circle Calculator";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.Button btnCancel;
        public System.Windows.Forms.CheckBox chkGag;
        private System.Windows.Forms.Label lblSort;
        public System.Windows.Forms.ComboBox cboSort;
        private System.Windows.Forms.Label lblDisplay;
        public System.Windows.Forms.RadioButton Normal;
        public System.Windows.Forms.RadioButton Post200Circle;
        public System.Windows.Forms.RadioButton NextCircle;
        public System.Windows.Forms.CheckBox chkEcho;
        public System.Windows.Forms.CheckBox chkLog;
        public System.Windows.Forms.CheckBox chkParse;
    }
}