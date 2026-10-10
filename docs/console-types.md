# Console Types

Console selection is **alias-only** (case-insensitive). The alias registry lives in the
[VideoGameFileSystemParser](https://github.com/drpetersonfernandes/VideoGameFileSystemParser)
library and is the single source of truth.

| Console / Format     | CLI Alias       | Filesystem              | Notes                                              |
|----------------------|-----------------|-------------------------|----------------------------------------------------|
| 3DO                  | `3do`           | OperaFS                 | Block-based directory chain, ISO 9660 fallback     |
| Amiga CD             | `amigacd`       | ISO 9660                |                                                    |
| Amiga CD32           | `amigacd32`     | ISO 9660                |                                                    |
| Amiga CDTV           | `amigacdtv`     | ISO 9660                |                                                    |
| CD-i                 | `cdi`           | Green Book              | Interleaved stream support, Path Table             |
| CUE/BIN RAW 2048     | `cuebin2048`    | Virtual                 | CUE + BIN at 2048 bytes/sector                     |
| CUE/BIN RAW 2352     | `cuebin2352`    | Virtual                 | CUE + BIN at 2352 bytes/sector                     |
| CUE/BIN/WAV RAW 2048 | `cuebinwav2048` | Virtual                 | CUE + BIN + WAV audio tracks (2048-byte data)      |
| CUE/BIN/WAV RAW 2352 | `cuebinwav2352` | Virtual                 | CUE + BIN + WAV audio tracks (2352-byte data)      |
| CUE/ISO RAW 2048     | `cueiso2048`    | Virtual                 | CUE sheet + ISO at 2048 bytes/sector               |
| CUE/ISO RAW 2352     | `cueiso2352`    | Virtual                 | CUE sheet + ISO at 2352 bytes/sector               |
| CUE/ISO/WAV RAW 2048 | `cueisowav2048` | Virtual                 | CUE + ISO + WAV audio tracks (2048-byte data)      |
| CUE/ISO/WAV RAW 2352 | `cueisowav2352` | Virtual                 | CUE + ISO + WAV audio tracks (2352-byte data)      |
| FM Towns             | `fmtowns`       | ISO 9660                |                                                    |
| ISO 9660             | `iso9660`       | ISO 9660 / High Sierra  | Joliet/UTF-16BE, SUSP/Rock Ridge                   |
| ISO RAW 2048         | `isoraw2048`    | Raw sectors             | Entire CHD as single `image.iso` (2048-byte units) |
| ISO RAW 2352         | `isoraw2352`    | Raw sectors             | Entire CHD as single `image.iso` (2352-byte units) |
| Neo Geo CD           | `neogeocd`      | ISO 9660                |                                                    |
| Nuon                 | `nuon`          | UDF → ISO 9660 fallback | VM Labs Nuon DVD                                   |
| PC Engine CD         | `pcengine`      | ISO 9660                | Boot signature scan, fallback raw track files      |
| PC-98                | `pc98`          | ISO 9660                |                                                    |
| PC-FX                | `pcfx`          | PC-FX ISO               | Dedicated byte-offset VD scanner                   |
| Pico                 | `pico`          | ISO 9660                | Sega Pico                                          |
| Pippin               | `pippin`        | HFS → HFS+ → UDF → ISO  | Apple Bandai Pippin                                |
| PlayStation (Auto)   | `psauto`        | ISO 9660                | Auto-detect PS1/PS2/PS3/PSP                        |
| PS1                  | `ps1`           | ISO 9660                | Track-level parsing, aggressive PVD scan           |
| PS2                  | `ps2`           | ISO 9660                | CD/DVD auto-detection                              |
| PS3                  | `ps3`           | UDF → ISO 9660 fallback | Multi-extent large file support                    |
| PS3 ISO RAW 2352     | `isoraw2352`    | Virtual ISO passthrough | Single `image.iso` for RPCS3                       |
| PSP                  | `psp`           | ISO 9660                | UMD image support                                  |
| Sega Dreamcast       | `segadreamcast` | ISO 9660                | GD-ROM offset search (-45000, -150, 0, etc.)       |
| Sega Genesis         | `segagenesis`   | ISO 9660                |                                                    |
| Sega Saturn          | `segasaturn`    | ISO 9660                |                                                    |
| X68000               | `x68000`        | ISO 9660 → UDF fallback |                                                    |
| Xbox                 | `xbox`          | XDVDFS                  | Binary tree directory structure                    |
| Xbox 360             | `xbox360`       | XDVDFS                  | XGD2/XGD3 offset detection                         |
| Xbox ISO RAW 2352    | `isoraw2352`    | Virtual ISO passthrough | Single `image.iso` for xemu                        |

> `isoraw2048` and `isoraw2352` are generic passthrough modes; they are also the recommended
> choice for emulators that expect a plain ISO (xemu, RPCS3).
