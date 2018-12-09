using System;

namespace Sds.Core
{
	/// <summary>
	/// Extensions methods for GUIDs
	/// Credits to some of the methods: http://madskristensen.net/post/A-shorter-and-URL-friendly-GUID
	/// </summary>
	public static class GuidExtensions
	{
		/// <summary>
		/// Returns short, encoded GUID representation.
		/// It takes a standard GUID like this:
		/// c9a646d3-9c61-4cb7-bfcd-ee2522c8f633
		/// And converts it into this smaller string:
		/// 00amyWGct0y_ze4lIsj2Mw
		/// </summary>
		/// <param name="guid"></param>
		/// <returns>short, encoded GUID representation</returns>
		public static string EncodeGuid(this string guid)
		{
			return Encode(new Guid(guid));
		}

		/// <summary>
		/// Returns short, encoded GUID representation.
		/// It takes a standard GUID like this:
		/// c9a646d3-9c61-4cb7-bfcd-ee2522c8f633
		/// And converts it into this smaller string:
		/// 00amyWGct0y_ze4lIsj2Mw
		/// </summary>
		/// <param name="guid"></param>
		/// <returns>short, encoded GUID representation</returns>
		public static string Encode(this Guid guid)
		{
			return Convert.ToBase64String(guid.ToByteArray())
				.Replace("/", "_")
				.Replace("+", "-")
				.Substring(0, 22);
		}

		/// <summary>
		/// Returns "standard" GUID string from a short, encoded GUID representation.
		/// It takes a standard GUID like this:
		/// c9a646d3-9c61-4cb7-bfcd-ee2522c8f633
		/// And converts it into this smaller string:
		/// 00amyWGct0y_ze4lIsj2Mw
		/// </summary>
		/// <param name="encodedGuid"></param>
		/// <returns>"standard" GUID string from a short, encoded GUID representation.</returns>
		public static Guid DecodeGuid(this string encodedGuid)
		{
			byte[] buffer = Convert.FromBase64String(
				encodedGuid
				.Replace("_", "/")
				.Replace("-", "+") + "==");

			return new Guid(buffer);
		}
	}
}
