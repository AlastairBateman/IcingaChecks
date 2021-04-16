# IcingaChecks
A series of checks, plugins and commands for Icinga (nagios too)

## check_ip_change

Used to check when the IP address a hostname resolves to changes over time.

## check_tcp_state

Like `check_tcp` but you can add an expected state, so that you can be sure a port is blocked by a firewall and not just `closed`, or worse `open`._