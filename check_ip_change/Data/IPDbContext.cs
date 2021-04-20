using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace check_ip_change.Data {
    public class IPDbContext : DbContext {

        public DbSet<IPRecord> IPRecords { get; set; }
        private string DbFileName => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile, Environment.SpecialFolderOption.DoNotVerify), "ip_record_database.db");

        protected override void OnConfiguring(DbContextOptionsBuilder options) => options.UseSqlite($"Data Source={DbFileName}");

        public IPDbContext(): base() {
            if (!File.Exists(DbFileName)) {
                // a quick hack to ensure that the database is created if it doesn't exist. It doesn't currently check for validity.
                this.Database.EnsureCreated();
            }
        }
    }
}
