# check_tcp_state

This was written as a supplement to the nagios plugin [check_tcp](https://github.com/nagios-plugins/nagios-plugins/blob/master/plugins/check_tcp.c).

That's great for checking if a port is open, but sometimes you want to be 
sure that a port is closed, when it should be (or open, or filtered). 
`check_tcp` is fine, however it will only tell you if a port is open and 
throw an alert if it is inaccessible. Sometimes you want to make sure it 
isn't available and throw an alert if it is open.

I started writing in bash script, but then decide dotnetcore/c# would be 
more fun and portable. 

A few notes:
- The original reason for creating this check it I had a large number of 
services open that I wanted to keep an eye on as they were closed off. 
- The expected states (`open`, `closed`, `filtered`) are based around a 
simplified version of the [nmap definitions](https://nmap.org/book/man-port-scanning-basics.html)
- `filtered` doesn't necessarily mean a firewall is blocking packets, it 
just means that you didn't get a response before the `timeout` ran out. 
This could happen if you set an unnecessarily low `timeout` value (i.e. if 
you forget it's in milliseconds)

## Building/Publishing

You can create a single executable with the following command. The below example command uses `linux-x64` as an example platform. You can find a list of runtime identifiers [here](https://docs.microsoft.com/en-us/dotnet/core/rid-catalog#using-rids).

```
dotnet publish --self-contained --runtime linux-x64 -p:PublishSingleFile=true
```

You can then copy the resulting executable to your plugin folder (again, using Linux as an example).

```
sudo cp bin/Debug/net5.0/linux-x64/publish/check_tcp_state /usr/lib/nagios/plugins/
```

## Usage

```
check_tcp_state:
  A check to see if a port returns an expected state

Usage:
  check_tcp_state [options]

Options:
  -H, --host <host> (REQUIRED)                        The host to check
  -p, --port <port> (REQUIRED)                        The port to check
  -t, --timeout <timeout>                             How long to wait for a response (in milliseconds) [default:5000]
  -s, --expected-state <expected-state> (REQUIRED)    The state you expect the check to return
  --version                                           Show version information
  -?, -h, --help                                      Show help and usage information
```

## Examples

Check that `google.com` responds on port 443, which it should.

```
check_tcp_state --host google.com --port 443  --expected-state open
```

Check that `google.com` does not respond on port 444, which it shouldn't.

```
check_tcp_state --host google.com --port 444  --expected-state filtered
```

Check that port 80 on host `cert.example.com` is closed. This is the case 
(in my experience anyway) when LetsEncrypt sits on port 80 so that it can 
accept certificate authorisation requests, but only activates when it's 
actually approving a certificate. The rest of the time it's just a closed 
port (not filtered, but not open either).

```
check_tcp_state --host cert.example.com --port 80  --expected-state closed
```

## Icinga 2 Config

I set up the monitoring using [Icinga 2](https://icinga.org). More information 
about Icinga 2 object configuration can be found [here](https://icinga.com/docs/icinga-2/latest/doc/04-configuration/).

```
object CheckCommand "tcp_state" {
  command = [ "/usr/lib/nagios/plugins/check_tcp_state" ]
  arguments = {
    "--host" = "$address$"
    "--port" = "$port$"
    "--timeout" = "$timeout$"
    "--expected-state" = "$expectedState$"
  }
}
```

I then set up the following service.

```
apply Service "Filtered Port" for (config in host.vars.filtered_ports) {
  import "generic-service"
  check_command = "tcp_state"

  vars.timeout = 3000
  vars.port    = config
  vars.expectedState = "filtered"
}
```

And you can then define the host variables like this.

```
object Host "Example Host" {
  import "generic-host"
  address = "XXX.XXX.XXX.XXX"
  vars.location = "XXXXXXX"
  vars.filtered_ports = [
    "22",
    "445",
    "1433"
  ]
}
```