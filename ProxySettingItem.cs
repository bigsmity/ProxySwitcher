using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProxySwitcher
{
    public class ProxySettingItem
    {
        public string Name { get; set; }
        public string NetworkID { get; set; }
        public string Proxy { get; set; }
        public string Port { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public bool Bypass { get; set; }
    }
}

