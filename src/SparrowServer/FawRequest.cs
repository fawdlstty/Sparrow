using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace SparrowServer {
	public class FawRequest {
		public string m_ip = "";
		public string m_agent_ip = "";
		public string m_method = "";
		public string m_url = "";
		public string m_path = "";
		public Dictionary<string, string> m_gets = new Dictionary<string, string> ();
		public Dictionary<string, string> m_posts = new Dictionary<string, string> ();
		public Dictionary<string, (string, byte [])> m_files = new Dictionary<string, (string, byte [])> ();

		// Data check / 数据检查
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
					throw new Exception ($"Parameter {_varname} format error / 参数 {_varname} 格式错误");
				return (object) _var_int;
			} else if (_type == typeof (long)) {
				var _var_long = _var_str.to_long ();
				if (_check_int != null && !_check_long (_varname, _var_long, _force_valid))
					throw new Exception ($"Parameter {_varname} format error / 参数 {_varname} 格式错误");
				return (object) _var_long;
			} else if (_type == typeof (string)) {
				if (_check_string != null && !_check_string (_varname, _var_str, _force_valid))
					throw new Exception ($"Parameter {_varname} format error / 参数 {_varname} 格式错误");
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
	}
}
