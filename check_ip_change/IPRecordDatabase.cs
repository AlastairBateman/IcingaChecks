using System.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace check_ip_change {
    public class IPRecordDatabase : IDisposable {
        public class IPRecord {
            [JsonConverter(typeof(IPAddressConverter))]
            public IPAddress IPAddress { get; set; }
            public DateTime LastChecked { get; set; } = DateTime.Now;

            public IPRecord(IPAddress ip) {
                IPAddress = ip;
            }
        }

        private Dictionary<string, IPRecord> Database { get; set; } = new Dictionary<string, IPRecord>();
        private string DbFileName => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile, Environment.SpecialFolderOption.DoNotVerify), "ip_record_database.json");

        public IPRecordDatabase() {
            Load();
        }

        private void Load() {
            if (File.Exists(DbFileName)) {
                var text = File.ReadAllText(DbFileName);

                Database = JsonConvert.DeserializeObject<Dictionary<string, IPRecord>>(text);
            } else {
                Database = new Dictionary<string, IPRecord>();
            }
        }
        public void Add(string host, IPAddress ip) {
            var record = new IPRecord(ip);
            if (Database.ContainsKey(host)) {
                Database[host] = record;
            } else {
                Database.Add(host, record);
            }

            Save();
        }
        private void Save() {
            var text = JsonConvert.SerializeObject(Database);
            File.WriteAllText(DbFileName, text);
        }
        public override string ToString() {
            return JsonConvert.SerializeObject(Database, Formatting.Indented);
        }
        public IPAddress LookupHost(string host) {
            return Database.FirstOrDefault(x => x.Key == host).Value?.IPAddress;
        }
        public void Dispose() {
            Save();
        }
    }
}
