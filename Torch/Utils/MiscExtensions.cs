using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Torch.Utils
{
    public static class MiscExtensions
    {
        private static readonly ThreadLocal<WeakReference<byte[]>> _streamBuffer = new ThreadLocal<WeakReference<byte[]>>(() => new WeakReference<byte[]>(null));

        public static byte[] ReadToEnd(this Stream stream)
        {
            byte[] buffer;
            if (!_streamBuffer.Value.TryGetTarget(out buffer))
                buffer = new byte[stream.Length];
            if (buffer.Length < stream.Length)
                buffer = new byte[stream.Length];
            if (buffer.Length < 1024)
                buffer = new byte[1024];
            while (true)
            {
                if (buffer.Length == stream.Position)
                    Array.Resize(ref buffer, Math.Max((int)stream.Length, buffer.Length * 2));
                int count = stream.Read(buffer, (int)stream.Position, buffer.Length - (int)stream.Position);
                if (count == 0)
                    break;
            }
            var result = new byte[(int)stream.Position];
            Array.Copy(buffer, 0, result, 0, result.Length);
            _streamBuffer.Value.SetTarget(buffer);
            return result;
        }
    }
}
