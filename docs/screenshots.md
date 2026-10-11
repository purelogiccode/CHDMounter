# Screenshots (F8)

Press **F8** while a CHDMounter window is focused to capture the active window and save it as a
PNG file. The same shortcut is available in the `CHDMounter_Tester` developer tool.

## Where screenshots are saved

CHDMounter always tries the application folder first and falls back to the application data
folder when that fails (for example when the application folder is read-only):

| Order | Platform | Location |
|-------|----------|----------|
| 1 (primary) | All | `Screenshot` folder next to the executable |
| 2 (fallback) | Windows | `%LOCALAPPDATA%\CHDMounter\Screenshot` |
| 2 (fallback) | Linux | `~/.local/share/CHDMounter/Screenshot` |
| 2 (fallback) | macOS | `~/Library/Application Support/CHDMounter/Screenshot` |

- The `Screenshot` folder is created automatically if it does not exist.
- File names use the pattern `screenshot_YYYY-MM-DD_HH-mm-ss-fff.png` (local time).
- If both locations fail (for example a full disk), the failure is written to the log and nothing
  is captured.
- The path of every saved screenshot is recorded in the log pane and the log file.

## Notes

- Screenshots are stored **locally only** — they are never uploaded. See [Privacy](privacy.md).
- The capture includes the window content at the current window size and display scaling.

## See also

- [Usage](usage.md) — the GUI reference
- [Privacy](privacy.md) — what is and is not sent off your machine
