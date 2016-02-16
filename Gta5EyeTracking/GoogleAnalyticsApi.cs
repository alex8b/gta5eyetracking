using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Gta5EyeTracking
{
	public class GoogleAnalyticsApi
	{
		private readonly string _trackingId;
		private readonly string _userGuid;
		private readonly string _applicationName;
		private readonly string _applicationId;
		private readonly string _applicationVersion;

		public GoogleAnalyticsApi(string trackingId, string userGuid, string applicationName, string applicationId, string applicationVersion)
		{
			_trackingId = trackingId;
			_userGuid = userGuid;
			_applicationName = applicationName;
			_applicationId = applicationId;
			_applicationVersion = applicationVersion;
		}

		public void TrackEvent(string category, string action, string label, int? value = null)
		{
			Track(HitType.@event, category, action, label, value);
		}

		public void TrackPageview(string category, string action, string label, int? value = null)
		{
			Track(HitType.@pageview, category, action, label, value);
		}

		private void Track(HitType type, string category, string action, string label,
			int? value = null)
		{
			Task.Run(() => 
			{ 
				try
				{
					if (string.IsNullOrEmpty(category)) return;
					if (string.IsNullOrEmpty(action)) return;

					var request = (HttpWebRequest) WebRequest.Create("http://www.google-analytics.com/collect");
					request.Method = "POST";
					request.KeepAlive = false;

					// the request body we want to send
					var postData = new Dictionary<string, string>
					{
						{"v", "1"},
						{"tid", _trackingId},
						{"cid", _userGuid},
						{"uid", _userGuid},
						{"t", type.ToString()},
						{"ec", category},
						{"ea", action},
						{"an", _applicationName},
						{"aid", _applicationId},
						{"av", _applicationVersion},
					};
					if (!string.IsNullOrEmpty(label))
					{
						postData.Add("el", label);
					}
					if (value.HasValue)
					{
						postData.Add("ev", value.ToString());
					}

					var postDataString = postData
						.Aggregate("", (data, next) => string.Format("{0}&{1}={2}", data, next.Key,
							HttpUtility.UrlEncode(next.Value)))
						.TrimEnd('&');

					// set the Content-Length header to the correct value
					request.ContentLength = Encoding.UTF8.GetByteCount(postDataString);

					// write the request body to the request
					using (var writer = new StreamWriter(request.GetRequestStream()))
					{
						writer.Write(postDataString);
					}


					using (var webResponse = (HttpWebResponse) request.GetResponse())
					{
						if (webResponse.StatusCode != HttpStatusCode.OK)
						{
							Util.Log("Google Analytics tracking did not return OK 200");
						}
						webResponse.Close();
					}

				}
				catch (Exception e)
				{
					Util.Log(e.Message);
				}
			});
		}

		private enum HitType
		{
			// ReSharper disable InconsistentNaming
			@event,
			@pageview,
			// ReSharper restore InconsistentNaming
		}
	}
}
