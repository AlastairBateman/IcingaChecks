using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;

namespace check_ip_change {
    public class IPRecordDatabase : IDisposable {
        public class IPRecord {
            [JsonConverter(typeof(IPAddressConverter))]
            public IPAddress IPAddress { get; set; }
            public DateTime LastChecked { get; set; } = DateTime.UtcNow;
            public DateTime LastChanged { get; set; } = DateTime.UtcNow;
            public IPRecord(IPAddress ip) {
                IPAddress = ip;
            }
        }



        private Dictionary<string, IPRecord> Database { get; set; }
        private string DbFileName => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile, Environment.SpecialFolderOption.DoNotVerify), "ip_record_database.json");

        public IPRecordDatabase() {
            // If the file already exists, then load it.
            if (File.Exists(DbFileName)) {
                var text = File.ReadAllText(DbFileName);
                Database = JsonConvert.DeserializeObject<Dictionary<string, IPRecord>>(text);
            } else {
                // otherwise create a new one and save it.
                Database = new Dictionary<string, IPRecord>();
                Save();
            }
        }
        public void Add(string host, IPAddress ip) {
            if (Database.ContainsKey(host)) {
                // if the host exists in the database update the LastChecked timestamp.
                Database[host].LastChecked = DateTime.UtcNow;

                // if the entry has changed, update the IPAddress and LastChanged timestamp.
                if (Database[host].IPAddress != ip) {
                    Database[host].IPAddress = ip;
                    Database[host].LastChanged = DateTime.UtcNow;
                }
            } else {
                // otherwise add a new record
                Database.Add(host, new IPRecord(ip));
            }

            Save();
        }
        private void Save() {
            using var mutex = new Mutex(false, "file lock");
            var haveMutex = false;

            try {
                // try to acquire the mutex for 10 seconds.
                haveMutex = mutex.WaitOne(10000);
            } catch (AbandonedMutexException) {
                haveMutex = true;
            }

            if (!haveMutex) {
                // Haven't been able to acquire the mutex before timing out. 
                return;
            }

            // We have the mutex. Time to write to the file. 
            try {
                var text = JsonConvert.SerializeObject(Database);
                File.WriteAllText(DbFileName, text);
            } finally {
                mutex.ReleaseMutex();
            }
        }
        public override string ToString() {
            return JsonConvert.SerializeObject(Database, Formatting.Indented);
        }
        public IPAddress LookupHost(string host) {
            var val = Database.FirstOrDefault(x => x.Key == host);

            if (val.Key == null) {
                return null;
            } else {
                return val.Value?.IPAddress;
            }
        }
        public void Dispose() {
            Save();
        }
    }
}
