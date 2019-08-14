//using SparrowServer.Attributes;
//using Newtonsoft.Json.Linq;
//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Net;
//using System.Reflection;
//using System.Text;
//using System.Threading;
//using System.Threading.Tasks;
//using System.Web;
//using SparrowServer.Monitor;
//using SparrowServer.HttpProtocol;

//namespace SparrowServer {
//	public class FawHttpServer {
//		//private delegate void RequestHandler (FawRequest _req, FawResponse _res);
//		private Dictionary<string, RequestStruct> m_request_handlers = new Dictionary<string, RequestStruct> ();
//		private void add_handler (string path_prefix, RequestStruct _req_struct) {
//			foreach (var key in m_request_handlers.Keys) {
//				if (key == path_prefix) // key.left_is (path_prefix) || path_prefix.left_is (key)
//					throw new Exception ("Url request address prefix conflict");
//			}
//			m_request_handlers.Add (path_prefix, _req_struct);
//		}

//		// data check
//		public Func<string, int, bool, bool> m_check_int { get; set; } = null;
//		public Func<string, long, bool, bool> m_check_long { get; set; } = null;
//		public Func<string, string, bool, bool> m_check_string { get; set; } = null;



//		public FawHttpServer (ushort port, Assembly assembly, string jwt_secret) {
//			m_assembly = assembly;
//			JWTManager.m_secret = jwt_secret;
//			//https://www.w3cschool.cn/swaggerbootstrapui/swaggerbootstrapui-31rf32is.html
//			string _local_path = Path.GetDirectoryName (typeof (FawHttpServer).Assembly.Location).Replace ('\\', '/');
//			//
//			Log.m_path = (_local_path.right_is ("/") ? _local_path : $"{_local_path}/");
//			m_wwwroot = $"{Log.m_path}wwwroot/";
//			//
//			m_listener = new HttpListener ();
//			m_listener.AuthenticationSchemes = AuthenticationSchemes.Anonymous;
//			try {
//				m_listener.Prefixes.Add ($"http://*:{port}/");
//				m_listener.Start ();
//			} catch (System.Net.HttpListenerException) {
//				m_listener.Close ();
//				m_listener = new HttpListener ();
//				m_listener.AuthenticationSchemes = AuthenticationSchemes.Anonymous;
//				m_listener.Prefixes.Add ($"http://127.0.0.1:{port}/");
//				m_listener.Start ();
//			}
//		}

//		public void set_doc_info (WEBDocInfo doc_info) {
//			m_doc_info = doc_info;
//		}

//		public void enable_monitor (bool _enable = true) {
//			if (_enable != (m_monitor != null)) {
//				m_monitor = (_enable ? new MonitoringManager () : null);
//			}
//		}

//		// processing a request
//		private void _request_once (HttpListenerContext ctx) {
//			var _request_begin = DateTime.Now;
//			bool _static = true, _error = false;

//			HttpListenerRequest req = ctx.Request;
//			HttpListenerResponse res = ctx.Response;

//			FawRequest _req = new FawRequest () { _check_int = m_check_int, _check_long = m_check_long, _check_string = m_check_string };
//			FawResponse _res = new FawResponse ();
//			_req.m_url = req.RawUrl; // this valus is lose schema://domain:host
//			_req.m_option = req.HttpMethod.ToUpper ();
//			_req.m_path = req.RawUrl.simplify_path ();
//			int _p = _req.m_path.IndexOfAny (new char [] { '?', '#' });
//			if (_p > 0)
//				_req.m_path = _req.m_path.Substring (0, _p);

//			// get ip and agent-ip
//			if ((req.Headers ["X-Real-IP"]?.Length ?? 0) > 0) {
//				_req.m_ip = req.Headers ["X-Real-IP"];
//				_req.m_agent_ip = req.UserHostAddress;
//			} else {
//				_req.m_ip = req.UserHostAddress;
//			}

//			// get request header/query/post parameters
//			foreach (var _key in req.Headers.AllKeys)
//				_req.m_headers.Add (_key, req.Headers [_key]);
//			foreach (string str_key in req.QueryString.AllKeys)
//				_req.m_gets.Add (str_key, req.QueryString [str_key]);
//			if (_req.m_option == "POST")
//				(_req.m_posts, _req.m_files) = parse_form (req);

//			try {
//				// compare query header
//				res.StatusCode = 200;
//				byte [] result_data = new byte [0];
//				if (_req.m_option != "HEAD") {
//					try {
//						if (_req.m_path.left_is ("/api/")) {
//							_static = false;
//							bool _proc = false;
//							if (m_request_handlers.ContainsKey ($"{_req.m_option} {_req.m_path}")) {
//								m_request_handlers [$"{_req.m_option} {_req.m_path}"].process (_req, _res);
//								_proc = true;
//							} else if (m_request_handlers.ContainsKey ($" {_req.m_path}")) {
//								m_request_handlers [$" {_req.m_path}"].process (_req, _res);
//								_proc = true;
//							}
//							if (!_proc)
//								throw new Exception ("Unknown request");
//						} else if (_req.m_path.left_is ("/swagger/") && m_doc_info != null) {
//							if (_req.m_path == "/swagger/api.json") {
//								_res.write (m_swagger_data);
//								_res.set_content_from_filename (_req.m_path);
//							} else {
//								var _asm_path = $"{MethodBase.GetCurrentMethod ().DeclaringType.Namespace}.Swagger.res.{_req.m_path.mid ("/swagger/")}";
//								using (var _stream = Assembly.GetCallingAssembly ().GetManifestResourceStream (_asm_path)) {
//									if (_stream == null)
//										throw new Exception ("File Not Found");
//									var _buffer = new byte [_stream.Length];
//									_stream.Read (_buffer);
//									_res.write (_buffer);
//									_res.set_content_from_filename (_asm_path);
//								}
//							}
//						} else if (_req.m_path.left_is ("/monitor/") && m_monitor != null) {
//							if (_req.m_path == "/monitor/data.json") {
//								_res.write (m_monitor.get_json (_req.get_value<int> ("count", false)));
//								//_res.set_content_from_filename (_req.m_path);
//							}
//						} else if (_req.m_option == "GET") {
//							bool _is_404 = (_req.m_path == "/404.html");
//							if (_is_404) {
//								_res.m_status_code = 404;
//							} else if (_req.m_path == "/") {
//								if (File.Exists ($"{m_wwwroot}index.html")) {
//									_req.m_path = "/index.html";
//								} else if (File.Exists ($"{m_wwwroot}index.htm")) {
//									_req.m_path = "/index.htm";
//								}
//							}
//							_req.m_path = $"{m_wwwroot}{_req.m_path.Substring (1)}";
//							if (File.Exists (_req.m_path)) {
//								_res.write_file (_req.m_path);
//							} else if (_is_404) {
//								_res.write ("404 Not Found");
//							} else {
//								_res.redirect ("404.html");
//							}
//						}
//						result_data = _res._get_writes ();
//						res.StatusCode = _res.m_status_code;
//					} catch (KeyNotFoundException ex) {
//						result_data = get_failure_res ($"not given parameter {ex.Message.mid ("The given key '", "'")}");
//						res.StatusCode = 500;
//						_error = true;
//					} catch (_AuthException) {
//						result_data = get_failure_res ("Authorize failed");
//						res.StatusCode = 401;
//						_error = true;
//					} catch (Exception ex) {
//						result_data = get_failure_res (ex.GetType ().Name == "Exception" ? ex.Message : ex.ToString ());
//						res.StatusCode = 500;
//						_error = true;
//					}
//				}

//				// 生成请求头
//				foreach (var (_key, _value) in _res.m_headers)
//					res.AppendHeader (_key, _value);
//				Action<string, string> _unique_add_header = (_key, _value) => {
//					if (!_res.m_headers.ContainsKey (_key))
//						res.AppendHeader (_key, _value);
//				};

//				// HTTP头输入可以自定
//				_unique_add_header ("Cache-Control", (_req.m_option == "GET" ? "private" : "no-store"));
//				_unique_add_header ("Content-Type", "text/plain; charset=utf-8");
//				_unique_add_header ("Server", "Microsoft-IIS/7.5"); // nginx/1.9.12
//				_unique_add_header ("X-Powered-By", "ASP.NET");
//				_unique_add_header ("X-AspNet-Version", "4.0.30319");
//				_unique_add_header ("Access-Control-Allow-Origin", "*");
//				_unique_add_header ("Access-Control-Allow-Headers", "X-Requested-With,Content-Type,Accept");
//				_unique_add_header ("Access-Control-Allow-Methods", "HEAD,GET,POST,PUT,DELETE,OPTIONS");
//				//_unique_add_header ("Content-Length", "");

//				// 添加cookie
//				//res.SetCookie (new Cookie ());

//				// 是否启用压缩
//				string [] encodings = req.Headers ["Accept-Encoding"]?.Split (',') ?? new string [0];
//				encodings = (from p in encodings select p.Trim ().ToLower ()).ToArray ();
//				if (Array.IndexOf (encodings, "gzip") >= 0) {
//					// 使用 gzip 压缩
//					_unique_add_header ("Content-Encoding", "gzip");
//					_unique_add_header ("Vary", "Accept-Encoding");
//					result_data = result_data.gzip_compress ();
//				} else if (Array.IndexOf (encodings, "deflate") >= 0) {
//					// 使用 deflate 压缩
//					_unique_add_header ("Content-Encoding", "deflate");
//					_unique_add_header ("Vary", "Accept-Encoding");
//					result_data = result_data.deflate_compress ();
//				}
//				res.OutputStream.Write (result_data, 0, result_data.Length);
//			} catch (Exception ex) {
//				Log.show_error (ex);
//			} finally {
//				res.Close ();
//				m_monitor?.OnRequest (_static, (long) ((DateTime.Now - _request_begin).TotalMilliseconds + 0.5000001), _error);
//			}
//		}

//		// loop processing
//		public void run () {
//			Swagger.DocBuilder _builder = (m_doc_info != null ? new Swagger.DocBuilder (m_doc_info) : null);
//			foreach (var _module in m_assembly.GetTypes ()) {
//				var _module_attr = _module.GetCustomAttribute (typeof (HTTPModuleAttribute), true) as HTTPModuleAttribute;
//				if (_module_attr != null) {
//					string _module_prefix = (_module.Name.right_is_nocase ("Module") ? _module.Name.left (_module.Name.Length - 6) : _module.Name);
//					_builder?.add_module (_module.Name, _module_prefix, _module_attr.m_description);
//					//
//					MethodInfo _jwt_reconnect_func = null;
//					Action<MethodInfo> _process_method = (_method) => {
//						// process attribute
//						var _method_attrs = (from p in _method.GetCustomAttributes () where p is IHTTPMethod select p as IHTTPMethod);
//						if (_method_attrs.Count () > 1) {
//							throw new Exception ("Simultaneous use of multiple HTTP Attribute is not supported");
//						}
//						var _method_attr = (_method_attrs.Count () > 0 ? _method_attrs.First () : null);
//						//
//						var _jwt_attrs = (from p in _method.GetCustomAttributes () where p is IJWTMethod select p as IJWTMethod);
//						if (_jwt_attrs.Count () > 1) {
//							throw new Exception ("Simultaneous use of multiple HTTP Attribute is not supported");
//						}
//						var _jwt_type = (_jwt_attrs.Count () > 0 ? _jwt_attrs.First ().Type : "");
//						//
//						var _params = _method.GetParameters ();
//						if (_jwt_type == "Request") {
//							if (_method.ReturnType != _module)
//								throw new Exception ("Return value in [JWTRequest] method must be current class type");
//							if (!_method.IsStatic)
//								throw new Exception ("[JWTRequest] method must be static");
//							if (_params.Length != 1 || _params [0].ParameterType != typeof (JObject))
//								throw new Exception ("[JWTRequest] can only have one parameter, and the type of parameter is JObject");
//							if (_jwt_reconnect_func != null)
//								throw new Exception ("[JWTRequest] cannot appear twice in the same module");
//							_jwt_reconnect_func = _method;
//						} else if (_method_attr != null) {
//							if (!_method.IsStatic && _jwt_reconnect_func == null)
//								throw new Exception ("A module that has a non-static HTTP method must contain the [JWTRequest] method");
//							_builder?.add_method (_module.Name, _method_attr.Type, _method.Name, !_method.IsStatic, _method_attr.Summary, _method_attr.Description);
//							//
//							string _path_prefix = $"/api/{_module_prefix}/{_method.Name}";
//							foreach (var _param in _params) {
//								if (_param.ParameterType == typeof (FawRequest) || _param.ParameterType == typeof (FawResponse))
//									continue;
//								if (((from p in _param.GetCustomAttributes () where p is IReqParam select 1).Count ()) > 0)
//									continue;
//								var _param_desps = (from p in _param.GetCustomAttributes () where p is ParamAttribute select (p as ParamAttribute).m_description);
//								var _param_desp = (_param_desps.Count () > 0 ? _param_desps.First () : "");
//								_builder?.add_param (_module.Name, _method_attr.Type, _method.Name, _param.Name, _param.ParameterType.Name, _param_desp);
//							}
//							add_handler ($"{_method_attr.Type} {_path_prefix}", new RequestStruct (_method, _jwt_reconnect_func, _jwt_type));
//						}
//					};
//					// process static method
//					foreach (var _method in _module.GetMethods ()) {
//						if (!_method.IsStatic)
//							continue;
//						_process_method (_method);
//					}
//					// process dynamic method
//					foreach (var _method in _module.GetMethods ()) {
//						if (_method.IsStatic)
//							continue;
//						_process_method (_method);
//					}
//				}
//			}
//			m_swagger_data = _builder?.build ().to_bytes ();
//			//
//			new Thread (() => {
//				while (true) {
//					Thread.Sleep (10000);
//					GC.Collect ();
//				}
//			}).Start ();
//			//
//			try {
//				while (true) {
//					var ctx = m_listener.GetContext ();
//					Task.Factory.StartNew ((_ctx) => _request_once (_ctx as HttpListenerContext), ctx);
//					//ThreadPool.QueueUserWorkItem ((_ctx) => { _request_once (_ctx as HttpListenerContext).Wait (); }, ctx);
//				}
//			} catch (Exception ex) {
//				Log.show_error (ex);
//			}
//		}

//		private Assembly m_assembly = null;
//		private WEBDocInfo m_doc_info = null;
//		private string m_wwwroot = "";
//		private HttpListener m_listener = null;
//		private byte [] m_swagger_data = null;
//		private MonitoringManager m_monitor = null;

//		// 解析HTTP请求参数
//		private static (Dictionary<string, string>, Dictionary<string, (string, byte [])>) parse_form (HttpListenerRequest req) {
//			// 请求数据
//			var post_param = new Dictionary<string, string> ();
//			var post_file = new Dictionary<string, (string, byte [])> ();
//			try {
//				if (req.HttpMethod != "POST") {
//					return (post_param, post_file);
//				} else {
//					var _encoding = req.Headers ["Content-Encoding"];
//					var _content_data = new List<byte> ();
//					using (req.InputStream) {
//						while (_content_data.Count < 5 * 1024 * 1024) {
//							int _byte = req.InputStream.ReadByte ();
//							if (_byte == -1)
//								break;
//							_content_data.Add ((byte) _byte);
//						}
//					}
//					if (_content_data.Count >= 5 * 1024 * 1024)
//						throw new Exception ("The uploading of files exceeding 5M is not currently supported");
//					if (_encoding == "gzip") {
//						_content_data = _content_data.gzip_decompress (5 * 1024 * 1024);
//					} else if (_encoding == "deflate") {
//						_content_data = _content_data.deflate_decompress (5 * 1024 * 1024);
//					}
//					if (req.ContentType.left_is ("multipart/form-data;")) {
//						string [] values = req.ContentType.Split (';').Skip (1).ToArray ();
//						string boundary = string.Join (";", values).Replace ("boundary=", "").Trim ();
//						byte [] bytes_boundary = Encoding.UTF8.GetBytes ($"--{boundary}");
//						//
//						if (!_left_is (_content_data, bytes_boundary))
//							throw new Exception ("Parse Error in [first read is not bytes_boundary]");
//						_content_data.RemoveRange (0, bytes_boundary.Length);
//						//
//						byte [] _end_line, _end_line2;
//						if (_left_is (_content_data, "\r\n".to_bytes ())) {
//							_end_line = "\r\n".to_bytes ();
//							_end_line2 = "\r\n\r\n".to_bytes ();
//						} else if (_left_is (_content_data, "\n".to_bytes ())) {
//							_end_line = "\n".to_bytes ();
//							_end_line2 = "\n\n".to_bytes ();
//						} else if (_left_is (_content_data, "--".to_bytes ())) {
//							return (post_param, post_file);
//						} else {
//							throw new Exception ("Parse Error in [No newline character is recognized]");
//						}
//						while (true) {
//							if (_content_data.Count < 5 || _left_is (_content_data, "--".to_bytes ()))
//								return (post_param, post_file);
//							_content_data.RemoveRange (0, _end_line.Length);
//							int _p = _find (_content_data, _end_line);
//							string _tmp = _left (_content_data, _p).to_str ();
//							if (!_tmp.left_is_nocase ("Content-Disposition:"))
//								throw new Exception ("Parse Error in [begin block is not Content-Disposition]");
//							string _name = _tmp.mid ("name=\"", "\"");
//							string _filename = _tmp.mid ("filename=\"", "\"");
//							if ((_p = _find (_content_data, _end_line2)) < 0)
//								throw new Exception ("Parse Error in [do not find value in block]");
//							_content_data.RemoveRange (0, _p + _end_line2.Length);
//							_p = _find (_content_data, bytes_boundary);
//							if (_p - _end_line.Length < 0)
//								throw new Exception ("Parse Error in [end block is not Content-Disposition]");
//							byte [] _value = _left (_content_data, _p - _end_line.Length);
//							_content_data.RemoveRange (0, _p + bytes_boundary.Length);
//							if (_filename.is_null ()) {
//								post_param [_name] = Encoding.UTF8.GetString (_value);
//							} else {
//								post_file [_name] = (_filename, _value);
//							}
//						}
//					} else {
//						string post_data = _content_data.to_str ();
//						if (post_data [0] == '{') {
//							JObject obj = JObject.Parse (post_data);
//							foreach (var (key, val) in obj)
//								post_param [HttpUtility.UrlDecode (key)] = HttpUtility.UrlDecode (val.ToString ());
//						} else {
//							string [] pairs = post_data.Split (new char [] { '&' }, StringSplitOptions.RemoveEmptyEntries);
//							foreach (string pair in pairs) {
//								int p = pair.IndexOf ('=');
//								if (p > 0)
//									post_param [HttpUtility.UrlDecode (pair.Substring (0, p))] = HttpUtility.UrlDecode (pair.Substring (p + 1));
//							}
//						}
//					}
//				}
//			} catch (Exception ex) {
//				Log.show_error (ex);
//			}
//			return (post_param, post_file);
//		}

//		// 判断字符数组是否以什么开始
//		public static bool _left_is (List<byte> l, byte [] a) {
//			if (l.Count < a.Length)
//				return false;
//			for (int i = 0; i < a.Length; ++i) {
//				if (l [i] != a [i])
//					return false;
//			}
//			return true;
//		}

//		// 从字符数组中截取字符串
//		public static byte [] _left (List<byte> l, int n) {
//			var _arr = new byte [Math.Min (n, l.Count)];
//			for (int i = 0; i < _arr.Length; ++i)
//				_arr [i] = l [i];
//			return _arr;
//		}

//		// 从字符数组中寻找目标字符集
//		public static int _find (List<byte> l, byte [] a) {
//			for (int i = 0; i < l.Count - a.Length + 1; ++i) {
//				int j = 0;
//				for (; j < a.Length; ++j) {
//					if (l [i + j] != a [j])
//						break;
//				}
//				if (j == a.Length)
//					return i;
//			}
//			return -1;
//		}

//		private static byte [] get_failure_res (string content) {
//			return Encoding.UTF8.GetBytes (JObject.FromObject (new { result = "failure", content }).ToString (Newtonsoft.Json.Formatting.None));
//		}

//		private static Dictionary<string, string> s_plain = new Dictionary<string, string> () { ["323"] = "text/h323", ["acx"] = "application/internet-property-stream", ["ai"] = "application/postscript", ["aif"] = "audio/x-aiff", ["aifc"] = "audio/x-aiff", ["aiff"] = "audio/x-aiff", ["asf"] = "video/x-ms-asf", ["asr"] = "video/x-ms-asf", ["asx"] = "video/x-ms-asf", ["au"] = "audio/basic", ["avi"] = "video/x-msvideo", ["axs"] = "application/olescript", ["bas"] = "text/plain", ["bcpio"] = "application/x-bcpio", ["bin"] = "application/octet-stream", ["bmp"] = "image/bmp", ["c"] = "text/plain", ["cat"] = "application/vnd.ms-pkiseccat", ["cdf"] = "application/x-cdf", ["cer"] = "application/x-x509-ca-cert", ["class"] = "application/octet-stream", ["clp"] = "application/x-msclip", ["cmx"] = "image/x-cmx", ["cod"] = "image/cis-cod", ["cpio"] = "application/x-cpio", ["crd"] = "application/x-mscardfile", ["crl"] = "application/pkix-crl", ["crt"] = "application/x-x509-ca-cert", ["csh"] = "application/x-csh", ["css"] = "text/css", ["dcr"] = "application/x-director", ["der"] = "application/x-x509-ca-cert", ["dir"] = "application/x-director", ["dll"] = "application/x-msdownload", ["dms"] = "application/octet-stream", ["doc"] = "application/msword", ["dot"] = "application/msword", ["dvi"] = "application/x-dvi", ["dxr"] = "application/x-director", ["eps"] = "application/postscript", ["etx"] = "text/x-setext", ["evy"] = "application/envoy", ["exe"] = "application/octet-stream", ["fif"] = "application/fractals", ["flr"] = "x-world/x-vrml", ["gif"] = "image/gif", ["gtar"] = "application/x-gtar", ["gz"] = "application/x-gzip", ["h"] = "text/plain", ["hdf"] = "application/x-hdf", ["hlp"] = "application/winhlp", ["hqx"] = "application/mac-binhex40", ["hta"] = "application/hta", ["htc"] = "text/x-component", ["htm"] = "text/html", ["html"] = "text/html", ["htt"] = "text/webviewhtml", ["ico"] = "image/x-icon", ["ief"] = "image/ief", ["iii"] = "application/x-iphone", ["ins"] = "application/x-internet-signup", ["isp"] = "application/x-internet-signup", ["jfif"] = "image/pipeg", ["jpe"] = "image/jpeg", ["jpeg"] = "image/jpeg", ["jpg"] = "image/jpeg", ["js"] = "application/x-javascript", ["latex"] = "application/x-latex", ["lha"] = "application/octet-stream", ["lsf"] = "video/x-la-asf", ["lsx"] = "video/x-la-asf", ["lzh"] = "application/octet-stream", ["m13"] = "application/x-msmediaview", ["m14"] = "application/x-msmediaview", ["m3u"] = "audio/x-mpegurl", ["man"] = "application/x-troff-man", ["mdb"] = "application/x-msaccess", ["me"] = "application/x-troff-me", ["mht"] = "message/rfc822", ["mhtml"] = "message/rfc822", ["mid"] = "audio/mid", ["mny"] = "application/x-msmoney", ["mov"] = "video/quicktime", ["movie"] = "video/x-sgi-movie", ["mp2"] = "video/mpeg", ["mp3"] = "audio/mpeg", ["mpa"] = "video/mpeg", ["mpe"] = "video/mpeg", ["mpeg"] = "video/mpeg", ["mpg"] = "video/mpeg", ["mpp"] = "application/vnd.ms-project", ["mpv2"] = "video/mpeg", ["ms"] = "application/x-troff-ms", ["mvb"] = "application/x-msmediaview", ["nws"] = "message/rfc822", ["oda"] = "application/oda", ["p10"] = "application/pkcs10", ["p12"] = "application/x-pkcs12", ["p7b"] = "application/x-pkcs7-certificates", ["p7c"] = "application/x-pkcs7-mime", ["p7m"] = "application/x-pkcs7-mime", ["p7r"] = "application/x-pkcs7-certreqresp", ["p7s"] = "application/x-pkcs7-signature", ["pbm"] = "image/x-portable-bitmap", ["pdf"] = "application/pdf", ["pfx"] = "application/x-pkcs12", ["pgm"] = "image/x-portable-graymap", ["pko"] = "application/ynd.ms-pkipko", ["pma"] = "application/x-perfmon", ["pmc"] = "application/x-perfmon", ["pml"] = "application/x-perfmon", ["pmr"] = "application/x-perfmon", ["pmw"] = "application/x-perfmon", ["pnm"] = "image/x-portable-anymap", ["pot,"] = "application/vnd.ms-powerpoint", ["ppm"] = "image/x-portable-pixmap", ["pps"] = "application/vnd.ms-powerpoint", ["ppt"] = "application/vnd.ms-powerpoint", ["prf"] = "application/pics-rules", ["ps"] = "application/postscript", ["pub"] = "application/x-mspublisher", ["qt"] = "video/quicktime", ["ra"] = "audio/x-pn-realaudio", ["ram"] = "audio/x-pn-realaudio", ["ras"] = "image/x-cmu-raster", ["rgb"] = "image/x-rgb", ["rmi"] = "audio/mid", ["roff"] = "application/x-troff", ["rtf"] = "application/rtf", ["rtx"] = "text/richtext", ["scd"] = "application/x-msschedule", ["sct"] = "text/scriptlet", ["setpay"] = "application/set-payment-initiation", ["setreg"] = "application/set-registration-initiation", ["sh"] = "application/x-sh", ["shar"] = "application/x-shar", ["sit"] = "application/x-stuffit", ["snd"] = "audio/basic", ["spc"] = "application/x-pkcs7-certificates", ["spl"] = "application/futuresplash", ["src"] = "application/x-wais-source", ["sst"] = "application/vnd.ms-pkicertstore", ["stl"] = "application/vnd.ms-pkistl", ["stm"] = "text/html", ["svg"] = "image/svg+xml", ["sv4cpio"] = "application/x-sv4cpio", ["sv4crc"] = "application/x-sv4crc", ["swf"] = "application/x-shockwave-flash", ["t"] = "application/x-troff", ["tar"] = "application/x-tar", ["tcl"] = "application/x-tcl", ["tex"] = "application/x-tex", ["texi"] = "application/x-texinfo", ["texinfo"] = "application/x-texinfo", ["tgz"] = "application/x-compressed", ["tif"] = "image/tiff", ["tiff"] = "image/tiff", ["tr"] = "application/x-troff", ["trm"] = "application/x-msterminal", ["tsv"] = "text/tab-separated-values", ["txt"] = "text/plain", ["uls"] = "text/iuls", ["ustar"] = "application/x-ustar", ["vcf"] = "text/x-vcard", ["vrml"] = "x-world/x-vrml", ["wav"] = "audio/x-wav", ["wcm"] = "application/vnd.ms-works", ["wdb"] = "application/vnd.ms-works", ["wks"] = "application/vnd.ms-works", ["wmf"] = "application/x-msmetafile", ["wps"] = "application/vnd.ms-works", ["wri"] = "application/x-mswrite", ["wrl"] = "x-world/x-vrml", ["wrz"] = "x-world/x-vrml", ["xaf"] = "x-world/x-vrml", ["xbm"] = "image/x-xbitmap", ["xla"] = "application/vnd.ms-excel", ["xlc"] = "application/vnd.ms-excel", ["xlm"] = "application/vnd.ms-excel", ["xls"] = "application/vnd.ms-excel", ["xlt"] = "application/vnd.ms-excel", ["xlw"] = "application/vnd.ms-excel", ["xof"] = "x-world/x-vrml", ["xpm"] = "image/x-xpixmap", ["xwd"] = "image/x-xwindowdump", ["z"] = "application/x-compress", ["zip"] = "application/zip" };
//		public static string _get_content_type (string _ext) {
//			return (s_plain.ContainsKey (_ext) ? s_plain [_ext] : "application/octet-stream");
//		}
//	}
//}
