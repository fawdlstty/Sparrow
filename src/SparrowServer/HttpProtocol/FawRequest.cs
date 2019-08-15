using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;

namespace SparrowServer.HttpProtocol {
	public class FawRequest {
		public string m_version = "";
		public string m_ip = "";
		public string m_agent_ip = "";
		public string m_option = "";
		public string m_url = "";
		public string m_path = "";
		public Dictionary<string, string> m_headers = new Dictionary<string, string> (StringComparer.OrdinalIgnoreCase);
		public Dictionary<string, string> m_gets = new Dictionary<string, string> ();
		public Dictionary<string, string> m_posts = new Dictionary<string, string> ();
		public Dictionary<string, (string, byte [])> m_files = new Dictionary<string, (string, byte [])> ();

		// Data check
		public Func<string, int, bool, bool> _check_int { get; set; } = null;
		public Func<string, long, bool, bool> _check_long { get; set; } = null;
		public Func<string, string, bool, bool> _check_string { get; set; } = null;

		public T get_value<T> (string _varname, bool _force_valid = true) {
			return (T) get_type_value (typeof (T), _varname, _force_valid);
		}
		public object get_type_value (Type _type, string _varname, bool _force_valid = true) {
			string _var_str = (m_posts.ContainsKey (_varname) ? m_posts [_varname] : (m_gets.ContainsKey (_varname) ? m_gets [_varname] : ""));
			if (_type == typeof (int)) {
				int _var_int = _var_str.to_int ();
				if (_check_int != null && !_check_int (_varname, _var_int, _force_valid))
					throw new Exception ($"Parameter {_varname} format error");
				return (object) _var_int;
			} else if (_type == typeof (long)) {
				var _var_long = _var_str.to_long ();
				if (_check_int != null && !_check_long (_varname, _var_long, _force_valid))
					throw new Exception ($"Parameter {_varname} format error");
				return (object) _var_long;
			} else if (_type == typeof (string)) {
				if (_check_string != null && !_check_string (_varname, _var_str, _force_valid))
					throw new Exception ($"Parameter {_varname} format error");
				return (object) _var_str;
			} else if (_type == typeof (bool)) {
				return (object) _var_str.to_bool ();
			} else if (_type == typeof (short)) {
				return (object) _var_str.to_short ();
			} else if (_type == typeof (double)) {
				return (object) _var_str.to_double ();
			} else if (_type == typeof (DateTime)) {
				return (object) _var_str.to_datetime ();
			} else {
				return JsonConvert.DeserializeObject (_var_str, _type);
				//var _method = typeof (JsonConvert).GetMethod ("DeserializeObject").MakeGenericMethod (_type);
				//return _method.Invoke (null, new object [] { _var_str });
			}
		}

		public void parse (Stream _req_stream, string _src_ip) {
			int _header_max = 100 * 1024;
			var _header_line = _read_line (_req_stream, ref _header_max).split (true, ' ');
			if (_header_line.Length < 3)
				throw new MyHttpException (400);
			_header_line [0] = _header_line [0].ToUpper ();
			if (m_options.IndexOf (_header_line [0]) < 0)
				throw new MyHttpException (405);
			m_version = _header_line [2].ToUpper ();
			if (!m_version.left_is ("HTTP/1"))
				throw new MyHttpException (505);
			m_option = _header_line [0];
			m_url = _header_line [1];
			m_path = m_url.simplify_path ();
			int _p = m_path.IndexOfAny (new char [] { '?', '#' });
			if (_p > 0)
				m_path = m_path.Substring (0, _p);
			//
			foreach (var _get_group in m_url.mid ("?", "#").split (true, '&')) {
				var (_key, _val) = _get_group.split2 ('=');
				if (_key.is_null ())
					continue;
				m_gets.TryAdd (HttpUtility.UrlDecode (_key), HttpUtility.UrlDecode (_val));
			}
			while (true) {
				string _header_group = _read_line (_req_stream, ref _header_max);
				if (_header_group.is_null ())
					break;
				var (_key, _val) = _header_group.split2 (':');
				m_headers.TryAdd (HttpUtility.UrlDecode (_key).Trim (), HttpUtility.UrlDecode (_val).Trim ());
			}
			if (m_headers.ContainsKey ("Content-Length")) {
				int _cnt_len = m_headers ["Content-Length"].to_int ();
				if (_cnt_len > 5 * 1024 * 1024)
					throw new MyHttpException (413);
				var _cnt_data = new List<byte> ();
				while (_cnt_len-- > 0) {
					int _byte = _req_stream.ReadByte ();
					if (_byte == -1)
						break;
					_cnt_data.Add ((byte) _byte);
				}
				if (m_headers.ContainsKey ("Content-Encoding")) {
					var _cnt_encoding = m_headers ["Content-Encoding"].ToLower ();
					if (_cnt_encoding == "gzip") {
						_cnt_data = _cnt_data.gzip_decompress (5 * 1024 * 1024);
					} else if (_cnt_encoding == "deflate") {
						_cnt_data = _cnt_data.deflate_decompress (5 * 1024 * 1024);
					}
				}
				string _content_type = m_headers.ContainsKey ("Content-Type") ? m_headers ["Content-Type"] : "";
				if (_content_type.left_is ("multipart/form-data;")) {
					string [] values = _content_type.Split (';').Skip (1).ToArray ();
					string boundary = string.Join (";", values).Replace ("boundary=", "").Trim ();
					byte [] bytes_boundary = Encoding.UTF8.GetBytes ($"--{boundary}");
					//
					if (!_left_is (_cnt_data, bytes_boundary))
						throw new MyHttpException (400);
					_cnt_data.RemoveRange (0, bytes_boundary.Length);
					//
					byte [] _end_line = null, _end_line2 = null;
					if (_left_is (_cnt_data, "\r\n".to_bytes ())) {
						_end_line = "\r\n".to_bytes ();
						_end_line2 = "\r\n\r\n".to_bytes ();
					} else if (_left_is (_cnt_data, "\n".to_bytes ())) {
						_end_line = "\n".to_bytes ();
						_end_line2 = "\n\n".to_bytes ();
					} else if (!_left_is (_cnt_data, "--".to_bytes ())) {
						throw new MyHttpException (400);
					}
					if (_end_line != null) {
						while (true) {
							if (_cnt_data.Count < 5 || _left_is (_cnt_data, "--".to_bytes ()))
								break;
							_cnt_data.RemoveRange (0, _end_line.Length);
							_p = _find (_cnt_data, _end_line);
							string _tmp = _left (_cnt_data, _p).to_str ();
							if (!_tmp.left_is_nocase ("Content-Disposition:"))
								throw new MyHttpException (400);
							string _name = _tmp.mid ("name=\"", "\"");
							string _filename = _tmp.mid ("filename=\"", "\"");
							if ((_p = _find (_cnt_data, _end_line2)) < 0)
								throw new MyHttpException (400);
							_cnt_data.RemoveRange (0, _p + _end_line2.Length);
							_p = _find (_cnt_data, bytes_boundary);
							if (_p - _end_line.Length < 0)
								throw new MyHttpException (400);
							byte [] _value = _left (_cnt_data, _p - _end_line.Length);
							_cnt_data.RemoveRange (0, _p + bytes_boundary.Length);
							if (_filename.is_null ()) {
								m_posts [_name] = Encoding.UTF8.GetString (_value);
							} else {
								m_files [_name] = (_filename, _value);
							}
						}
					}
				} else {
					string post_data = _cnt_data.to_str ();
					if (post_data [0] == '{') {
						JObject obj = JObject.Parse (post_data);
						foreach (var (key, val) in obj)
							m_posts [HttpUtility.UrlDecode (key)] = HttpUtility.UrlDecode (val.ToString ());
					} else {
						string [] pairs = post_data.Split (new char [] { '&' }, StringSplitOptions.RemoveEmptyEntries);
						foreach (string pair in pairs) {
							int p = pair.IndexOf ('=');
							if (p > 0)
								m_posts [HttpUtility.UrlDecode (pair.Substring (0, p))] = HttpUtility.UrlDecode (pair.Substring (p + 1));
						}
					}
				}
			}
			//
			m_ip = _src_ip;
			if (m_headers.ContainsKey ("X-Real-IP")) {
				m_agent_ip = _src_ip;
				m_ip = m_headers ["X-Real-IP"];
			} else {
				m_agent_ip = "";
				m_ip = _src_ip;
			}
		}

		// 读取一行
		private static string _read_line (Stream _req_stream, ref int _header_max) {
			var _bytes = new List<byte> ();
			int _max_len = 1024;
			while (--_max_len >= 0) {
				int _ch = _req_stream.ReadByte ();
				if (_ch == -1) {
					return _bytes.to_str ();
				} else if (--_header_max < 0) {
					throw new MyHttpException (413);
				} else if (_ch == 0x0a) {
					if (_bytes.Count > 0 && _bytes [_bytes.Count - 1] == 0x0d)
						_bytes.RemoveAt (_bytes.Count - 1);
					return _bytes.to_str ();
				}
				_bytes.Add ((byte) _ch);
			}
			throw new MyHttpException (414);
		}

		// 判断字符数组是否以什么开始
		private static bool _left_is (List<byte> l, byte [] a) {
			if (l.Count < a.Length)
				return false;
			for (int i = 0; i < a.Length; ++i) {
				if (l [i] != a [i])
					return false;
			}
			return true;
		}

		// 从字符数组中截取字符串
		private static byte [] _left (List<byte> l, int n) {
			var _arr = new byte [Math.Min (n, l.Count)];
			for (int i = 0; i < _arr.Length; ++i)
				_arr [i] = l [i];
			return _arr;
		}

		// 从字符数组中寻找目标字符集
		private static int _find (List<byte> l, byte [] a) {
			for (int i = 0; i < l.Count - a.Length + 1; ++i) {
				int j = 0;
				for (; j < a.Length; ++j) {
					if (l [i + j] != a [j])
						break;
				}
				if (j == a.Length)
					return i;
			}
			return -1;
		}

		private static List<string> m_options = new List<string> { "HEAD", "GET", "PUT", "POST", "DELETE" /*, "OPTIONS", "TRACE", "CONNECT"*/ };
	}
}
