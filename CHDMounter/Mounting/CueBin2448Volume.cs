using System.Globalization;
using CHDSharp;
using CHDSharp.Models;
using VideoGameFileSystemParser.Parsers;

namespace CHDMounter.Mounting;

/// <summary>
///     Exposes a CD image as a virtual CUE/BIN pair whose BIN file contains full
///     2448-byte CD frames: 2352 bytes of sector data followed by the 96 bytes of
///     subchannel data stored in the CHD. This preserves the CD+G graphics data that
///     the standard 2352-byte export drops.
/// </summary>
internal sealed class CueBin2448Volume : IDiscVolume
{
    private const int SectorBytes = 2352;
    private const int SubchannelBytes = 96;
    private const int FrameBytes = SectorBytes + SubchannelBytes;

    private readonly ulong _binSize;
    private readonly byte[] _cueBytes;
    private readonly List<FileEntry> _entries;
    private readonly byte[] _frameBuffer = new byte[FrameBytes];
    private readonly Lock _readLock = new();
    private readonly List<TrackSegment> _segments;
    private readonly Dictionary<TrackInfo, uint> _trackStartFrames = [];
    private readonly ChdFile _chd;
    private readonly SectorReader _reader;

    private bool _disposed;

    private CueBin2448Volume(string chdPath, ChdFile chd, SectorReader reader)
    {
        _chd = chd;
        _reader = reader;
        VolumeName = Path.GetFileNameWithoutExtension(chdPath);
        var stem = VolumeName;

        _segments = [];
        ulong offset = 0;
        foreach (var track in reader.Tracks)
        {
            _trackStartFrames[track] = (uint)(offset / FrameBytes);
            var size = (ulong)track.Frames * FrameBytes;
            _segments.Add(new TrackSegment(track, offset, size));
            offset += size;
        }

        _binSize = offset;
        _cueBytes = Encoding.ASCII.GetBytes(BuildCueText(stem));

        var root = new FileEntry
        {
            Name = "\\",
            FullPath = "\\",
            IsDirectory = true
        };
        _entries =
        [
            root,
            new FileEntry
            {
                Name = stem + ".cue",
                FullPath = "\\" + stem + ".cue",
                Lba = 0,
                Size = (ulong)_cueBytes.Length,
                IsDirectory = false
            },
            new FileEntry
            {
                Name = stem + ".bin",
                FullPath = "\\" + stem + ".bin",
                Lba = 0,
                Size = _binSize,
                IsDirectory = false
            }
        ];
    }

    /// <inheritdoc />
    public string VolumeName { get; }

    /// <inheritdoc />
    public ulong VolumeSize => _binSize;

    /// <inheritdoc />
    public IReadOnlyList<FileEntry> Entries => _entries;

    /// <inheritdoc />
    public FileEntry? FindFile(string path)
    {
        var key = MakeEntryKey(path);
        return _entries.FirstOrDefault(entry =>
            string.Equals(entry.FullPath, key, StringComparison.OrdinalIgnoreCase));
    }

    /// <inheritdoc />
    public IEnumerable<FileEntry> ListDirectory(string path)
    {
        return string.Equals(MakeEntryKey(path), "\\", StringComparison.OrdinalIgnoreCase)
            ? _entries.Where(static entry => !entry.IsDirectory)
            : [];
    }

    /// <inheritdoc />
    public int ReadFile(FileEntry entry, ulong offset, byte[] buffer, int bufOffset, int count)
    {
        if (_disposed || entry.IsDirectory || offset >= entry.Size || count <= 0)
            return 0;

        var remaining = entry.Size - offset;
        var bytesToRead = (int)Math.Min((ulong)count, remaining);

        if (entry.Name.EndsWith(".cue", StringComparison.OrdinalIgnoreCase))
            return ReadCue(offset, buffer, bufOffset, bytesToRead);

        if (entry.Name.EndsWith(".bin", StringComparison.OrdinalIgnoreCase))
            return ReadBin(offset, buffer, bufOffset, bytesToRead);

        return 0;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;
        _reader.Dispose();
        _chd.Dispose();
    }

    /// <summary>
    ///     Opens a CHD and prepares its 2448-byte CUE/BIN virtual export.
    /// </summary>
    /// <param name="chdPath">The CHD file to open.</param>
    /// <param name="volume">The created volume on success.</param>
    /// <param name="error">A description of the failure on failure.</param>
    /// <returns><c>true</c> if the image can be exported as 2448-byte frames.</returns>
    internal static bool TryCreate(string chdPath, out CueBin2448Volume? volume, out string? error)
    {
        volume = null;
        error = null;

        var openError = ChdFile.Open(chdPath, out var chd, CancellationToken.None);
        if (openError != ChdError.Chderrnone || chd is null)
        {
            error = $"CHDSharp failed to open the file: {openError} ({ChdErrorExtensions.GetMessage(openError)})";
            return false;
        }

        if (chd.UnitBytes < SectorBytes)
        {
            error = $"The CHD stores {chd.UnitBytes}-byte units, but 2448-byte CUE/BIN export requires raw CD sectors.";
            chd.Dispose();
            return false;
        }

        SectorReader? reader = null;
        try
        {
            reader = new SectorReader(chd, chd.UnitBytes);
            if (reader.Tracks.Count == 0)
            {
                reader.Dispose();
                chd.Dispose();
                error = "The CHD has no CD track metadata.";
                return false;
            }

            volume = new CueBin2448Volume(chdPath, chd, reader);
            return true;
        }
        catch (Exception ex)
        {
            reader?.Dispose();
            chd.Dispose();
            error = "CHDSharp opened the file but track/metadata parsing failed: " + ex.Message;
            return false;
        }
    }

    private int ReadCue(ulong offset, byte[] buffer, int bufOffset, int count)
    {
        if (offset >= (ulong)_cueBytes.Length) return 0;

        var available = _cueBytes.Length - (int)offset;
        var length = Math.Min(count, available);
        Array.Copy(_cueBytes, (int)offset, buffer, bufOffset, length);
        return length;
    }

    private int ReadBin(ulong offset, byte[] buffer, int bufOffset, int count)
    {
        var written = 0;

        lock (_readLock)
        {
            while (written < count)
            {
                var position = offset + (ulong)written;
                var segment = FindSegment(position);
                if (segment is null) break;

                var frameOffset = position - segment.Value.Start;
                var frameIndex = frameOffset / FrameBytes;
                var offsetInFrame = (int)(frameOffset % FrameBytes);
                var lba = segment.Value.Track.StartLba + (uint)frameIndex;

                _reader.SetTrack(segment.Value.Track, locked: true);
                if (!_reader.ReadRawSector(lba, out var rawSector)) break;

                FixAudioSubchannel(segment.Value.Track, rawSector);

                Array.Clear(_frameBuffer);
                var sourceLength = Math.Min(rawSector.Length, FrameBytes);
                Array.Copy(rawSector, _frameBuffer, sourceLength);

                var length = Math.Min(FrameBytes - offsetInFrame, count - written);
                Array.Copy(_frameBuffer, offsetInFrame, buffer, bufOffset + written, length);
                written += length;
            }
        }

        return written;
    }

    private TrackSegment? FindSegment(ulong position)
    {
        foreach (var segment in _segments)
            if (position >= segment.Start && position < segment.Start + segment.Size)
                return segment;

        return null;
    }

    /// <summary>
    ///     The parser's raw-sector reader byte-swaps the whole hunk for audio tracks so
    ///     CDDA samples come out little-endian. The CHD stores subchannel data unswapped,
    ///     so undo the swap on the 96-byte subchannel tail to restore it.
    /// </summary>
    /// <param name="track">The track the frame belongs to.</param>
    /// <param name="rawSector">The frame read by the parser.</param>
    private static void FixAudioSubchannel(TrackInfo track, byte[] rawSector)
    {
        if (rawSector.Length < FrameBytes ||
            !track.TrackType.Contains("AUDIO", StringComparison.OrdinalIgnoreCase))
            return;

        for (var i = SectorBytes; i + 1 < FrameBytes; i += 2)
            (rawSector[i], rawSector[i + 1]) = (rawSector[i + 1], rawSector[i]);
    }

    private string BuildCueText(string stem)
    {
        var builder = new StringBuilder();
        builder.AppendLine(CultureInfo.InvariantCulture, $"FILE \"{stem}.bin\" BINARY");

        foreach (var track in _reader.Tracks)
        {
            var trackType = GetCueTrackType(track);
            var index = track.Index.ToString("D2", CultureInfo.InvariantCulture);

            // Each 2448-byte frame in the BIN is one sector, so index positions are the
            // track's cumulative frame offsets.
            var startFrame = GetTrackStartFrame(track);
            if (track.Pregap != 0)
            {
                builder.AppendLine(CultureInfo.InvariantCulture, $"  TRACK {index} {trackType}");
                builder.AppendLine(CultureInfo.InvariantCulture, $"    INDEX 00 {SectorToMsf(startFrame)}");
                builder.AppendLine(CultureInfo.InvariantCulture,
                    $"    INDEX 01 {SectorToMsf(startFrame + track.Pregap)}");
            }
            else
            {
                builder.AppendLine(CultureInfo.InvariantCulture, $"  TRACK {index} {trackType}");
                builder.AppendLine(CultureInfo.InvariantCulture, $"    INDEX 01 {SectorToMsf(startFrame)}");
            }
        }

        return builder.ToString();
    }

    private static string GetCueTrackType(TrackInfo track)
    {
        if (track.TrackType.Contains("AUDIO", StringComparison.OrdinalIgnoreCase))
            return "CDG";

        if (track.TrackType.Contains("MODE2", StringComparison.OrdinalIgnoreCase) ||
            track.TrackType.Contains("CDI", StringComparison.OrdinalIgnoreCase))
            return "MODE2/2448";

        return "MODE1/2448";
    }

    private uint GetTrackStartFrame(TrackInfo track)
    {
        return _trackStartFrames[track];
    }

    private static string SectorToMsf(uint sectors)
    {
        var minutes = sectors / 4500;
        var seconds = sectors / 75 % 60;
        var frames = sectors % 75;
        return $"{minutes:D2}:{seconds:D2}:{frames:D2}";
    }

    private static string MakeEntryKey(string path)
    {
        if (string.IsNullOrEmpty(path) || path is "\\" or "/")
            return "\\";

        var key = path.Replace('/', '\\');
        if (key[0] != '\\') key = "\\" + key;

        return key.Length > 1 ? key.TrimEnd('\\') : key;
    }

    private readonly record struct TrackSegment(TrackInfo Track, ulong Start, ulong Size);
}
