using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Net.Sockets;

namespace check_tcp_state {
    class Program {
        public enum PortState {
            open,
            closed,
            filtered,
            not_open,
            unknown
        }
        private static int NAGIOS_OK => 0;
        private static int NAGIOS_WARNING => 1;
        private static int NAGIOS_CRITICAL => 2;
        private static int NAGIOS_UNKNOWN => 3;

        static void Main(string[] args) {
            var cmd = new RootCommand {
                new Option<string>(new [] {"-H","--host"},"The host to check") { IsRequired = true },
                new Option<int>(new [] {"-p","--port"},"The port to check") { IsRequired = true },
                new Option<int>(new []{"-t","--timeout" }, () => 5000, "How long to wait for a response (in milliseconds)") { IsRequired = false },
                new Option<string>(new [] {"-s","--expected-state"},"The state you expect the check to return") { IsRequired = true },
            };

            if (args.Length == 0) {
                Console.WriteLine("UNKNOWN - no commandline arguments specified");
                Environment.Exit(NAGIOS_UNKNOWN);
            }
            cmd.Description = "A check to see if a port returns an expected state";

            cmd.Handler = CommandHandler.Create<string, int, int, PortState>((host, port, timeout, expectedState) => {

                var testStart = DateTime.UtcNow;
                var actualState = TestPort(host, port, timeout);
                var testEnd = DateTime.UtcNow;
                var testTime = testEnd - testStart;

                if (actualState == expectedState) {
                    Console.WriteLine($"OK - port is {actualState} as expected. Test took {testTime.TotalMilliseconds:N3}ms");

                } else if (expectedState == PortState.not_open && (actualState == PortState.filtered || actualState == PortState.closed)) {
                    Console.WriteLine($"OK - port is {actualState} as expected (not_open). Test took {testTime.TotalMilliseconds:N3}ms");
                } else {
                    Console.WriteLine($"CRITICAL - port is {actualState} where it should be {expectedState}. Test took {testTime.TotalMilliseconds:N3}ms");
                }
            });

            var result = cmd.InvokeAsync(args).Result;

            Environment.Exit(result);
        }

        private static PortState TestPort(string host, int port, int timeout) {
            var actualState = PortState.unknown;

            try {
                using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                var result = socket.BeginConnect(host, port, null, null);

                bool completedInTime = result.AsyncWaitHandle.WaitOne(timeout, true);

                if (completedInTime == true) {
                    if (socket.Connected) {
                        actualState = PortState.open;
                    } else {
                        actualState = PortState.closed;
                    }
                } else {
                    actualState = PortState.filtered;
                }

                socket.Close();

            } catch (Exception ex) {
                // No connection could be made because the target machine actively refused it
                if (ex.Message.StartsWith("No connection could be made because the target machine actively refused it")) {
                    actualState = PortState.closed;
                } else if (ex.Message.StartsWith("A connection attempt failed because the connected party did not properly respond after a period of time, or established connection failed because connected host has failed to respond."))
                    actualState = PortState.filtered;
                else {
                    Console.WriteLine($"UNKNOWN - Unexpected exception message: {ex.Message}", ex);
                    Environment.Exit(NAGIOS_UNKNOWN);
                }
            }

            return actualState;
        }
    }
}
