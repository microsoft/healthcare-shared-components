// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;

namespace Microsoft.Health.Core.Features.MonitorStream;

/// <summary>
/// A stream to wrap an underlying stream and counts the number of bytes passed while operating with it.
/// </summary>
public sealed class MonitoredStream : Stream
{
    private readonly Stream _stream;

    /// <summary>
    /// Initializes a new instance of the <see cref="MonitoredStream"/> class.
    /// </summary>
    /// <param name="stream">An underlying stream.</param>
    public MonitoredStream(Stream stream)
    {
        _stream = EnsureArg.IsNotNull(stream, nameof(stream));
    }

    /// <inheritdoc />
    public override bool CanRead => _stream.CanRead;

    /// <inheritdoc />
    public override bool CanSeek => _stream.CanSeek;

    /// <inheritdoc />
    public override bool CanWrite => _stream.CanWrite;

    /// <inheritdoc />
    public override long Length => _stream.Length;

    /// <inheritdoc />
    public override long Position
    {
        get => _stream.Position;
        set => _stream.Position = value;
    }

    /// <summary>
    /// The number of bytes that have been written.
    /// </summary>
    public long WriteCount { get; private set; }

    /// <inheritdoc />
    public override void Flush()
    {
        _stream.Flush();
    }

    /// <inheritdoc />
    public override Task FlushAsync(CancellationToken cancellationToken)
    {
        return _stream.FlushAsync(cancellationToken);
    }

    /// <inheritdoc />
    public override int Read(byte[] buffer, int offset, int count)
    {
        return _stream.Read(buffer, offset, count);
    }

    /// <inheritdoc />
    public override long Seek(long offset, SeekOrigin origin)
    {
        return _stream.Seek(offset, origin);
    }

    /// <inheritdoc />
    public override void SetLength(long value)
    {
        _stream.SetLength(value);
    }

    /// <inheritdoc />
    public override void WriteByte(byte value)
    {
        _stream.WriteByte(value);
        WriteCount++;
    }

    /// <inheritdoc />
    public override void Write(byte[] buffer, int offset, int count)
    {
        _stream.Write(buffer, offset, count);
        WriteCount += count;
    }

    /// <inheritdoc />
    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        WriteCount += count;
        return _stream.WriteAsync(buffer, offset, count, cancellationToken);
    }

    /// <inheritdoc />
    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        WriteCount += buffer.Length;
        return _stream.WriteAsync(buffer, cancellationToken);
    }

    /// <inheritdoc />
    public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
    {
        WriteCount += count;
        return _stream.BeginWrite(buffer, offset, count, callback, state);
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        _stream.Dispose();
        base.Dispose(disposing);
    }
}
