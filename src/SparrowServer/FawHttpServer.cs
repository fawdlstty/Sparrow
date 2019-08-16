using SparrowServer.Attributes;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.Reflection;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using SparrowServer.Monitor;
using SparrowServer.HttpProtocol;

namespace SparrowServer {
	public class FawHttpServer {
		//private delegate void RequestHandler (FawRequest _req, FawResponse _res);
		private Dictionary<string, RequestStruct> m_request_handlers = new Dictionary<string, RequestStruct> ();
		private void add_handler (string path_prefix, RequestStruct _req_struct) {
			foreach (var key in m_request_handlers.Keys) {
				if (key == path_prefix) // key.left_is (path_prefix) || path_prefix.left_is (key)
					throw new Exception ("Url request address prefix conflict");
			}
			m_request_handlers.Add (path_prefix, _req_struct);
		}

		// data check
		public Func<string, int, bool, bool> m_check_int { get; set; } = null;
		public Func<string, long, bool, bool> m_check_long { get; set; } = null;
		public Func<string, string, bool, bool> m_check_string { get; set; } = null;



		public FawHttpServer (Assembly assembly, string jwt_secret) {
			m_assembly = assembly;
			JWTManager.m_secret = jwt_secret;
			//https://www.w3cschool.cn/swaggerbootstrapui/swaggerbootstrapui-31rf32is.html
			string _local_path = Path.GetDirectoryName (typeof (FawHttpServer).Assembly.Location).Replace ('\\', '/');
			//
			Log.m_path = (_local_path.right_is ("/") ? _local_path : $"{_local_path}/");
			m_wwwroot = $"{Log.m_path}wwwroot/";
		}

		public void set_ssl_file (string _pfx_file, string _pwd = "") {
			m_pfx = _pfx_file.is_null () ? null : (_pwd.is_null () ? new X509Certificate (_pfx_file) : new X509Certificate (_pfx_file, _pwd));
		}

		public void set_doc_info (WEBDocInfo doc_info) {
			m_doc_info = doc_info;
		}

		public void enable_monitor (bool _enable = true) {
			if (_enable != (m_monitor != null)) {
				m_monitor = (_enable ? new MonitoringManager () : null);
			}
		}

		// processing a request
		private (bool, string) _request_http_once (Stream _req_stream, string _src_ip, int _first_byte = -1) {
			//https://referencesource.microsoft.com/#System/net/System/Net/HttpListener.cs,671908476fdfe5be
			bool _static = true, _error = false, _go_ws = false;
			var _request_begin = DateTime.Now;

			FawRequest _req = new FawRequest ();
			FawResponse _res = new FawResponse ();
			try {
				_req.parse (_req_stream, _src_ip, _first_byte);
				_req._check_int = m_check_int;
				_req._check_long = m_check_long;
				_req._check_string = m_check_string;

				if (_req.m_option == "HEAD") {
					throw new MyHttpException (204);
				} else if (_req.get_header ("Upgrade").ToLower ().IndexOf ("websocket") >= 0) {
					_go_ws = true;
					// TODO: check and reply
				} else if (_req.m_path.left_is ("/api/")) {
					_static = false;
					bool _proc = false;
					if (m_request_handlers.ContainsKey ($"{_req.m_option} {_req.m_path}")) {
						m_request_handlers [$"{_req.m_option} {_req.m_path}"].process (_req, _res);
						_proc = true;
					} else if (m_request_handlers.ContainsKey ($" {_req.m_path}")) {
						m_request_handlers [$" {_req.m_path}"].process (_req, _res);
						_proc = true;
					}
					if (!_proc)
						throw new Exception ("Unknown request");
				} else if (_req.m_path.left_is ("/swagger/") && m_doc_info != null) {
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
				} else if (_req.m_path.left_is ("/monitor/") && m_monitor != null) {
					if (_req.m_path == "/monitor/data.json") {
						_res.write (m_monitor.get_json (_req.get_value<int> ("count", false)));
						//_res.set_content_from_filename (_req.m_path);
					}
				} else if (_req.m_option == "GET") {
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
			} catch (MyHttpException ex) {
				_res.m_status_code = ex.m_error_num;
				_error = (_res.m_status_code / 100 != 2);
			} catch (Exception ex) {
				_res.clear ();
				_res.write (new { result = "failure", content = (ex.GetType ().Name == "Exception" ? ex.Message : ex.ToString ()) }.to_json ());
				_res.m_status_code = 500;
				_error = true;
			}
			_req_stream.Write (_res.build_response (_req));
			m_monitor?.OnRequest (_static, (long) ((DateTime.Now - _request_begin).TotalMilliseconds + 0.5000001), _error);
			return ((_go_ws && !_error), _req.get_header ("X-API-Key"));
		}

		// loop processing
		public void run (ushort port) {
			Swagger.DocBuilder _builder = (m_doc_info != null ? new Swagger.DocBuilder (m_doc_info, (m_pfx == null ? "http" : "https")) : null);
			foreach (var _module in m_assembly.GetTypes ()) {
				var _module_attr = _module.GetCustomAttribute (typeof (HTTPModuleAttribute), true) as HTTPModuleAttribute;
				if (_module_attr != null) {
					string _module_prefix = (_module.Name.right_is_nocase ("Module") ? _module.Name.left (_module.Name.Length - 6) : _module.Name);
					_builder?.add_module (_module.Name, _module_prefix, _module_attr.m_description);
					//
					MethodInfo _jwt_reconnect_func = null;
					Action<MethodInfo> _process_method = (_method) => {
						// process attribute
						var _method_attrs = (from p in _method.GetCustomAttributes () where p is IHTTPMethod select p as IHTTPMethod);
						if (_method_attrs.Count () > 1) {
							throw new Exception ("Simultaneous use of multiple HTTP Attribute is not supported");
						}
						var _method_attr = (_method_attrs.Count () > 0 ? _method_attrs.First () : null);
						//
						var _jwt_attrs = (from p in _method.GetCustomAttributes () where p is IJWTMethod select p as IJWTMethod);
						if (_jwt_attrs.Count () > 1) {
							throw new Exception ("Simultaneous use of multiple HTTP Attribute is not supported");
						}
						var _jwt_type = (_jwt_attrs.Count () > 0 ? _jwt_attrs.First ().Type : "");
						//
						var _params = _method.GetParameters ();
						if (_jwt_type == "Connect") {
							if (_method.ReturnType != _module)
								throw new Exception ("Return value in [JWTRequest] method must be current class type");
							if (!_method.IsStatic)
								throw new Exception ("[JWTRequest] method must be static");
							if (_params.Length != 1 || _params [0].ParameterType != typeof (JObject))
								throw new Exception ("[JWTRequest] can only have one parameter, and the type of parameter is JObject");
							if (_jwt_reconnect_func != null)
								throw new Exception ("[JWTRequest] cannot appear twice in the same module");
							_jwt_reconnect_func = _method;
						} else if (_method_attr != null) {
							if (!_method.IsStatic && _jwt_reconnect_func == null)
								throw new Exception ("A module that has a non-static HTTP method must contain the [JWTRequest] method");
							_builder?.add_method (_module.Name, _method_attr.Type, _method.Name, !_method.IsStatic, _method_attr.Summary, _method_attr.Description);
							//
							string _path_prefix = $"/api/{_module_prefix}/{_method.Name}";
							foreach (var _param in _params) {
								if (_param.ParameterType == typeof (FawRequest) || _param.ParameterType == typeof (FawResponse))
									continue;
								if (((from p in _param.GetCustomAttributes () where p is IReqParam select 1).Count ()) > 0)
									continue;
								var _param_desps = (from p in _param.GetCustomAttributes () where p is ParamAttribute select (p as ParamAttribute).m_description);
								var _param_desp = (_param_desps.Count () > 0 ? _param_desps.First () : "");
								_builder?.add_param (_module.Name, _method_attr.Type, _method.Name, _param.Name, _param.ParameterType.Name, _param_desp);
							}
							add_handler ($"{_method_attr.Type} {_path_prefix}", new RequestStruct (_method, _jwt_reconnect_func, _jwt_type));
						}
					};
					// process static method
					foreach (var _method in _module.GetMethods ()) {
						if (!_method.IsStatic)
							continue;
						_process_method (_method);
					}
					// process dynamic method
					foreach (var _method in _module.GetMethods ()) {
						if (_method.IsStatic)
							continue;
						_process_method (_method);
					}
				}
			}
			m_swagger_data = _builder?.build ().to_bytes ();
			//
			new Thread (() => {
				while (true) {
					Thread.Sleep (10000);
					GC.Collect ();
				}
			}).Start ();
			//
			try {
				var _listener = new TcpListener (IPAddress.Loopback, port);
				_listener.Start ();
				while (true) {
					var _tmp_client = _listener.AcceptTcpClient ();
					// Task.Factory.StartNew  ThreadPool.QueueUserWorkItem
					ThreadPool.QueueUserWorkItem ((_client_o) => {
						using (TcpClient _client = _client_o as TcpClient) {
							Console.WriteLine ("conn start");
							try {
								var _src_ip = _client.Client.RemoteEndPoint.to_str ().split2 (':').Item1;
								using (var _source_stream = _client.GetStream ()) {
									_source_stream.ReadTimeout = _source_stream.WriteTimeout = 66666;
									if (m_pfx != null) {
										using (var _ssl_stream = new SslStream (_source_stream)) {
											_ssl_stream.AuthenticateAsServer (m_pfx, false, SslProtocols.Tls, true);
											_ssl_stream.ReadTimeout = _ssl_stream.WriteTimeout = 66666;
											//(_static, _error) = _loop_process_http (_ssl_stream, _src_ip);
											_loop_process_http (_ssl_stream, _src_ip);
										}
									} else {
										//(_static, _error) = _loop_process_http (_source_stream, _src_ip);
										_loop_process_http (_source_stream, _src_ip);
									}
								}
							} catch (Exception) {
								//_static = false;
								//_error = true;
							}
							Console.WriteLine ("conn stop");
						}
					}, _tmp_client);
				}
			} catch (Exception ex) {
				Log.show_error (ex);
			}
		}

		private void _loop_process_http (Stream _stream, string _src_ip) {
			//int _byte;
			//while ((_byte = _stream.ReadByte ()) == -1)
			//	Thread.Sleep (10);
			while (true) {
				var _buf = new byte [1] { 0 };
				if (_stream.ReadAsync (_buf, 0, 1).Result == 0)
					throw new Exception ("no data");
				var (_go_ws, _api_key) = _request_http_once (_stream, _src_ip, _buf [0]);
				if (_go_ws) {
					_loop_process_ws (_stream, _src_ip, _api_key);
					break;
				}
			}
		}

		private void _loop_process_ws (Stream _stream, string _src_ip, string _api_key) {
			// TODO: process message
		}

		private Assembly m_assembly = null;
		private X509Certificate m_pfx = null;
		private WEBDocInfo m_doc_info = null;
		private string m_wwwroot = "";
		private byte [] m_swagger_data = null;
		private MonitoringManager m_monitor = null;
	}
}
