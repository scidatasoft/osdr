using System.Collections.Generic;
using System.IO;

namespace Sds.Download
{
	public interface IDownload
	{
		Stream DownloadFile(string id);
		Stream DownloadArchive(IEnumerable<string> folderIds = null, IEnumerable<string> fileIds = null);
	}
}
