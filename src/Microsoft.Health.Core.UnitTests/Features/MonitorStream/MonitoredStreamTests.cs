// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Core.Features.MonitorStream;
using Xunit;

namespace Microsoft.Health.Core.UnitTests.Features.MonitorStream;

public class MonitoredStreamTests
{
    [Fact]
    public void BillingResponseLogMiddleware_MonitoredStream_PropertiesCheck()
    {
        var memoryStream = new MemoryStream();
        using (var monitoredStream = new MonitoredStream(memoryStream))
        {
            monitoredStream.Write(new byte[123]);
            Assert.Equal(memoryStream.CanRead, monitoredStream.CanRead);
            Assert.Equal(memoryStream.CanSeek, monitoredStream.CanSeek);
            Assert.Equal(memoryStream.CanWrite, monitoredStream.CanWrite);
            Assert.Equal(memoryStream.CanTimeout, monitoredStream.CanTimeout);
            Assert.Equal(memoryStream.Length, monitoredStream.Length);
            Assert.Equal(memoryStream.Position, monitoredStream.Position);
        }
    }

    [Fact]
    public void BillingResponseLogMiddleware_MonitoredStream_ReadWrite()
    {
        var memoryStream = new MemoryStream();
        using (var monitoredStream = new MonitoredStream(memoryStream))
        {
            byte[] bytes = Encoding.UTF8.GetBytes("This is a string");
            monitoredStream.Write(bytes);
            Assert.Equal(bytes.Length, monitoredStream.WriteCount);
            Assert.Equal(bytes.Length, memoryStream.Length);

            monitoredStream.Flush();
            monitoredStream.Seek(0, 0);
            int bytesRead = monitoredStream.Read(new byte[bytes.Length], 0, bytes.Length);
            Assert.Equal(bytes.Length, bytesRead);
        }
    }

    [Fact]
    [SuppressMessage("Performance", "CA1835:Prefer the 'Memory'-based overloads for 'ReadAsync' and 'WriteAsync'", Justification = "Test different overloads")]
    public async Task BillingResponseLogMiddleware_MonitoredStream_WriteAsync()
    {
        byte[] bytes = Encoding.UTF8.GetBytes("This is a string");
        using (var monitoredStream = new MonitoredStream(new MemoryStream()))
        {
            await monitoredStream.WriteAsync(bytes);
            Assert.Equal(bytes.Length, monitoredStream.WriteCount);
        }

        using (var monitoredStream = new MonitoredStream(new MemoryStream()))
        {
            await monitoredStream.WriteAsync(bytes, 0, bytes.Length);
            Assert.Equal(bytes.Length, monitoredStream.WriteCount);
        }

        using (var monitoredStream = new MonitoredStream(new MemoryStream()))
        {
            await monitoredStream.WriteAsync(bytes.AsMemory(0, bytes.Length), CancellationToken.None);
            Assert.Equal(bytes.Length, monitoredStream.WriteCount);
        }

        using (var monitoredStream = new MonitoredStream(new MemoryStream()))
        {
            IAsyncResult asyncResult = monitoredStream.BeginWrite(bytes, 0, bytes.Length, null, new object());
            Assert.Equal(bytes.Length, monitoredStream.WriteCount);
        }
    }

    [Fact]
    public void BillingResponseLogMiddleware_MonitoredStream_WriteByte()
    {
        using (var monitoredStream = new MonitoredStream(new MemoryStream()))
        {
            monitoredStream.WriteByte(0);
            Assert.Equal(1, monitoredStream.WriteCount);
            Assert.Equal(1, monitoredStream.Length);
        }
    }

    [Fact]
    public void BillingResponseLogMiddleware_MonitoredStream_WriteVaryingCounts()
    {
        byte[] bytes = Encoding.UTF8.GetBytes("This is a string");

        using (var monitoredStream = new MonitoredStream(new MemoryStream()))
        {
            // Specified count is the same as the byte array length
            monitoredStream.Write(bytes, 0, bytes.Length);
            Assert.Equal(bytes.Length, monitoredStream.WriteCount);
            Assert.Equal(bytes.Length, monitoredStream.Length);
        }

        using (var monitoredStream = new MonitoredStream(new MemoryStream()))
        {
            // Specified count is less than the byte array length
            int count = bytes.Length / 2;
            monitoredStream.Write(bytes, 0, count);
            Assert.Equal(count, monitoredStream.WriteCount);
            Assert.Equal(count, monitoredStream.Length);
        }
    }

    [Fact]
    public void BillingResponseLogMiddleware_MonitoredStream_NullBaseStreamThrows()
    {
        Assert.Throws<ArgumentNullException>(() => new MonitoredStream(null));
    }

    [Fact]
    public void BillingResponseLogMiddleware_MonitoredStream_IsDisposed()
    {
        var memoryStream = new MemoryStream();
        var monitoredStream = new MonitoredStream(memoryStream);
        byte[] bytes = Encoding.UTF8.GetBytes("This is a string");
        monitoredStream.Write(bytes);

        monitoredStream.Dispose();
        Assert.Throws<ObjectDisposedException>(() => { monitoredStream.WriteByte(1); });
        Assert.Throws<ObjectDisposedException>(() => { memoryStream.WriteByte(1); });
    }
}
