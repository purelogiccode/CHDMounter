# How It Works

## CHD reading

CHD (Compressed Hunks of Data) is MAME's lossless compression format for CD/DVD/HDD images.
CHDMounter uses [CHDSharp](https://github.com/drpetersonfernandes/CHDSharp) to read all five CHD
versions (V1–V5) and all compression codecs:

- **General**: zlib (deflate), LZMA, FLAC (headerless, 16-bit stereo), dynamic Huffman, Zstd
- **CD-sector**: CDZL (zlib), CDLZ (LZMA), CDFL (FLAC), CDZS (Zstd) — with ECC regeneration
- **AV**: AVHuff — audio+video Huffman for laserdisc/arcade captures

## Sector reading

The `SectorReader` maps logical block addresses (LBAs) to byte offsets within CHD hunks. It:

1. Parses track metadata (`CHT2`/`CHTR`/`CHGD` tags) for multi-track CD images
2. Maps LBA to CHD frame number using track offsets (handles the GD-ROM 45000-LBA shift)
3. Reads and caches the compressed hunk via `ReadHunk()`
4. Detects the sector mode (Mode 1 = 16-byte header, Mode 2 = 24-byte header) by scanning for
   the CD sync pattern `00 FF×10 00`
5. Strips headers to deliver 2048 bytes of user data per sector

## File system parsing

The [VideoGameFileSystemParser](https://github.com/drpetersonfernandes/VideoGameFileSystemParser)
library (vendored into this solution) parses console file systems from raw sectors and reconstructs
directory trees. Supported file systems include ISO 9660 (with Joliet/Rock Ridge), XDVDFS (Xbox),
OperaFS (3DO), CD-i Green Book, UDF and HFS/HFS+.

## VFS operations

The `ChdContainer` bridges the parsed `FsNode` tree to the driver-specific VFS layer:

- **Dokan**: `ChdFs` implements `IDokanOperations` — `CreateFile` resolves paths, `ReadFile` maps
  file offsets to sector reads, `FindFiles` lists directories, `GetVolumeInformation` reports a
  read-only "CHDFS" volume.
- **WinFsp**: `ChdFs` extends `FileSystemBase` — `Open` resolves paths and returns a `FileEntry`
  as both file node and descriptor, `Read` copies sector data into native memory via
  `Marshal.Copy`, `ReadDirectoryEntry` enumerates children including `.` and `..`.
- **FUSE**: `ChdFuseVolume` implements the `IFuseVolume` contract of the bundled `FuseSharp`
  layer — `GetEntry`/`GetFolderList` map POSIX paths to container entries and `ReadFile` copies
  sector data into the FUSE read buffer. The blocking FUSE session runs on a background thread
  and is stopped through `FuseFileSystem.Exit()`.

The core in use is selected in **Settings → Mount core**: Dokan (default) or WinFsp on Windows;
FUSE on Linux and macOS.

## Virtual CUE/BIN/ISO/WAV export

For CUE/BIN/ISO/WAV modes, the container generates virtual entries dynamically:

- The `.cue` file contains standard `FILE`, `TRACK` and `INDEX` descriptors built from CHD track
  metadata.
- The `.bin`/`.iso` file maps reads to raw sector data (2352 or 2048 bytes per sector).
- `.wav` files are generated per audio track with standard WAV headers.

For SingleFile mode, the container serves the entire decompressed CHD image as a single
`image.iso` file.

## Mount safety

- Images are opened read-only; CHDMounter never writes to a CHD file.
- When running as Administrator with WinFsp, CHDMounter uses cross-integrity folder mounts with a
  permissive DACL so standard (non-elevated) processes can access the mounted drive.
