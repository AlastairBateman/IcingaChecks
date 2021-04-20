using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Net;

namespace check_ip_change.Data {
    public class IPRecord {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Guid { get; set; }

        public string Host { get; set; }
        public IPAddress IPAddress { get; set; }
        public DateTime LastChecked { get; set; } = DateTime.UtcNow;
        public DateTime LastChanged { get; set; } = DateTime.UtcNow;
        public IPRecord(string host, IPAddress ip) {
            Host = host;
            IPAddress = ip;
        }
        public IPRecord() { }
    }
}
