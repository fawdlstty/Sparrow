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
using SparrowServer.WSProtocol;

namespace SparrowServer {
	public class FawHttpServer {
		public FawHttpServer (Assembly assembly, string jwt_secret) {
			m_assembly = assembly;
			JWTManager.m_secret = jwt_secret;
			//https://www.w3cschool.cn/swaggerbootstrapui/swaggerbootstrapui-31rf32is.html
		}

		public void set_keep_alive_ms (int _http = 10000, int _websocket = 66000) {
			m_alive_http_ms = _http;
			m_alive_websocket_ms = _websocket;
		}

		public void set_api_path (string _api_path = "/api") {
			m_api_path = $"{_api_path}/";
		}

		public void set_res_from_path (string _path = "D:/wwwroot") {
			_path = _path.Replace ('\\', '/');
			m_res_path = (_path.right_is ("/") ? _path : $"{_path}/");
			m_res_namespace = "";
		}

		public void set_res_from_namespace (string _namespace = "TestServer.res") {
			m_res_path = "";
			m_res_namespace = (_namespace.right_is (".") ? _namespace : $"{_namespace}.");
		}

		public void set_log_path (string _log_path = "D:/sparrow/log") {
			_log_path = _log_path.Replace ('\\', '/');
			Log.m_path = (_log_path.right_is ("/") ? _log_path : $"{_log_path}/");
		}

		public void set_ssl_file (string _pfx_file, string _pwd = "") {
			m_pfx = _pfx_file.is_null () ? null : (_pwd.is_null () ? new X509Certificate (_pfx_file) : new X509Certificate (_pfx_file, _pwd));
		}

		public void set_doc_info (string _doc_path = "/swagger", WEBDocInfo doc_info = null) {
			m_doc_path = $"{_doc_path}/";
			m_doc_info = doc_info;
		}

		public void enable_monitor (bool _enable = true) {
			if (_enable != (m_monitor != null)) {
				m_monitor = (_enable ? new MonitoringManager () : null);
			}
		}



		// processing a request
		private (bool, string, string) _request_http_once (Stream _req_stream, string _src_ip, CancellationToken _token, byte _first_byte) {
			bool _static = true, _error = false;
			string _ws_module = "";
			var _request_begin = DateTime.Now;

			FawRequest _req = new FawRequest ();
			FawResponse _res = new FawResponse ();
			try {
				_req.deserialize (_req_stream, _src_ip, _token, _first_byte);
				_req._check_int = m_check_int;
				_req._check_long = m_check_long;
				_req._check_string = m_check_string;

				if (_req.m_option == "HEAD") {
					throw new MyHttpException (204);
				} else if (_req.get_header ("Upgrade").ToLower ().IndexOf ("websocket") >= 0) {
					if (_req.get_header ("Connection").ToLower () != "upgrade")
						throw new MyHttpException (502);
					var _ws_types = _req.get_header ("Sec-WebSocket-Protocol").ToLower ().Replace (" ", "").split_list (true, ',');
					if (_ws_types.IndexOf ("chat") == -1)
						throw new MyHttpException (501);
					if (_req.get_header ("Sec-WebSocket-Version") != "13")
						throw new MyHttpException (501);
					_ws_module = _req.m_path;
					_res.m_status_code = 101;
					_res.m_headers ["Sec-WebSocket-Accept"] = $"{_req.get_header ("Sec-WebSocket-Key")}258EAFA5-E914-47DA-95CA-C5AB0DC85B11".to_bytes ().sha1_encode ().base64_encode ();
					_res.m_headers ["Sec-WebSocket-Version"] = "13";
					//_res.m_headers ["Origin"] = "";
					//_res.m_headers ["Sec-WebSocket-Protocol"] = "chat.";
					//_res.m_headers ["Sec-WebSocket-Extensions"] = "";
					// TODO: check and reply
				} else if (!m_api_path.is_null () && _req.m_path.left_is (m_api_path.mid (1))) {
					_static = false;
					if (m_api_handlers.ContainsKey ($"{_req.m_option} /{_req.m_path}")) {
						m_api_handlers [$"{_req.m_option} /{_req.m_path}"].process (_req, _res);
					} else if (m_api_handlers.ContainsKey ($" /{_req.m_path}")) {
						m_api_handlers [$" /{_req.m_path}"].process (_req, _res);
					} else {
						throw new Exception ("Unknown request");
					}
				} else if (m_doc_info != null && !m_doc_path.is_null () && _req.m_path.left_is (m_doc_path.mid (1))) {
					if (_req.m_path == $"{m_doc_path.mid (1)}api.json") {
						string _host = (_req.get_header ("Host").is_null () ? $"127.0.0.1:{m_port}" : _req.get_header ("Host"));
						_res.write (m_swagger_data.Replace ("%-faq-host-%", _host));
						_res.set_content_from_filename (_req.m_path);
					} else if (m_doc_path != "/swagger/" && (_req.m_path == (m_doc_path.mid (1)) || _req.m_path.mid (m_doc_path.Length - 1) == "index.html")) {
						var _namespace = $"{MethodBase.GetCurrentMethod ().DeclaringType.Namespace}.Swagger.res.{_req.m_path.mid (m_doc_path.mid (1))}";
						_namespace = (_namespace.right_is (".") ? $"{_namespace}index.html" : _namespace);
						string _data = _read_from_namespace (_namespace).to_str ();
						_res.write (_data.Replace ("/swagger/", m_doc_path));
						_res.set_content_from_filename (_namespace);
					} else {
						var _namespace = $"{MethodBase.GetCurrentMethod ().DeclaringType.Namespace}.Swagger.res.{_req.m_path.mid (m_doc_path.mid (1))}";
						_res.write (_read_from_namespace (_namespace));
						_res.set_content_from_filename (_namespace);
					}
					//} else if (_req.m_path.left_is ("monitor/") && m_monitor != null) {
					//	if (_req.m_path == "monitor/data.json") {
					//		_res.write (m_monitor.get_json (_req.get_value<int> ("count", false)));
					//		//_res.set_content_from_filename (_req.m_path);
					//}
				} else if (_req.m_option == "GET") {
					byte [] _data = _load_res (_req.m_path);
					if (_data != null) {
						_res.write (_data);
						_res.set_content_from_filename (_req.m_path);
					} else {
						throw new MyHttpException (404);
					}
				} else {
					throw new MyHttpException (404);
				}
			} catch (MyHttpException ex) {
				if (ex.m_error_num == 404) {
					if (_req.m_path == "404.html") {
						_res.m_status_code = ex.m_error_num;
						_error = (_res.m_status_code / 100 != 2);
					} else {
						_res.redirect ("/404.html");
					}
				} else {
					_res.m_status_code = ex.m_error_num;
					_error = (_res.m_status_code / 100 != 2);
				}
			} catch (Exception ex) {
				_res.clear ();
				_res.write (new { result = "failure", content = (ex.GetType ().Name == "Exception" ? ex.Message : ex.ToString ()) }.to_json ());
				_res.m_status_code = 500;
				_error = true;
			}
			bool _go_ws = (!_ws_module.is_null ()) && (!_error);
			_res.m_headers ["Connection"] = "Keep-Alive";
			_res.m_headers ["Keep-Alive"] = $"timeout={(_go_ws ? 66 : 10)}, max=1000";
			_req_stream.Write (_res.build_response (_req));
			m_monitor?.OnRequest (_static, (long) ((DateTime.Now - _request_begin).TotalMilliseconds + 0.5000001), _error);
			return (_go_ws, _ws_module, _req.get_header ("Sec-WebSocket-Key"));
		}

		// loop processing
		public void run (ushort port) {
			m_port = port;
			Swagger.DocBuilder _builder = (m_doc_info != null ? new Swagger.DocBuilder (m_doc_info, (m_pfx == null ? "http" : "https")) : null);
			foreach (var _module in m_assembly.GetTypes ()) {
				var _http_module_attr = _module.GetCustomAttribute (typeof (HTTPModuleAttribute), true) as HTTPModuleAttribute;
				if (_http_module_attr != null) {
					string _module_prefix = (_module.Name.right_is_nocase ("Module") ? _module.Name.left (_module.Name.Length - 6) : _module.Name);
					_builder?.add_module (_module.Name, _module_prefix, _http_module_attr.m_description);
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
							throw new Exception ("Simultaneous use of multiple JWT Attribute is not supported");
						}
						var _jwt_type = (_jwt_attrs.Count () > 0 ? _jwt_attrs.First ().Type : "");
						//
						if (_method_attrs.Count () + _jwt_attrs.Count () >= 2)
							throw new Exception ("Simultaneous use of multiple between JWT and WS Attribute is not supported");
						//
						//
						var _params = _method.GetParameters ();
						if (_jwt_type == "Connect") {
							if (_method.ReturnType != _module)
								throw new Exception ("Return value in [JWTConnect] method must be current class type");
							if (!_method.IsStatic)
								throw new Exception ("[JWTConnect] method must be static");
							if (_params.Length != 1 || _params [0].ParameterType != typeof (JObject))
								throw new Exception ("[JWTConnect] can only have one parameter, and the type of parameter is JObject");
							if (_jwt_reconnect_func != null)
								throw new Exception ("[JWTConnect] cannot appear twice in the same module");
							_jwt_reconnect_func = _method;
						} else if (_method_attr != null) {
							if (!_method.IsStatic && _jwt_reconnect_func == null)
								throw new Exception ("A module that has a non-static HTTP method must contain the [JWTConnect] method");
							_builder?.add_method (_module.Name, _method_attr.Type, _method.Name, !_method.IsStatic, _method_attr.Summary, _method_attr.Description);
							//
							string _path_prefix = $"{m_api_path}{_module_prefix}/{_method.Name}";
							foreach (var _param in _params) {
								if (_param.ParameterType == typeof (FawRequest) || _param.ParameterType == typeof (FawResponse))
									continue;
								if (((from p in _param.GetCustomAttributes () where p is IReqParam select 1).Count ()) > 0)
									continue;
								var _param_desps = (from p in _param.GetCustomAttributes () where p is ParamAttribute select (p as ParamAttribute).m_description);
								var _param_desp = (_param_desps.Count () > 0 ? _param_desps.First () : "");
								_builder?.add_param (_module.Name, _method_attr.Type, _method.Name, _param.Name, _param.ParameterType.Name, _param_desp);
							}
							_path_prefix = $"{_method_attr.Type} {_path_prefix}";
							if (m_api_handlers.ContainsKey (_path_prefix))
								throw new Exception ("Url request address prefix conflict");
							m_api_handlers.Add (_path_prefix, new RequestStruct ($"{_module.Name}.{_method.Name}", _method, _jwt_reconnect_func, _jwt_type));
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
				var _ws_module_attr = _module.GetCustomAttribute (typeof (WSModuleAttribute), true) as WSModuleAttribute;
				if (_ws_module_attr != null) {
					string _module_prefix = (_module.Name.right_is_nocase ("Module") ? _module.Name.left (_module.Name.Length - 6) : _module.Name);
					_builder?.add_module ($"{_module.Name}_ws", _module_prefix, _ws_module_attr.m_description);
					//
					ConnectStruct _connect = new ConnectStruct (_module.Name, _module);
					foreach (var _method in _module.GetMethods ()) {
						var _method_attrs = (from p in _method.GetCustomAttributes () where p is WSMethodAttribute select p as WSMethodAttribute);
						if (_method_attrs.Count () > 1) {
							throw new Exception ("Simultaneous use of multiple WSMethod Attribute is not supported");
						}
						var _method_attr = (_method_attrs.Count () > 0 ? _method_attrs.First () : null);
						//
						var _jwt_attrs = (from p in _method.GetCustomAttributes () where p is IJWTMethod select p as IJWTMethod);
						if (_jwt_attrs.Count () > 1) {
							throw new Exception ("Simultaneous use of multiple JWT Attribute is not supported");
						}
						var _jwt_type = (_jwt_attrs.Count () > 0 ? _jwt_attrs.First ().Type : "");
						//
						if (_method_attrs.Count () + _jwt_attrs.Count () >= 2)
							throw new Exception ("Simultaneous use of multiple between JWT and WS Attribute is not supported");
						//
						//
						if (_jwt_type == "PureConnect") {
							_connect.add_pure_auth_method (_method);
						} else if (_jwt_type == "Connect") {
							_connect.add_auth_method (_method);
						} else if (_method_attrs.Count () > 0) {
							_connect.add_ws_method (_method);
						}
					}
					if (m_ws_handlers.ContainsKey (_module.Name))
						throw new Exception ($"{_module_prefix}: Websocket url request address is conflict");
					m_ws_handlers.Add (_module_prefix, _connect);
				}
			}
			m_swagger_data = _builder?.build ();
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
					//Console.WriteLine ($"accept {_tmp_client.Client.Handle}");
					// Task.Factory.StartNew  ThreadPool.QueueUserWorkItem
					ThreadPool.QueueUserWorkItem ((_client_o) => {
						//Console.WriteLine ("conn start");
						_loop_process (_client_o as TcpClient);
						//Console.WriteLine ("conn stop");
					}, _tmp_client);
				}
			} catch (Exception ex) {
				Log.show_error (ex);
			}
		}

		private void _loop_process (TcpClient _client) {
			try {
				using (_client) {
					var _src_ip = _client.Client.RemoteEndPoint.to_str ().split2 (':').Item1;
					var _net_stream = _client.GetStream ();
					_net_stream.ReadTimeout = _net_stream.WriteTimeout = m_alive_http_ms;
					if (m_pfx != null) {
						using (var _ssl_stream = new SslStream (_net_stream)) {
							_ssl_stream.AuthenticateAsServer (m_pfx, false, SslProtocols.Tls, true);
							_ssl_stream.ReadTimeout = _ssl_stream.WriteTimeout = m_alive_http_ms;
							_loop_process_http (_net_stream, _ssl_stream, _src_ip);
						}
					} else {
						_loop_process_http (_net_stream, null, _src_ip);
					}
				}
			} catch (TaskCanceledException) {
			} catch (IOException) {
			} catch (Exception ex) {
				Console.Write (ex.ToString ());
			}
		}

		private void _loop_process_http (NetworkStream _net_stream, SslStream _ssl_stream, string _src_ip) {
			Stream _stream = (_ssl_stream != null ? (Stream) _ssl_stream : _net_stream);
			while (true) {
				int _byte = _stream.ReadByte ();
				if (_byte == -1)
					return;
				var _source = new CancellationTokenSource (m_alive_http_ms);
				//Console.WriteLine ("_request_http_once start");
				var (_go_ws, _module, _api_key) = _request_http_once (_stream, _src_ip, _source.Token, (byte) _byte);
				//Console.WriteLine ("_request_http_once stop");
				if (_go_ws) {
					_net_stream.ReadTimeout = _net_stream.WriteTimeout = m_alive_websocket_ms;
					_ssl_stream.ReadTimeout = _ssl_stream.WriteTimeout = m_alive_websocket_ms;
					_loop_process_ws (_stream, _module, _api_key);
					break;
				}
			}
		}

		private void _loop_process_ws (Stream _stream, string _module, string _api_key) {
			var _conn_struct = m_ws_handlers [_module];
			var _observer = (_api_key.is_null () ? _conn_struct.get_pure_connection () : _conn_struct.get_jwt_connection (_api_key));
			while (true) {
				var _buf = new byte [] { 0, 0 };
				// https://www.jianshu.com/p/f666da1b1835
				CancellationTokenSource _source = new CancellationTokenSource (m_alive_websocket_ms);
				if (_stream.ReadAsync (_buf, _source.Token).Result < 2)
					break;
				_source.CancelAfter (m_alive_websocket_ms);
				bool _is_eof = (_buf [0] & 0x80) > 0;
				if ((_buf [0] & 0x70) > 0)
					throw new Exception ("RSV1~RSV3 is not 0");
				// TODO: 处理连接
			}
		}



		private Dictionary<string, RequestStruct> m_api_handlers = new Dictionary<string, RequestStruct> ();
		private Dictionary<string, ConnectStruct> m_ws_handlers = new Dictionary<string, ConnectStruct> ();

		// data check
		public Func<string, int, bool, bool> m_check_int { get; set; } = null;
		public Func<string, long, bool, bool> m_check_long { get; set; } = null;
		public Func<string, string, bool, bool> m_check_string { get; set; } = null;

		// load resource (from namespace or path)
		private byte [] _load_res (string _path_name) {
			if (!m_res_path.is_null ()) {
				string _real_path = $"{m_res_path}{_path_name}";
				if (File.Exists (_real_path))
					return File.ReadAllBytes (_real_path);
				if (_real_path.right_is ("/")) {
					if (File.Exists ($"{_real_path}index.htm"))
						return File.ReadAllBytes ($"{_real_path}index.htm");
					if (File.Exists ($"{_real_path}index.html"))
						return File.ReadAllBytes ($"{_real_path}index.html");
				}
			} else if (!m_res_namespace.is_null ()) {
				string _real_namespace = $"{m_res_namespace}{_path_name.Replace ('/', '.')}";
				byte [] _data = _read_from_namespace (_real_namespace);
				if (_data != null)
					return _data;
				if (_real_namespace.right_is (".")) {
					if ((_data = _read_from_namespace ($"{_real_namespace}index.htm")) != null)
						return _data;
					if ((_data = _read_from_namespace ($"{_real_namespace}index.html")) != null)
						return _data;
				}
			}
			throw new MyHttpException (404);
		}

		// load resource from namespace
		private byte [] _read_from_namespace (string _namespace) {
			using (var _stream = Assembly.GetCallingAssembly ().GetManifestResourceStream (_namespace)) {
				if (_stream == null)
					return null;
				var _buffer = new byte [_stream.Length];
				_stream.Read (_buffer);
				return _buffer;
			}
		}

		private ushort m_port = 80;

		private int m_alive_http_ms = 10000;
		private int m_alive_websocket_ms = 66000;

		private string m_api_path = "/api/";
		private Assembly m_assembly = null;
		private X509Certificate m_pfx = null;

		// swagger doc
		private string m_doc_path = "/swagger/";
		private WEBDocInfo m_doc_info = null;
		private string m_swagger_data = "";

		// monitor
		private MonitoringManager m_monitor = null;

		// static res
		private string m_res_path = "";
		private string m_res_namespace = "";
	}
}
