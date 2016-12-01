using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Timers;
using System.Management;
using NETWORKLIST;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace ProxySwitcher
{
    public partial class FormConfig : Form
    {
        [DllImport("wininet.dll")]
        public static extern bool InternetSetOption(IntPtr hInternet, int dwOption, IntPtr lpBuffer, int dwBufferLength);
        public const int INTERNET_OPTION_SETTINGS_CHANGED = 39;
        public const int INTERNET_OPTION_REFRESH = 37;
        static bool settingsReturn, refreshReturn;

        private List<string> ConnectedNetworks;
        private List<ProxySettingItem> ProxyEntries;
        private ProxySettingItem CurrentlyAppliedProxy;
        private System.Timers.Timer proxyTimer;

        public FormConfig()
        {
            InitializeComponent();
            this.WindowState = FormWindowState.Minimized;
            this.ShowInTaskbar = false;

            GetConnectedNetworkNames();
            LoadJson();
            LoadUIList();
            StartTimer();
        }

        private void StartTimer()
        {
            proxyTimer = new System.Timers.Timer(5000);
            proxyTimer.Elapsed += new ElapsedEventHandler(CheckNetwork);
            proxyTimer.Enabled = true;
            proxyTimer.Start();
        }

        private void CheckNetwork(object sender, ElapsedEventArgs e)
        {
            GetConnectedNetworkNames();

            //if current network already has proxy set
            if (CurrentlyAppliedProxy != null)
            {
                foreach (string network in ConnectedNetworks)
                {
                    if (!string.IsNullOrEmpty(CurrentlyAppliedProxy.NetworkID) && CurrentlyAppliedProxy.NetworkID.ToLower() == network.ToLower())
                    {
                        return;
                    }
                }
            }

            foreach (string network in ConnectedNetworks)
            {
                foreach (ProxySettingItem proxy in ProxyEntries)
                {
                    if (proxy != CurrentlyAppliedProxy && !string.IsNullOrEmpty(proxy.NetworkID) && network.ToLower() == proxy.NetworkID.ToLower())
                    {
                        //match found
                        SetProxy(proxy);
                        return;
                    }
                }
            }

            ProxySettingItem defaultProxy = ProxyEntries.SingleOrDefault(x => x.Name.ToLower() == "default");
            if (defaultProxy != CurrentlyAppliedProxy)
            {
                SetProxy(defaultProxy);
            }
        }

        private void SetProxy(ProxySettingItem proxy)
        {
            CurrentlyAppliedProxy = proxy;
            RegistryKey registry = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings", true);

            if (proxy == null || (proxy != null && string.IsNullOrEmpty(proxy.Proxy) && string.IsNullOrEmpty(proxy.Port)))
            {
                registry.SetValue("ProxyEnable", 0);
                registry.SetValue("ProxyServer", "");
                registry.SetValue("ProxyUser", "");
                registry.SetValue("ProxyPass", "");
                registry.SetValue("ProxyOverride", "");
            }
            else
            {
                registry.SetValue("ProxyEnable", 1);
                registry.SetValue("ProxyServer", proxy.Proxy + ":" + proxy.Port);
                if (!string.IsNullOrEmpty(proxy.Username) && !string.IsNullOrEmpty(proxy.Password))
                {
                    registry.SetValue("ProxyUser", proxy.Username);
                    registry.SetValue("ProxyPass", StringEncryption.Unprotect(proxy.Password));
                }
                else
                {
                    registry.SetValue("ProxyUser", "");
                    registry.SetValue("ProxyPass", "");
                }

                if (proxy.Bypass)
                {
                    registry.SetValue("ProxyOverride", "<local>");
                }
                else
                {
                    registry.SetValue("ProxyOverride", "");
                }
            }

            settingsReturn = InternetSetOption(IntPtr.Zero, INTERNET_OPTION_SETTINGS_CHANGED, IntPtr.Zero, 0);
            refreshReturn = InternetSetOption(IntPtr.Zero, INTERNET_OPTION_REFRESH, IntPtr.Zero, 0);

            notifyIcon1.BalloonTipText = "Proxy Change (" + proxy.Name + ":" + proxy.NetworkID + ":" + proxy.Proxy + ":" + proxy.Port + ")";
            notifyIcon1.ShowBalloonTip(3000);
        }

        private void GetConnectedNetworkNames()
        {
            ConnectedNetworks = new List<string>();
            var manager = new NetworkListManagerClass();
            var connectedNetworks = manager.GetNetworks(NLM_ENUM_NETWORK.NLM_ENUM_NETWORK_CONNECTED).Cast<INetwork>();
            foreach (var network in connectedNetworks)
            {
                ConnectedNetworks.Add(network.GetName());
            }
        }

        private void listMenu_Opening(object sender, CancelEventArgs e)
        {
            if (lstSettings.SelectedItem == null)
            {
                deleteToolStripMenuItem.Enabled = false;
            }
            else
            {
                deleteToolStripMenuItem.Enabled = true;
            }
        }

        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ProxySettingItem item = new ProxySettingItem();
            item.Name = "New Item";
            this.ProxyEntries.Add(item);
            LoadUIList();
        }

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (lstSettings.SelectedItem == null)
            {
                return;
            }
            else
            {
                int selectedIndex = lstSettings.SelectedIndex;
                if (selectedIndex < 0)
                {
                    return;
                }

                if (selectedIndex < lstSettings.Items.Count)
                {
                    var entry = this.ProxyEntries[selectedIndex];
                    if (entry != null)
                    {
                        if (entry.Name.ToLower() == "default")
                        {
                            return;
                        }
                        this.ProxyEntries.Remove(entry);
                    }

                    SaveJson();
                    ClearControls();
                    LoadJson();
                    LoadUIList();
                }
            }
        }

        public void LoadJson()
        {
            this.ProxyEntries = new List<ProxySwitcher.ProxySettingItem>();
            using (StreamReader r = new StreamReader("proxySettings.json"))
            {
                string json = r.ReadToEnd();
                this.ProxyEntries = JsonConvert.DeserializeObject<List<ProxySettingItem>>(json);
            }

            if (this.ProxyEntries.Count(x => x.Name.ToLower() == "default") == 0)
            {
                ProxySettingItem item = new ProxySettingItem();
                item.Name = "Default";
                this.ProxyEntries.Add(item);

                SaveJson();
            }
        }

        public void SaveJson()
        {
            string jsonData = JsonConvert.SerializeObject(this.ProxyEntries.ToArray(), Formatting.Indented);
            System.IO.File.WriteAllText("proxySettings.json", jsonData);
        }

        public void LoadUIList()
        {
            lstSettings.Items.Clear();
            foreach (ProxySettingItem item in this.ProxyEntries)
            {
                lstSettings.Items.Add(item.Name);
            }
        }

        private void lstSettings_SelectedIndexChanged(object sender, EventArgs e)
        {
            int index = lstSettings.SelectedIndex;
            if (index < 0)
            {
                return;
            }

            if (index < this.ProxyEntries.Count)
            {
                var item = this.ProxyEntries[index];
                if (item != null)
                {
                    tbName.Text = item.Name;
                    tbNetwork.Text = item.NetworkID;
                    tbProxy.Text = item.Proxy;
                    tbPort.Text = item.Port;
                    tbUsername.Text = item.Username;
                    tbPassword.Text = StringEncryption.Unprotect(item.Password);
                    cbBypass.Checked = item.Bypass;
                }
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            int index = lstSettings.SelectedIndex;
            if (index < 0)
            {
                return;
            }

            if (index < this.ProxyEntries.Count)
            {
                var item = this.ProxyEntries[index];
                if (item != null)
                {
                    item.Name = tbName.Text;
                    item.NetworkID = tbNetwork.Text;
                    item.Proxy = tbProxy.Text;
                    item.Port = tbPort.Text;
                    item.Username = tbUsername.Text;
                    item.Password = StringEncryption.Protect(tbPassword.Text);
                    item.Bypass = cbBypass.Checked;
                }
                SaveJson();
                LoadUIList();
            }
        }

        private void ClearControls()
        {
            tbName.Text = "";
            tbNetwork.Text = "";
            tbProxy.Text = "";
            tbPort.Text = "";
            tbUsername.Text = "";
            tbPassword.Text = "";
            cbBypass.Checked = false;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            ClearControls();
            LoadJson();
            LoadUIList();
        }

        private void FormConfig_Resize(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                notifyIcon1.Visible = true;
                this.ShowInTaskbar = false;
            }
        }

        private void FormConfig_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason != CloseReason.WindowsShutDown && e.CloseReason != CloseReason.TaskManagerClosing)
            {
                e.Cancel = true;
                this.WindowState = FormWindowState.Minimized;
                notifyIcon1.Visible = true;
                this.ShowInTaskbar = false;
            }
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Normal;
            this.ShowInTaskbar = true;
            notifyIcon1.Visible = false;
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            proxyTimer.Stop();
            proxyTimer.Dispose();
            Application.Exit();
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            this.WindowState = FormWindowState.Normal;
            this.ShowInTaskbar = true;
            notifyIcon1.Visible = false;
        }
    }
}
