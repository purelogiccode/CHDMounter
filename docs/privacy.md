# Privacy and Telemetry

CHDMounter keeps everything local by default. Two optional, best-effort services send data over
the network: **anonymous usage statistics** and **bug reports**. This page describes exactly what
is sent.

## Summary

| Data | Sent when | Contains |
|------|-----------|----------|
| Usage statistics | Once per application launch | Application id and version only |
| Bug reports | When the application logs a warning or error | Error/exception details and environment information (see below) |
| Diagnostic logs | Never | Log files always stay in the local application data folder |
| Screenshots | Never | F8 captures are saved to local folders only |

## Usage statistics

On every launch CHDMounter sends a single HTTP `POST` to:

```text
https://www.purelogiccode.com/ApplicationStats/stats
```

The JSON body contains only:

```json
{
  "applicationId": "chdmounter",
  "version": "1.3.1"
}
```

- The call is made at most **once per process** and runs in the background.
- Failures (no network, server down, rate limit) are logged locally at Information level and
  never affect the application.
- No machine identifiers, user names, file names or IP addresses are included in the payload.
  (The server records the caller IP and geolocation for aggregate country statistics.)

## Bug reports

CHDMounter routes all logging through Serilog. A dedicated sink forwards every event at
**Warning level or above** to the bug report service:

```text
https://www.purelogiccode.com/bugreport/api/send-bug-report
```

A report contains:

- **Environment details** — date, application name and version, OS version, architecture,
  bitness, Windows/Linux/macOS version, processor count, base directory and temp path.
- **Error details** — the log message.
- **Exception details** — exception type, message, source and stack trace (when an exception is
  attached).

Reports are queued and sent one at a time with a short delay between them.

### What is never reported

- **Known user/environment errors** — missing Dokan/WinFsp/FUSE drivers, a wrong console type, a
  busy mount point, an unparsable image and similar expected conditions are logged at Information
  level and do **not** create bug reports.
- **Test runs** — test host processes (`testhost`, `vstest`, ReSharper, xUnit console, NUnit,
  MSTest) can never file reports.

### Caveat

Stack traces and environment details can contain local file paths (for example the application
base directory). Do not file reports from machines where such paths are sensitive.

## Disabling network access

There is no in-application switch for these services. To prevent CHDMounter from reaching them,
block the following hosts with your firewall, DNS filter or `hosts` file:

- `www.purelogiccode.com`
- `api.github.com` (used by the [update checker](usage.md#update-checker))

The application remains fully functional without network access.

## See also

- [Usage](usage.md) — update checker and logs
- [Troubleshooting](troubleshooting.md) — where to find the logs
