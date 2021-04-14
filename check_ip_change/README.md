# check_ip_change

Tracks IP address changes.

## Notes

- This stores the data in a plain-text file in `Environment.SpecialFolder.UserProfile`
 (home folder). There's nothing really sensitive in it but be aware that the file exists.
- This only checks the first result of a DNS lookup `Dns.GetHostEntry(host).AddressList.First()`.
 If a DNS lookup produces multiple IP addresses, then you might get differing results.
- Currently it only alerts on a change (the first time, or when a result is different 
from a previous one). Depending on your monitoring frequency you might notice 

## Building/Publishing

You can create a single executable with the following command. The below example command uses `linux-x64` as an example platform. You can find a list of runtime identifiers [here](https://docs.microsoft.com/en-us/dotnet/core/rid-catalog#using-rids).

```
dotnet publish --self-contained --runtime linux-x64 -p:PublishSingleFile=true
```

You can then copy the resulting executable to your plugin folder (again, using Linux as an example).

```
sudo cp bin/Debug/net5.0/linux-x64/publish/check_ip_change /usr/lib/nagios/plugins/
```

## Usage

```
check_ip_change:
  A check to see if the IP address for a given host has changed since it was last checked.

Usage:
  check_ip_change [options]

Options:
  -H, --host <h> (REQUIRED)    The hostname to check
  --version                    Display version information
```

## Examples

Check to see whether the IP address for `google.com` resolves as.

```
check_ip_change --host google.com
```

The first time you run the check you'll get a response like this:

```
WARNING - First check of host google.com. Current IP address is 216.58.199.46. Test took 28.032ms
```

On subsequent checks (running the same command) it **should** respond with something like:

```
OK - No change in IP address for google.com: 216.58.199.46. Test took 136.437ms
```

## Icinga 2 Config

I set up the monitoring using [Icinga 2](https://icinga.org). More information 
about Icinga 2 object configuration can be found [here](https://icinga.com/docs/icinga-2/latest/doc/04-configuration/).

```
object CheckCommand "ip_change" {
  command = [ "/usr/lib/nagios/plugins/check_ip_change" ]
  arguments = {
    "--host" = "$host$"
  }
}
```

I then set up the following service.

```
apply Service "IP Change" for (config in host.vars.host_ip_changes) {
  import "generic-service"
  check_command = "ip_change"
  vars.host = config
}
```

And you can then define the host variables like this.

```
object Host "Example Host" {
  import "generic-host"
  address = "XXX.XXX.XXX.XXX"
  vars.location = "XXXXXXX"
  vars.host_ip_changes = [
    "google.com",
    "microsoft.com",
    "facebook.com"
  ]
}
```