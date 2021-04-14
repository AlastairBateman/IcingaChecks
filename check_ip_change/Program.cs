using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Linq;
using System.Net;

namespace check_ip_change {
    class Program {
        public enum NagiosResponseCode {
            OK = 0,
            WARNING = 1,
            CRITICAL = 2,
            UNKNOWN = 3
        }
        static void Main(string[] args) {
            var cmd = new RootCommand {
                new Option<string>(new [] {"-H","--host"},"The hostname/FQDN to check") { Required = true }
            };

            if (args.Length == 0) {
                DoNagiosResponse(NagiosResponseCode.UNKNOWN, "no commandline arguments specified");
            }
            cmd.Description = "A check to see if the IP address for a given host has changed since it was last checked.";

            cmd.Handler = CommandHandler.Create<string>((host) => {

                var testStart = DateTime.UtcNow;
                using var db = new IPRecordDatabase();

                var prev = db.LookupHost(host);
                var curr = Dns.GetHostEntry(host).AddressList.First();

                var testEnd = DateTime.UtcNow;
                var testTime = testEnd - testStart;

                db.Add(host, curr);

                if (curr == null) {
                    DoNagiosResponse(NagiosResponseCode.CRITICAL, $"Unable to obtain valid IP address for {host}. Test took {testTime.TotalMilliseconds:N3}ms");
                } else if (prev == null) {
                    DoNagiosResponse(NagiosResponseCode.WARNING, $"First check of host {host}. Current IP address is {curr}. Test took {testTime.TotalMilliseconds:N3}ms");
                } else if (prev.ToString() == curr.ToString()) {
                    DoNagiosResponse(NagiosResponseCode.OK, $"No change in IP address for {host}: {curr}. Test took {testTime.TotalMilliseconds:N3}ms");
                } else {
                    DoNagiosResponse(NagiosResponseCode.CRITICAL, $"IP address for {host} has changed. Was {prev}, now {curr}. Test took {testTime.TotalMilliseconds:N3}ms");
                }
            });

            var result = cmd.InvokeAsync(args).Result;

            Environment.Exit(result);
        }

        static void DoNagiosResponse(NagiosResponseCode code, string message) {
            Console.WriteLine($"{code} - {message}");
            Environment.Exit((int)code);
        }
    }
}
