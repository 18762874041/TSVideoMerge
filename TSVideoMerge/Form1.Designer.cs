namespace TSVideoMerge
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            label1 = new Label();
            textBox1 = new TextBox();
            button1 = new Button();
            button2 = new Button();
            dataGridView1 = new DataGridView();
            colPath = new DataGridViewTextBoxColumn();
            colFileName = new DataGridViewTextBoxColumn();
            colFileCount = new DataGridViewTextBoxColumn();
            colProgress = new DataGridViewTextBoxColumn();
            ((System.ComponentModel.ISupportInitialize)dataGridView1).BeginInit();
            SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(39, 54);
            label1.Name = "label1";
            label1.Size = new Size(181, 35);
            label1.TabIndex = 0;
            label1.Text = "TS文件路径：";
            // 
            // textBox1
            // 
            textBox1.BorderStyle = BorderStyle.FixedSingle;
            textBox1.Location = new Point(203, 52);
            textBox1.Margin = new Padding(10);
            textBox1.Name = "textBox1";
            textBox1.Size = new Size(914, 42);
            textBox1.TabIndex = 1;
            textBox1.WordWrap = false;
            textBox1.Click += textBox1_Click;
            // 
            // button1
            // 
            button1.Location = new Point(1130, 45);
            button1.Name = "button1";
            button1.Size = new Size(169, 52);
            button1.TabIndex = 2;
            button1.Text = "扫描";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // button2
            // 
            button2.Location = new Point(1317, 45);
            button2.Name = "button2";
            button2.Size = new Size(169, 52);
            button2.TabIndex = 3;
            button2.Text = "合并";
            button2.UseVisualStyleBackColor = true;
            button2.Click += button2_Click;
            // 
            // dataGridView1
            // 
            dataGridView1.AllowUserToAddRows = false;
            dataGridView1.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridView1.Columns.AddRange(new DataGridViewColumn[] { colPath, colFileName, colFileCount, colProgress });
            dataGridView1.Location = new Point(28, 125);
            dataGridView1.Name = "dataGridView1";
            dataGridView1.RowHeadersWidth = 92;
            dataGridView1.Size = new Size(1469, 583);
            dataGridView1.TabIndex = 4;
            // 
            // colPath
            // 
            colPath.HeaderText = "目录";
            colPath.MinimumWidth = 11;
            colPath.Name = "colPath";
            colPath.Visible = false;
            colPath.Width = 225;
            // 
            // colFileName
            // 
            colFileName.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            colFileName.HeaderText = "目录名称";
            colFileName.MinimumWidth = 11;
            colFileName.Name = "colFileName";
            colFileName.ReadOnly = true;
            // 
            // colFileCount
            // 
            colFileCount.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            colFileCount.HeaderText = "TS文件数";
            colFileCount.MinimumWidth = 11;
            colFileCount.Name = "colFileCount";
            colFileCount.ReadOnly = true;
            colFileCount.Width = 176;
            // 
            // colProgress
            // 
            colProgress.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            colProgress.HeaderText = "进度(%)";
            colProgress.MinimumWidth = 11;
            colProgress.Name = "colProgress";
            colProgress.ReadOnly = true;
            colProgress.Width = 160;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(16F, 35F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1522, 734);
            Controls.Add(dataGridView1);
            Controls.Add(button2);
            Controls.Add(button1);
            Controls.Add(textBox1);
            Controls.Add(label1);
            Name = "Form1";
            Text = "TS视频文件合并";
            Load += Form1_Load;
            ((System.ComponentModel.ISupportInitialize)dataGridView1).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label label1;
        private TextBox textBox1;
        private Button button1;
        private Button button2;
        private DataGridView dataGridView1;
        private DataGridViewTextBoxColumn colPath;
        private DataGridViewTextBoxColumn colFileName;
        private DataGridViewTextBoxColumn colFileCount;
        private DataGridViewTextBoxColumn colProgress;
    }
}
