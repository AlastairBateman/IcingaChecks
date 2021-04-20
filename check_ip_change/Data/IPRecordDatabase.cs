using System.Threading.Tasks;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;

namespace check_ip_change.Data {
    public class IPRecordDatabase {
        public static async Task Add(string host, IPAddress ip) {
            using var db = new IPDbContext();

            var record = db.IPRecords.FirstOrDefault(x => x.Host == host);

            if (record == default(IPRecord)) {
                // if it's one that doesn't exist
                db.IPRecords.Add(new IPRecord(host, ip));
                await db.SaveChangesAsync();
            } else {
                // if the host exists in the database update the LastChecked timestamp.
                record.LastChecked = DateTime.UtcNow;

                // if the entry has changed, update the IPAddress and LastChanged timestamp.
                if (record.IPAddress != ip) {
                    record.IPAddress = ip;
                    record.LastChanged = DateTime.UtcNow;
                }
                db.IPRecords.Update(record);
                await db.SaveChangesAsync();
            }
        }
        public override string ToString() {
            using var db = new IPDbContext();

            var records = db.IPRecords.ToList();
            return JsonConvert.SerializeObject(records, Formatting.Indented);
        }
        public static IPAddress LookupHost(string host) {
            using var db = new IPDbContext();

            var record = db.IPRecords.FirstOrDefault(x => x.Host == host);

            if (record == default(IPRecord)) {
                return null;
            } else {
                return record.IPAddress;
            }
        }
    }
}
