using System;
using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public class Form1 : Form
    {
        private TcpListener listener;
        private TcpClient client;
        private NetworkStream netStream;
        private CancellationTokenSource cts;

        private TextBox txtIP;
        private TextBox txtPort;
        private Button btnServer;
        private Button btnClient;
        private FlowLayoutPanel chatPanel;
        private TextBox txtMsg;
        private Button btnSend;
        private Label lblStatus;

        public Form1()
        {
            Text = "Pastel Chat 💜";
            Size = new Size(740, 620);
            BackColor = Color.FromArgb(255, 248, 252);
            StartPosition = FormStartPosition.CenterScreen;
            FormClosing += (s, e) => CloseConnections();

            BuildUI();
        }

        private void BuildUI()
        {
            Label lblIP = new Label() { Text = "IP:", Location = new Point(20, 15), ForeColor = Color.MediumVioletRed, AutoSize = true };
            Controls.Add(lblIP);

            txtIP = new TextBox() { Location = new Point(50, 12), Size = new Size(140, 26), Text = "127.0.0.1" };
            Controls.Add(txtIP);

            Label lblPort = new Label() { Text = "Port:", Location = new Point(200, 15), ForeColor = Color.MediumVioletRed, AutoSize = true };
            Controls.Add(lblPort);

            txtPort = new TextBox() { Location = new Point(250, 12), Size = new Size(80, 26), Text = "5000" };
            Controls.Add(txtPort);

            btnServer = new Button() { Text = "Sunucu Başlat", Location = new Point(350, 10), Size = new Size(120, 30), BackColor = Color.FromArgb(230, 210, 255) };
            btnServer.Click += BtnServer_Click;
            Controls.Add(btnServer);

            btnClient = new Button() { Text = "Bağlan", Location = new Point(480, 10), Size = new Size(80, 30), BackColor = Color.FromArgb(255, 240, 200) };
            btnClient.Click += BtnClient_Click;
            Controls.Add(btnClient);

            lblStatus = new Label() { Text = "Durum: Hazır", Location = new Point(580, 15), AutoSize = true, ForeColor = Color.DarkSlateGray };
            Controls.Add(lblStatus);

            chatPanel = new FlowLayoutPanel()
            {
                Location = new Point(20, 60),
                Size = new Size(680, 440),
                AutoScroll = true,
                BackColor = Color.FromArgb(245, 240, 255),
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false
            };
            Controls.Add(chatPanel);

            txtMsg = new TextBox() { Location = new Point(20, 515), Size = new Size(540, 60), Multiline = true, Font = new Font("Segoe UI", 10) };
            Controls.Add(txtMsg);

            btnSend = new Button() { Text = "Gönder 💌", Location = new Point(580, 515), Size = new Size(120, 60), BackColor = Color.FromArgb(255, 210, 235) };
            btnSend.Click += BtnSend_Click;
            Controls.Add(btnSend);
        }

        // Sunucu başlat
        private async void BtnServer_Click(object sender, EventArgs e)
        {
            if (!int.TryParse(txtPort.Text, out int port)) { MessageBox.Show("Geçerli port gir."); return; }

            try
            {
                listener = new TcpListener(IPAddress.Any, port);
                listener.Start();
                lblStatus.Text = $"Sunucu {port} dinliyor... 💜";
                AddBubble("Sunucu başlatıldı!", false);

                _ = Task.Run(async () =>
                {
                    client = await listener.AcceptTcpClientAsync();
                    netStream = client.GetStream();
                    cts = new CancellationTokenSource();
                    lblStatusInvoke("Bağlantı kuruldu!");
                    _ = ReceiveLoop(cts.Token);
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Sunucu hatası: " + ex.Message);
            }
        }

        // Bağlan (istemci)
        private async void BtnClient_Click(object sender, EventArgs e)
        {
            if (!int.TryParse(txtPort.Text, out int port)) { MessageBox.Show("Geçerli port gir."); return; }

            try
            {
                client = new TcpClient();
                await client.ConnectAsync(txtIP.Text.Trim(), port);
                netStream = client.GetStream();
                cts = new CancellationTokenSource();
                lblStatus.Text = "Bağlantı başarılı! 🔗";
                AddBubble("Sunucuya bağlanıldı!", true);
                _ = ReceiveLoop(cts.Token);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Bağlanma hatası: " + ex.Message);
            }
        }

        // Mesaj gönder
        private async void BtnSend_Click(object sender, EventArgs e)
        {
            string msg = txtMsg.Text.Trim();
            if (string.IsNullOrEmpty(msg) || client == null || !client.Connected) return;

            try
            {
                byte[] buffer = Encoding.UTF8.GetBytes(msg);
                await netStream.WriteAsync(buffer, 0, buffer.Length);
                await netStream.FlushAsync();

                AddBubble("🩷 " + msg, true);
                txtMsg.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Mesaj gönderilemedi: " + ex.Message);
            }
        }

        // Mesaj alma
        private async Task ReceiveLoop(CancellationToken token)
        {
            byte[] buffer = new byte[4096];
            try
            {
                while (!token.IsCancellationRequested)
                {
                    int read = await netStream.ReadAsync(buffer, 0, buffer.Length, token);
                    if (read == 0) break;
                    string msg = Encoding.UTF8.GetString(buffer, 0, read);
                    AddBubble("💬 " + msg, false);
                }
            }
            catch { }
        }

        // Mesaj balonları
        private void AddBubble(string text, bool mine)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => AddBubble(text, mine)));
                return;
            }

            Panel bubblePanel = new Panel()
            {
                AutoSize = true,
                Width = chatPanel.ClientSize.Width - 25,
                Padding = new Padding(5),
                BackColor = Color.Transparent
            };

            Label lbl = new Label()
            {
                Text = text,
                AutoSize = true,
                MaximumSize = new Size(420, 0),
                Font = new Font("Segoe UI", 10),
                Padding = new Padding(10),
                BackColor = mine ? Color.FromArgb(230, 210, 255) : Color.FromArgb(255, 230, 235),
                ForeColor = Color.Black
            };

            if (mine)
                lbl.Location = new Point(bubblePanel.Width - lbl.PreferredWidth - 15, 0);
            else
                lbl.Location = new Point(10, 0);

            bubblePanel.Controls.Add(lbl);
            chatPanel.Controls.Add(bubblePanel);
            chatPanel.ScrollControlIntoView(bubblePanel);
        }

        private void lblStatusInvoke(string text)
        {
            if (InvokeRequired) Invoke(new Action(() => lblStatus.Text = text));
            else lblStatus.Text = text;
        }

        private void CloseConnections()
        {
            try { cts?.Cancel(); } catch { }
            try { netStream?.Close(); } catch { }
            try { client?.Close(); } catch { }
            try { listener?.Stop(); } catch { }
        }

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.Run(new Form1());
        }
    }
}

