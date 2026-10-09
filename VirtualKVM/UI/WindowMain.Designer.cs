namespace VirtualKVM
{
    partial class WindowMain
    {
        /// <summary>
        /// 필수 디자이너 변수입니다.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 사용 중인 모든 리소스를 정리합니다.
        /// </summary>
        /// <param name="disposing">관리되는 리소스를 삭제해야 하면 true이고, 그렇지 않으면 false입니다.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form 디자이너에서 생성한 코드

        /// <summary>
        /// 디자이너 지원에 필요한 메서드입니다. 
        /// 이 메서드의 내용을 코드 편집기로 수정하지 마세요.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.notifyIcon = new System.Windows.Forms.NotifyIcon(this.components);
            this.contextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.MNU_EXIT = new System.Windows.Forms.ToolStripMenuItem();
            this.MNU_CONNECT = new System.Windows.Forms.ToolStripMenuItem();
            this.MNU_DISCONNECT = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.MNU_CONFIG = new System.Windows.Forms.ToolStripMenuItem();
            this.contextMenuStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // notifyIcon
            // 
            this.notifyIcon.BalloonTipIcon = System.Windows.Forms.ToolTipIcon.Info;
            this.notifyIcon.ContextMenuStrip = this.contextMenuStrip;
            this.notifyIcon.Text = "VirtualKVM";
            this.notifyIcon.Visible = true;
            // 
            // contextMenuStrip
            // 
            this.contextMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.MNU_CONNECT,
            this.MNU_DISCONNECT,
            this.toolStripSeparator1,
            this.MNU_CONFIG,
            this.toolStripSeparator2,
            this.MNU_EXIT});
            this.contextMenuStrip.Name = "contextMenuStrip";
            this.contextMenuStrip.Size = new System.Drawing.Size(163, 104);
            this.contextMenuStrip.TabStop = true;
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(159, 6);
            // 
            // MNU_EXIT
            // 
            this.MNU_EXIT.Name = "MNU_EXIT";
            this.MNU_EXIT.Size = new System.Drawing.Size(162, 22);
            this.MNU_EXIT.Text = "Exit(&E)";
            // 
            // MNU_CONNECT
            // 
            this.MNU_CONNECT.Name = "MNU_CONNECT";
            this.MNU_CONNECT.Size = new System.Drawing.Size(162, 22);
            this.MNU_CONNECT.Text = "Connect(&C)";
            // 
            // MNU_DISCONNECT
            // 
            this.MNU_DISCONNECT.Enabled = false;
            this.MNU_DISCONNECT.Name = "MNU_DISCONNECT";
            this.MNU_DISCONNECT.Size = new System.Drawing.Size(162, 22);
            this.MNU_DISCONNECT.Text = "Disconnect(&D)";
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(159, 6);
            // 
            // MNU_CONFIG
            // 
            this.MNU_CONFIG.Name = "MNU_CONFIG";
            this.MNU_CONFIG.Size = new System.Drawing.Size(162, 22);
            this.MNU_CONFIG.Text = "Configuration(&T)";
            // 
            // WindowMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(120, 0);
            this.Name = "WindowMain";
            this.Text = "Window";
            this.Load += new System.EventHandler(this.WindowMain_Load);
            this.contextMenuStrip.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.NotifyIcon notifyIcon;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripMenuItem MNU_EXIT;
        private System.Windows.Forms.ToolStripMenuItem MNU_CONNECT;
        private System.Windows.Forms.ToolStripMenuItem MNU_DISCONNECT;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem MNU_CONFIG;
    }
}

