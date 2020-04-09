namespace MotomanRS2TCP
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
            this.listBox1 = new System.Windows.Forms.ListBox();
            this.btnUp = new System.Windows.Forms.Button();
            this.btnDown = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.btnCurrentPos2SP = new System.Windows.Forms.Button();
            this.btnHomePos = new System.Windows.Forms.Button();
            this.btnSetPosVar = new System.Windows.Forms.Button();
            this.btnGetPosVar = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // listBox1
            // 
            this.listBox1.FormattingEnabled = true;
            this.listBox1.HorizontalScrollbar = true;
            this.listBox1.ItemHeight = 16;
            this.listBox1.Location = new System.Drawing.Point(12, 167);
            this.listBox1.Name = "listBox1";
            this.listBox1.Size = new System.Drawing.Size(968, 260);
            this.listBox1.TabIndex = 1;
            // 
            // btnUp
            // 
            this.btnUp.Location = new System.Drawing.Point(15, 15);
            this.btnUp.Name = "btnUp";
            this.btnUp.Size = new System.Drawing.Size(75, 23);
            this.btnUp.TabIndex = 2;
            this.btnUp.Text = "Up";
            this.btnUp.UseVisualStyleBackColor = true;
            this.btnUp.Click += new System.EventHandler(this.btnUp_Click);
            // 
            // btnDown
            // 
            this.btnDown.Location = new System.Drawing.Point(15, 44);
            this.btnDown.Name = "btnDown";
            this.btnDown.Size = new System.Drawing.Size(75, 23);
            this.btnDown.TabIndex = 3;
            this.btnDown.Text = "Down";
            this.btnDown.UseVisualStyleBackColor = true;
            this.btnDown.Click += new System.EventHandler(this.btnDown_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(189, 15);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(114, 17);
            this.label1.TabIndex = 7;
            this.label1.Text = "Position Setpoint";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(189, 54);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(114, 17);
            this.label2.TabIndex = 8;
            this.label2.Text = "Position Variable";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(189, 89);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(109, 17);
            this.label3.TabIndex = 9;
            this.label3.Text = "Current Position";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(418, 15);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(46, 17);
            this.label4.TabIndex = 10;
            this.label4.Text = "label4";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(418, 54);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(46, 17);
            this.label5.TabIndex = 11;
            this.label5.Text = "label5";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(418, 89);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(46, 17);
            this.label6.TabIndex = 12;
            this.label6.Text = "label6";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(124, 15);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(46, 17);
            this.label7.TabIndex = 13;
            this.label7.Text = "label7";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(12, 138);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(48, 17);
            this.label8.TabIndex = 14;
            this.label8.Text = "Status";
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(87, 138);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(30, 17);
            this.label9.TabIndex = 15;
            this.label9.Text = "Idle";
            // 
            // btnCurrentPos2SP
            // 
            this.btnCurrentPos2SP.Location = new System.Drawing.Point(309, 12);
            this.btnCurrentPos2SP.Name = "btnCurrentPos2SP";
            this.btnCurrentPos2SP.Size = new System.Drawing.Size(93, 23);
            this.btnCurrentPos2SP.TabIndex = 16;
            this.btnCurrentPos2SP.Text = "SetCurrent";
            this.btnCurrentPos2SP.UseVisualStyleBackColor = true;
            this.btnCurrentPos2SP.Click += new System.EventHandler(this.btnCurrentPos2SP_Click);
            // 
            // btnHomePos
            // 
            this.btnHomePos.Location = new System.Drawing.Point(15, 73);
            this.btnHomePos.Name = "btnHomePos";
            this.btnHomePos.Size = new System.Drawing.Size(75, 23);
            this.btnHomePos.TabIndex = 17;
            this.btnHomePos.Text = "Home";
            this.btnHomePos.UseVisualStyleBackColor = true;
            this.btnHomePos.Click += new System.EventHandler(this.btnHomePos_Click);
            // 
            // btnSetPosVar
            // 
            this.btnSetPosVar.Location = new System.Drawing.Point(304, 51);
            this.btnSetPosVar.Name = "btnSetPosVar";
            this.btnSetPosVar.Size = new System.Drawing.Size(46, 23);
            this.btnSetPosVar.TabIndex = 18;
            this.btnSetPosVar.Text = "Set";
            this.btnSetPosVar.UseVisualStyleBackColor = true;
            this.btnSetPosVar.Click += new System.EventHandler(this.btnSetPosVar_Click);
            // 
            // btnGetPosVar
            // 
            this.btnGetPosVar.Location = new System.Drawing.Point(356, 51);
            this.btnGetPosVar.Name = "btnGetPosVar";
            this.btnGetPosVar.Size = new System.Drawing.Size(46, 23);
            this.btnGetPosVar.TabIndex = 19;
            this.btnGetPosVar.Text = "Get";
            this.btnGetPosVar.UseVisualStyleBackColor = true;
            this.btnGetPosVar.Click += new System.EventHandler(this.btnGetPosVar_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(992, 439);
            this.Controls.Add(this.btnGetPosVar);
            this.Controls.Add(this.btnSetPosVar);
            this.Controls.Add(this.btnHomePos);
            this.Controls.Add(this.btnCurrentPos2SP);
            this.Controls.Add(this.label9);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.btnDown);
            this.Controls.Add(this.btnUp);
            this.Controls.Add(this.listBox1);
            this.Name = "Form1";
            this.Text = "Motoman RS2TCP";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.ListBox listBox1;
        private System.Windows.Forms.Button btnUp;
        private System.Windows.Forms.Button btnDown;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Button btnCurrentPos2SP;
        private System.Windows.Forms.Button btnHomePos;
        private System.Windows.Forms.Button btnSetPosVar;
        private System.Windows.Forms.Button btnGetPosVar;
    }
}

