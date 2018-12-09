using System.IO;

namespace Sds.FileStorage
{
    public static class DiskStorageExtensions
	{
		private const int DefaultBufferSize = 8 * 1024;

		/// <summary>
		/// Copies the contents of input to output. Doesn't close either stream.
		/// </summary>
		public static void CopyStream(this Stream input, Stream output, int bufferSize = DefaultBufferSize)
		{
			byte[] buffer = new byte[bufferSize];
			int length;
			while ((length = input.Read(buffer, 0, buffer.Length)) > 0)
			{
				output.Write(buffer, 0, length);
			}
		}
	}
}
