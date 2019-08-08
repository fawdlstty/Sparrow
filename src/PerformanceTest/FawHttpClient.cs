////////////////////////////////////////////////////////////////////////////////
//
// Class Name:  FawHttpClient
// Description: C# HTTP客户端类
// Class URI:   https://github.com/fawdlstty/some_tools
// Author:      Fawdlstty
// Author URI:  https://www.fawdlstty.com/
// Version:     0.1
// License:     MIT
// Last Update: Aug 10, 2018
// remarks:     使用此类需要先添加引用System.Web
//
////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace PerformanceTest {
	public enum hanUserAgent { Android, Chrome, Edge, Ie }
	public enum hanContentType { UrlEncode, Json, Xml, FormData }

	public class FawHttpClient: IDisposable {
		public FawHttpClient (hanUserAgent ua = hanUserAgent.Chrome) { m_ua = ua; }

		// 销毁对象
		public void Dispose () { GC.SuppressFinalize (this); }

		// 添加cookie
		public void add_cookie (string name, string value, string path = "/") { m_cookies.Add (new Cookie (name, value, path)); }

		// 获取cookie
		public (string, string) get_cookie (string name) { return (m_cookies[name]?.Name ?? "", m_cookies[name]?.Value ?? ""); }

		// 删除cookie
		public void del_cookie (string name) {
			var cookies = new CookieCollection ();
			foreach (Cookie cookie in m_cookies) {
				if (cookie.Name != name)
					cookies.Add (cookie);
			}
			m_cookies = cookies;
		}

		// post 同步请求
		public byte[] post (string url, hanContentType ct, params (string, string)[] param) {
			var post_data = _get_post_data (ct, param);
			return _request_impl (url, "POST", post_data.Item1, post_data.Item2);
		}

		// post 同步请求
		public byte[] post (string url, params (string, string)[] param) { return post (url, hanContentType.UrlEncode, param); }

		// get 同步请求
		public byte[] get (string url) { return _request_impl (url, "GET"); }

		// post 异步请求
		public async Task<byte[]> post_async (string url, hanContentType ct = hanContentType.UrlEncode, params (string, string)[] param) {
			try {
				var post_data = _get_post_data (ct, param);
				return await _request_impl_async (url, "POST", post_data.Item1, post_data.Item2);
			} catch (Exception) {
				return null;
			}
		}

		// post 异步请求
		public async Task<byte[]> post_async (string url, params (string, string)[] param) {
			return await post_async (url, hanContentType.UrlEncode, param);
		}

		// get 异步请求
		public Task<byte[]> get_async (string url) {
			try {
				return _request_impl_async (url, "GET");
			} catch (Exception) {
				return null;
			}
		}

		// 同步请求
		private byte[] _request_impl (string url, string method, string param_data = "", string content_type = "") {
			HttpWebRequest req = _make_request (url, method, param_data, content_type);
			using (HttpWebResponse res = (HttpWebResponse) req.GetResponse ())
				return _get_response_data (res);
		}

		// 异步请求
		private async Task<byte[]> _request_impl_async (string url, string method, string param_data = "", string content_type = "") {
			HttpWebRequest req = _make_request (url, method, param_data, content_type);
			using (HttpWebResponse res = (HttpWebResponse) await req.GetResponseAsync ())
				return _get_response_data (res);
		}

		// 清除 utf8 bom
		private static byte[] _clear_bom (byte[] data) {
			if (data.Length > 3 && data[0] == '\xef' && data[1] == '\xbb' && data[2] == '\xbf') {
				using (var ms = new MemoryStream ()) {
					ms.Write (data, 3, data.Length - 3);
					data = ms.ToArray ();
				}
			}
			return data;
		}

		// 获取 post data
		private (string, string) _get_post_data (hanContentType ct = hanContentType.UrlEncode, params (string, string)[] param) {
			string boundary = $"----hanHttpClient_{System.Guid.NewGuid ().ToString ("N").Substring (0, 8)}", crlf = "\r\n";
			string content_type = new Dictionary<hanContentType, string> {
				[hanContentType.UrlEncode] = "application/x-www-form-urlencoded",
				[hanContentType.Json] = "application/json",
				[hanContentType.Xml] = "text/xml",
				[hanContentType.FormData] = $"multipart/form-data; boundary={boundary}"
			}[ct];
			Func<string, string> _my_encode = (s) => s.Replace ("\\", "\\\\").Replace ("\"", "\\\"");
			StringBuilder sb = new StringBuilder ();
			foreach (var (key, value) in param) {
				if (ct == hanContentType.UrlEncode) {
					sb.Append (sb.Length == 0 ? "" : "&");
					sb.Append ($"{HttpUtility.UrlEncode (key)}={HttpUtility.UrlEncode (value)}");
				} else if (ct == hanContentType.Json) {
					sb.Append (sb.Length == 0 ? "{" : ",");
					sb.Append ($@"""{_my_encode (key)}"":""{_my_encode (value)}""");
				} else if (ct == hanContentType.Xml) {
					sb.Append (sb.Length == 0 ? @"<?xml version=""1.0"" encoding=""UTF-8""?>" : "");
					sb.Append ($"<{HttpUtility.UrlEncode (key)}>{HttpUtility.UrlEncode (value)}</{HttpUtility.UrlEncode (key)}>");
				} else if (ct == hanContentType.FormData) {
					sb.Append ($@"--{boundary}{crlf}Content-Disposition: form-data; name=""{_my_encode (key)}""{crlf}{crlf}{value}{crlf}");
				}
			}
			sb.Append (ct == hanContentType.Json ? "}" : (ct == hanContentType.FormData ? $"--{boundary}--" : ""));
			string str_data = sb.ToString ();
			return (str_data, content_type);
		}

		// 处理 url
		private void _process_url (string url) {
			if (url.Length < 8 || (url.Substring (0, 7) != "http://" && url.Substring (0, 8) != "https://")) {
				if (m_last_url == "") {
					throw new Exception ("url format error");
				} else if (url[0] == '/') {
					int p = m_last_url.IndexOf ('/', 8);
					url = $"{(p >= 0 ? m_last_url.Substring (0, p) : m_last_url)}{url}";
				} else {
					int p = m_last_url.LastIndexOf ('/');
					url = $"{(p >= 8 ? m_last_url.Substring (0, p) : m_last_url)}/{url}";
				}
			}
			m_last_url = url;
		}

		// 生成 request 请求
		private HttpWebRequest _make_request (string url, string method, string param_data = "", string content_type = "") {
			_process_url (url);
			//lock (Log.g_locker) {
			//	Log.show_info ($"{method} async request: [{m_last_url}]");
			//	Log.show_info ($"post data: [{param_data}]");
			//}
			var uri = new Uri (m_last_url);
			HttpWebRequest req = WebRequest.CreateHttp (uri);
			req.Method = method;
			req.AllowAutoRedirect = true;
			req.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8";
			req.Headers["Accept-Encoding"] = "gzip, deflate";
			req.Headers["Accept-Language"] = "zh-CN,zh;q=0.8";
			req.Headers["Cache-Control"] = "max-age=0";
			req.KeepAlive = false;
			req.UserAgent = new Dictionary<hanUserAgent, string> {
				[hanUserAgent.Android] = "Mozilla/5.0 (Linux; Android 8.0; Pixel 2 Build/OPD3.170816.012) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/68.0.3440.75 Mobile Safari/537.36",
				[hanUserAgent.Chrome] = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/68.0.3440.75 Safari/537.36",
				[hanUserAgent.Edge] = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/64.0.3282.140 Safari/537.36 Edge/17.17134",
				[hanUserAgent.Ie] = "Mozilla/5.0 (Windows NT 10.0; WOW64; Trident/7.0; rv:11.0) like Gecko"
			}[m_ua];
			req.ContentType = content_type;
			req.Timeout = m_timeout_ms;
			req.ReadWriteTimeout = m_timeout_ms;
			req.CookieContainer = new CookieContainer ();
			foreach (Cookie c in m_cookies)
				req.CookieContainer.Add (uri, c);
			byte[] bytes = Encoding.UTF8.GetBytes (param_data);
			req.ContentLength = bytes.Length;
			if (bytes.Length > 0) {
				using (Stream req_stm = req.GetRequestStream ())
					req_stm.Write (bytes, 0, bytes.Length);
			}
			return req;
		}

		// 获取 response data
		private static byte[] _get_response_data (HttpWebResponse res) {
			using (Stream res_stm = res.GetResponseStream ()) {
				using (MemoryStream dms = new MemoryStream ()) {
					int len = 0;
					byte[] bytes = new byte[1024];
					if (res.ContentEncoding == "gzip") {
						using (GZipStream gzip = new GZipStream (res_stm, CompressionMode.Decompress)) {
							while ((len = gzip.Read (bytes, 0, bytes.Length)) > 0)
								dms.Write (bytes, 0, len);
							dms.Seek (0, SeekOrigin.Begin);
							return dms.ToArray ();
						}
					} else if (res.ContentEncoding == "deflate") {
						using (DeflateStream deflate = new DeflateStream (res_stm, CompressionMode.Decompress)) {
							while ((len = deflate.Read (bytes, 0, bytes.Length)) > 0)
								dms.Write (bytes, 0, len);
							dms.Seek (0, SeekOrigin.Begin);
							return dms.ToArray ();
						}
					} else {
						using (StreamReader res_stm_rdr = new StreamReader (res_stm, Encoding.UTF8))
							return _clear_bom (Encoding.UTF8.GetBytes (res_stm_rdr.ReadToEnd ()));
					}
				}
			}
		}

		private hanUserAgent m_ua;
		private CookieCollection m_cookies = new CookieCollection ();
		public static int m_timeout_ms = 10000;
		public string m_last_url = "";
	}

	class _hanHttpInit {
		private _hanHttpInit () {
			System.Net.ServicePointManager.DefaultConnectionLimit = 200;
			ServicePointManager.ServerCertificateValidationCallback = new System.Net.Security.RemoteCertificateValidationCallback ((sender, certificate, chain, errors) => { return true; });
		}
		private static _hanHttpInit m_init = new _hanHttpInit ();
	}
}
