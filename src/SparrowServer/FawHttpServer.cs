using SparrowServer.Attributes;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace SparrowServer {
	public class FawHttpServer {
		//private delegate void RequestHandler (FawRequest _req, FawResponse _res);
		private Dictionary<string, RequestStruct> m_request_handlers = new Dictionary<string, RequestStruct> ();
		private void add_handler (string path_prefix, RequestStruct _req_struct) {
			foreach (var key in m_request_handlers.Keys) {
				if (key == path_prefix) // key.left_is (path_prefix) || path_prefix.left_is (key)
					throw new Exception ("Url request address prefix conflict / URL请求地址前缀冲突");
			}
			m_request_handlers.Add (path_prefix, _req_struct);
		}

		// 数据检查
		public Func<string, int, bool, bool> m_check_int { get; set; } = null;
		public Func<string, long, bool, bool> m_check_long { get; set; } = null;
		public Func<string, string, bool, bool> m_check_string { get; set; } = null;



		public FawHttpServer (ushort port) {
			//https://www.w3cschool.cn/swaggerbootstrapui/swaggerbootstrapui-31rf32is.html
			string _local_path = Path.GetDirectoryName (typeof (FawHttpServer).Assembly.Location).Replace ('\\', '/');
			//
			Log.m_path = (_local_path.right_is ("/") ? _local_path : $"{_local_path}/");
			m_wwwroot = $"{Log.m_path}wwwroot/";
			//
			m_listener = new HttpListener ();
			m_listener.AuthenticationSchemes = AuthenticationSchemes.Anonymous;
			try {
				m_listener.Prefixes.Add ($"http://*:{port}/");
				m_listener.Start ();
			} catch (System.Net.HttpListenerException) {
				m_listener.Close ();
				m_listener = new HttpListener ();
				m_listener.AuthenticationSchemes = AuthenticationSchemes.Anonymous;
				m_listener.Prefixes.Add ($"http://127.0.0.1:{port}/");
				m_listener.Start ();
			}
		}

		public void set_doc_info (Assembly assembly, WEBDocInfo doc_info) {
			Swagger.DocBuilder _builder = new Swagger.DocBuilder (doc_info);
			foreach (var _type in assembly.GetTypes ()) {
				var _module_attr = _type.GetCustomAttribute (typeof (WEBModuleAttribute), true) as WEBModuleAttribute;
				if (_module_attr != null) {
					string _module_prefix = (_type.Name.right_is_nocase ("Module") ? _type.Name.left (_type.Name.Length - 6) : _type.Name);
					_builder.add_module (_type.Name, _module_prefix, _module_attr.m_description);
					//
					foreach (var _method in _type.GetMethods ()) {
						var _method_attrs = (from p in _method.GetCustomAttributes () where p is WEBMethod.IWEBMethod select p as WEBMethod.IWEBMethod);
						if (_method_attrs.Count () == 0)
							continue;
						var _method_attr = _method_attrs.First ();
						var _params = _method.GetGenericArguments ();
						var _request_type = _method_attr.Type;
						//
						_builder.add_method (_type.Name, _request_type, _method.Name, _method_attr.Summary, _method_attr.Description);
						string _path_prefix = $"/api/{_module_prefix}/{_method.Name}";
						if ((_params?.Length ?? 0) == 0) {
							add_handler ($"{_request_type} {_path_prefix}", new RequestStruct (_method));
						}
					}
				}
			}
			m_swagger_data = _builder.build ().to_bytes ();
		}

		// 处理一次请求
		private void _request_once (HttpListenerContext ctx) {
			HttpListenerRequest req = ctx.Request;
			HttpListenerResponse res = ctx.Response;

			FawRequest _req = new FawRequest () { _check_int = m_check_int, _check_long = m_check_long, _check_string = m_check_string };
			_req.m_url = req.RawUrl;// 此处缺失schema://domain:host
			_req.m_method = req.HttpMethod.ToUpper ();

			// 获取请求命令
			_req.m_path = req.RawUrl.simplify_path ();
			int _p = _req.m_path.IndexOfAny (new char [] { '?', '#' });
			if (_p > 0)
				_req.m_path = _req.m_path.Substring (0, _p);

			// 获取请求内容
			foreach (string str_key in req.QueryString.AllKeys)
				_req.m_gets.Add (str_key, req.QueryString [str_key]);
			if (_req.m_method == "POST")
				(_req.m_posts, _req.m_files) = parse_form (req);

			// 获取请求者IP
			if ((req.Headers ["X-Real-IP"]?.Length ?? 0) > 0) {
				_req.m_ip = req.Headers ["X-Real-IP"];
				_req.m_agent_ip = req.UserHostAddress;
			} else {
				_req.m_ip = req.UserHostAddress;
			}

			FawResponse _res = new FawResponse ();

			try {
				// 生成请求内容
				res.StatusCode = 200;
				byte [] result_data = new byte [0];
				if (_req.m_method != "HEAD") {
					try {
						if (_req.m_path.left_is ("/api/")) {
							bool _proc = false;
							if (m_request_handlers.ContainsKey ($"{_req.m_method} {_req.m_path}")) {
								m_request_handlers [$"{_req.m_method} {_req.m_path}"].process (_req, _res);
								_proc = true;
							}
							if (!_proc)
								throw new Exception ("Unknown request / 未知请求");
						} else if (_req.m_path.left_is ("/swagger/")) {
							if (_req.m_path == "/swagger/api.json") {
								_res.write (m_swagger_data);
								_res.set_content_from_filename (_req.m_path);
							} else {
								var _asm_path = $"{MethodBase.GetCurrentMethod ().DeclaringType.Namespace}.Swagger.res.{_req.m_path.mid ("/swagger/")}";
								using (var _stream = Assembly.GetCallingAssembly ().GetManifestResourceStream (_asm_path)) {
									if (_stream == null)
										throw new Exception ("File Not Found");
									var _buffer = new byte [_stream.Length];
									_stream.Read (_buffer);
									_res.write (_buffer);
									_res.set_content_from_filename (_asm_path);
								}
							}
						} else if (_req.m_method == "GET") {
							bool _is_404 = (_req.m_path == "/404.html");
							if (_is_404) {
								_res.m_status_code = 404;
							} else if (_req.m_path == "/") {
								if (File.Exists ($"{m_wwwroot}index.html")) {
									_req.m_path = "/index.html";
								} else if (File.Exists ($"{m_wwwroot}index.htm")) {
									_req.m_path = "/index.htm";
								}
							}
							_req.m_path = $"{m_wwwroot}{_req.m_path.Substring (1)}";
							if (File.Exists (_req.m_path)) {
								_res.write_file (_req.m_path);
							} else if (_is_404) {
								_res.write ("404 Not Found");
							} else {
								_res.redirect ("404.html");
							}
						}
						result_data = _res._get_writes ();
						res.StatusCode = _res.m_status_code;
					} catch (KeyNotFoundException ex) {
						result_data = get_failure_res ($"not given parameter {ex.Message.mid ("The given key '", "'")}");
						res.StatusCode = 500;
					} catch (Exception ex) {
						result_data = get_failure_res (ex.GetType ().Name == "Exception" ? ex.Message : ex.ToString ());
						res.StatusCode = 500;
					}
				}

				// 生成请求头
				foreach (var (_key, _value) in _res.m_headers)
					res.AppendHeader (_key, _value);
				Action<string, string> _unique_add_header = (_key, _value) => {
					if (!_res.m_headers.ContainsKey (_key))
						res.AppendHeader (_key, _value);
				};

				// HTTP头输入可以自定
				_unique_add_header ("Cache-Control", (_req.m_method == "GET" ? "private" : "no-store"));
				_unique_add_header ("Content-Type", "text/plain; charset=utf-8");
				_unique_add_header ("Server", "Microsoft-IIS/7.5"); // nginx/1.9.12
				_unique_add_header ("X-Powered-By", "ASP.NET");
				_unique_add_header ("X-AspNet-Version", "4.0.30319");
				_unique_add_header ("Access-Control-Allow-Origin", "*");
				_unique_add_header ("Access-Control-Allow-Headers", "X-Requested-With,Content-Type,Accept");
				_unique_add_header ("Access-Control-Allow-Methods", "HEAD,GET,POST,PUT,DELETE,OPTIONS");
				//_unique_add_header ("Content-Length", "");

				// 添加cookie
				//res.SetCookie (new Cookie ());

				// 是否启用压缩
				string [] encodings = req.Headers ["Accept-Encoding"]?.Split (',') ?? new string [0];
				encodings = (from p in encodings select p.Trim ().ToLower ()).ToArray ();
				if (Array.IndexOf (encodings, "gzip") >= 0) {
					// 使用 gzip 压缩
					_unique_add_header ("Content-Encoding", "gzip");
					_unique_add_header ("Vary", "Accept-Encoding");
					using (GZipStream gzip = new GZipStream (res.OutputStream, CompressionMode.Compress))
						gzip.Write (result_data, 0, result_data.Length);
				} else if (Array.IndexOf (encodings, "deflate") >= 0) {
					// 使用 deflate 压缩
					_unique_add_header ("Content-Encoding", "deflate");
					_unique_add_header ("Vary", "Accept-Encoding");
					using (DeflateStream deflate = new DeflateStream (res.OutputStream, CompressionMode.Compress))
						deflate.Write (result_data, 0, result_data.Length);
				} else {
					// 不使用压缩
					res.OutputStream.Write (result_data, 0, result_data.Length);
				}
			} catch (Exception ex) {
				Log.show_error (ex);
			} finally {
				res.Close ();
			}
		}

		// 循环请求
		public void run () {
			new Thread (() => {
				while (true) {
					Thread.Sleep (10000);
					GC.Collect ();
				}
			}).Start ();
			try {
				while (true) {
					var ctx = m_listener.GetContext ();
					Task.Factory.StartNew ((_ctx) => _request_once (_ctx as HttpListenerContext), ctx);
					//ThreadPool.QueueUserWorkItem ((_ctx) => { _request_once (_ctx as HttpListenerContext).Wait (); }, ctx);
				}
			} catch (Exception ex) {
				Log.show_error (ex);
			}
		}

		private string m_wwwroot = "";
		private HttpListener m_listener = null;
		private byte [] m_swagger_data = null;

		// 解析HTTP请求参数
		private static (Dictionary<string, string>, Dictionary<string, (string, byte [])>) parse_form (HttpListenerRequest req) {
			// 从流中读取一行字节数组
			Func<Stream, byte []> _read_bytes_line = (_stm) => {
				using (var resultStream = new MemoryStream ()) {
					byte last_byte = 0;
					while (true) {
						int data = _stm.ReadByte ();
						resultStream.WriteByte ((byte) data);
						if (data == 10 && last_byte == 13)
							break;
						last_byte = (byte) data;
					}
					resultStream.Position = 0;
					byte [] dataBytes = new byte [resultStream.Length];
					resultStream.Read (dataBytes, 0, dataBytes.Length);
					return dataBytes;
				}
			};

			// 返回数据
			var post_param = new Dictionary<string, string> ();
			var post_file = new Dictionary<string, (string, byte [])> ();
			try {
				if (req.HttpMethod != "POST") {
					return (post_param, post_file);
				} else if (left_is (req.ContentType, "multipart/form-data;")) {
					Encoding encoding = req.ContentEncoding;
					string [] values = req.ContentType.Split (';').Skip (1).ToArray ();
					string boundary = string.Join (";", values).Replace ("boundary=", "").Trim ();
					byte [] bytes_boundary = encoding.GetBytes ($"--{boundary}\r\n");
					byte [] bytes_end_boundary = encoding.GetBytes ($"--{boundary}--\r\n");
					Stream SourceStream = req.InputStream;
					var bytes = _read_bytes_line (SourceStream);
					if (bytes == bytes_end_boundary) {
						return (post_param, post_file);
					} else if (!compare (bytes, bytes_boundary)) {
						Log.show_info ("Parse Error in [first read is not bytes_boundary]");
						return (post_param, post_file);
					}
					while (true) {
						bytes = _read_bytes_line (SourceStream);
						string _tmp = encoding.GetString (bytes);//Content-Disposition: form-data; name="text_"
						if (!left_is (_tmp, "Content-Disposition:")) {
							Log.show_info ("Parse Error in [begin block is not Content-Disposition]");
							return (post_param, post_file);
						}
						string name = substr_mid (_tmp, "name=\"", "\"");
						string filename = substr_mid (_tmp, "filename=\"", "\"");
						do {
							bytes = _read_bytes_line (SourceStream);
						} while (bytes [0] != 13 || bytes [1] != 10);
						bytes = _read_bytes_line (SourceStream);
						using (var ms = new MemoryStream ()) {
							while (!compare (bytes, bytes_boundary) && !compare (bytes, bytes_end_boundary)) {
								ms.Write (bytes, 0, bytes.Length);
								if (ms.Length > 5 * 1024 * 1024)
									throw new Exception ("The uploading of files exceeding 5M is not currently supported / 暂不支持超过5M的文件的上传");
								bytes = _read_bytes_line (SourceStream);
							}
							if (ms.Length < 2) {
								Log.show_info ("Parse Error in [ms.Length < 2]");
								return (post_param, post_file);
							}
							bytes = new byte [ms.Length - 2];
							if (bytes.Length > 2) {
								ms.Position = 0;
								ms.Read (bytes, 0, bytes.Length);
							}
							if (string.IsNullOrEmpty (filename)) {
								post_param [name] = encoding.GetString (bytes);
							} else {
								post_file [name] = (filename, bytes);
							}
						}
					}
				} else {
					using (StreamReader sr = new StreamReader (req.InputStream, Encoding.UTF8)) {
						string post_data = sr.ReadToEnd ();
						if (post_data [0] == '{') {
							JObject obj = JObject.Parse (post_data);
							foreach (var (key, val) in obj)
								post_param [HttpUtility.UrlDecode (key)] = HttpUtility.UrlDecode (val.ToString ());
						} else {
							string [] pairs = post_data.Split (new char [] { '&' }, StringSplitOptions.RemoveEmptyEntries);
							foreach (string pair in pairs) {
								int p = pair.IndexOf ('=');
								if (p > 0)
									post_param [HttpUtility.UrlDecode (pair.Substring (0, p))] = HttpUtility.UrlDecode (pair.Substring (p + 1));
							}
						}
					}
				}
			} catch (Exception ex) {
				Log.show_error (ex);
			}
			return (post_param, post_file);
		}

		private static bool compare (byte [] arr1, byte [] arr2) {
			if (arr1 == null && arr2 == null)
				return true;
			else if (arr1 == null || arr2 == null)
				return false;
			else if (arr1.Length != arr2.Length)
				return false;
			for (int i = 0; i < arr1.Length; ++i) {
				if (arr1 [i] != arr2 [i])
					return false;
			}
			return true;
		}

		private static bool left_is (string s, string s2) {
			if (string.IsNullOrEmpty (s))
				return string.IsNullOrEmpty (s2);
			if (s.Length < s2.Length)
				return false;
			return s.Substring (0, s2.Length) == s2;
		}

		private static string substr_mid (string s, string begin, string end = "") {
			if (string.IsNullOrEmpty (s) || string.IsNullOrEmpty (begin))
				return "";
			int p = s.IndexOf (begin);
			if (p == -1)
				return "";
			s = s.Substring (p + begin.Length);
			if (!string.IsNullOrEmpty (end)) {
				p = s.IndexOf (end);
				if (p >= 0)
					s = s.Substring (0, p);
			}
			return s;
		}

		private static byte [] get_failure_res (string content) {
			return Encoding.UTF8.GetBytes (JObject.FromObject (new { result = "failure", content }).ToString (Newtonsoft.Json.Formatting.None));
		}

		private static Dictionary<string, string> s_plain = new Dictionary<string, string> () { ["323"] = "text/h323", ["acx"] = "application/internet-property-stream", ["ai"] = "application/postscript", ["aif"] = "audio/x-aiff", ["aifc"] = "audio/x-aiff", ["aiff"] = "audio/x-aiff", ["asf"] = "video/x-ms-asf", ["asr"] = "video/x-ms-asf", ["asx"] = "video/x-ms-asf", ["au"] = "audio/basic", ["avi"] = "video/x-msvideo", ["axs"] = "application/olescript", ["bas"] = "text/plain", ["bcpio"] = "application/x-bcpio", ["bin"] = "application/octet-stream", ["bmp"] = "image/bmp", ["c"] = "text/plain", ["cat"] = "application/vnd.ms-pkiseccat", ["cdf"] = "application/x-cdf", ["cer"] = "application/x-x509-ca-cert", ["class"] = "application/octet-stream", ["clp"] = "application/x-msclip", ["cmx"] = "image/x-cmx", ["cod"] = "image/cis-cod", ["cpio"] = "application/x-cpio", ["crd"] = "application/x-mscardfile", ["crl"] = "application/pkix-crl", ["crt"] = "application/x-x509-ca-cert", ["csh"] = "application/x-csh", ["css"] = "text/css", ["dcr"] = "application/x-director", ["der"] = "application/x-x509-ca-cert", ["dir"] = "application/x-director", ["dll"] = "application/x-msdownload", ["dms"] = "application/octet-stream", ["doc"] = "application/msword", ["dot"] = "application/msword", ["dvi"] = "application/x-dvi", ["dxr"] = "application/x-director", ["eps"] = "application/postscript", ["etx"] = "text/x-setext", ["evy"] = "application/envoy", ["exe"] = "application/octet-stream", ["fif"] = "application/fractals", ["flr"] = "x-world/x-vrml", ["gif"] = "image/gif", ["gtar"] = "application/x-gtar", ["gz"] = "application/x-gzip", ["h"] = "text/plain", ["hdf"] = "application/x-hdf", ["hlp"] = "application/winhlp", ["hqx"] = "application/mac-binhex40", ["hta"] = "application/hta", ["htc"] = "text/x-component", ["htm"] = "text/html", ["html"] = "text/html", ["htt"] = "text/webviewhtml", ["ico"] = "image/x-icon", ["ief"] = "image/ief", ["iii"] = "application/x-iphone", ["ins"] = "application/x-internet-signup", ["isp"] = "application/x-internet-signup", ["jfif"] = "image/pipeg", ["jpe"] = "image/jpeg", ["jpeg"] = "image/jpeg", ["jpg"] = "image/jpeg", ["js"] = "application/x-javascript", ["latex"] = "application/x-latex", ["lha"] = "application/octet-stream", ["lsf"] = "video/x-la-asf", ["lsx"] = "video/x-la-asf", ["lzh"] = "application/octet-stream", ["m13"] = "application/x-msmediaview", ["m14"] = "application/x-msmediaview", ["m3u"] = "audio/x-mpegurl", ["man"] = "application/x-troff-man", ["mdb"] = "application/x-msaccess", ["me"] = "application/x-troff-me", ["mht"] = "message/rfc822", ["mhtml"] = "message/rfc822", ["mid"] = "audio/mid", ["mny"] = "application/x-msmoney", ["mov"] = "video/quicktime", ["movie"] = "video/x-sgi-movie", ["mp2"] = "video/mpeg", ["mp3"] = "audio/mpeg", ["mpa"] = "video/mpeg", ["mpe"] = "video/mpeg", ["mpeg"] = "video/mpeg", ["mpg"] = "video/mpeg", ["mpp"] = "application/vnd.ms-project", ["mpv2"] = "video/mpeg", ["ms"] = "application/x-troff-ms", ["mvb"] = "application/x-msmediaview", ["nws"] = "message/rfc822", ["oda"] = "application/oda", ["p10"] = "application/pkcs10", ["p12"] = "application/x-pkcs12", ["p7b"] = "application/x-pkcs7-certificates", ["p7c"] = "application/x-pkcs7-mime", ["p7m"] = "application/x-pkcs7-mime", ["p7r"] = "application/x-pkcs7-certreqresp", ["p7s"] = "application/x-pkcs7-signature", ["pbm"] = "image/x-portable-bitmap", ["pdf"] = "application/pdf", ["pfx"] = "application/x-pkcs12", ["pgm"] = "image/x-portable-graymap", ["pko"] = "application/ynd.ms-pkipko", ["pma"] = "application/x-perfmon", ["pmc"] = "application/x-perfmon", ["pml"] = "application/x-perfmon", ["pmr"] = "application/x-perfmon", ["pmw"] = "application/x-perfmon", ["pnm"] = "image/x-portable-anymap", ["pot,"] = "application/vnd.ms-powerpoint", ["ppm"] = "image/x-portable-pixmap", ["pps"] = "application/vnd.ms-powerpoint", ["ppt"] = "application/vnd.ms-powerpoint", ["prf"] = "application/pics-rules", ["ps"] = "application/postscript", ["pub"] = "application/x-mspublisher", ["qt"] = "video/quicktime", ["ra"] = "audio/x-pn-realaudio", ["ram"] = "audio/x-pn-realaudio", ["ras"] = "image/x-cmu-raster", ["rgb"] = "image/x-rgb", ["rmi"] = "audio/mid", ["roff"] = "application/x-troff", ["rtf"] = "application/rtf", ["rtx"] = "text/richtext", ["scd"] = "application/x-msschedule", ["sct"] = "text/scriptlet", ["setpay"] = "application/set-payment-initiation", ["setreg"] = "application/set-registration-initiation", ["sh"] = "application/x-sh", ["shar"] = "application/x-shar", ["sit"] = "application/x-stuffit", ["snd"] = "audio/basic", ["spc"] = "application/x-pkcs7-certificates", ["spl"] = "application/futuresplash", ["src"] = "application/x-wais-source", ["sst"] = "application/vnd.ms-pkicertstore", ["stl"] = "application/vnd.ms-pkistl", ["stm"] = "text/html", ["svg"] = "image/svg+xml", ["sv4cpio"] = "application/x-sv4cpio", ["sv4crc"] = "application/x-sv4crc", ["swf"] = "application/x-shockwave-flash", ["t"] = "application/x-troff", ["tar"] = "application/x-tar", ["tcl"] = "application/x-tcl", ["tex"] = "application/x-tex", ["texi"] = "application/x-texinfo", ["texinfo"] = "application/x-texinfo", ["tgz"] = "application/x-compressed", ["tif"] = "image/tiff", ["tiff"] = "image/tiff", ["tr"] = "application/x-troff", ["trm"] = "application/x-msterminal", ["tsv"] = "text/tab-separated-values", ["txt"] = "text/plain", ["uls"] = "text/iuls", ["ustar"] = "application/x-ustar", ["vcf"] = "text/x-vcard", ["vrml"] = "x-world/x-vrml", ["wav"] = "audio/x-wav", ["wcm"] = "application/vnd.ms-works", ["wdb"] = "application/vnd.ms-works", ["wks"] = "application/vnd.ms-works", ["wmf"] = "application/x-msmetafile", ["wps"] = "application/vnd.ms-works", ["wri"] = "application/x-mswrite", ["wrl"] = "x-world/x-vrml", ["wrz"] = "x-world/x-vrml", ["xaf"] = "x-world/x-vrml", ["xbm"] = "image/x-xbitmap", ["xla"] = "application/vnd.ms-excel", ["xlc"] = "application/vnd.ms-excel", ["xlm"] = "application/vnd.ms-excel", ["xls"] = "application/vnd.ms-excel", ["xlt"] = "application/vnd.ms-excel", ["xlw"] = "application/vnd.ms-excel", ["xof"] = "x-world/x-vrml", ["xpm"] = "image/x-xpixmap", ["xwd"] = "image/x-xwindowdump", ["z"] = "application/x-compress", ["zip"] = "application/zip" };
		public static string _get_content_type (string _ext) {
			return (s_plain.ContainsKey (_ext) ? s_plain [_ext] : "application/octet-stream");
		}
	}
}
