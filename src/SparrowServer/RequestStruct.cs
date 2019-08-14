using SparrowServer.Attributes;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SparrowServer.HttpProtocol;

namespace SparrowServer {
	class RequestStruct {
		public RequestStruct (MethodInfo _method, MethodInfo _auth_method, string _jwt_type) {
			m_method = _method;
			m_auth_method = _auth_method;
			m_jwt_type = _jwt_type;
			if (m_jwt_type == "Gen") {
				(JObject, DateTime) _jwt_gen_ret = (null, DateTime.Now);
				if (_jwt_gen_ret.GetType () != _method.ReturnType)
					throw new Exception ("[JWTGen] method return type must be (JObject, DateTime)");
			}
			foreach (var _param in m_method.GetParameters ()) {
				if (_param.ParameterType == typeof (FawRequest)) {
					m_params.Add ((null, "FawRequest"));
				} else if (_param.ParameterType == typeof (FawResponse)) {
					m_params.Add ((null, "FawResponse"));
				} else {
					var _attrs = (from p in _param.GetCustomAttributes () where p is IReqParam select p as IReqParam);
					if (_attrs.Count () > 0) {
						m_params.Add ((null, _attrs.First ().Name));
					} else{
						m_params.Add ((_param.ParameterType, _param.Name));
					}
				}
			}
		}

		public void process (FawRequest _req, FawResponse _res) {
			try {
				object _obj = null;
				if (!m_method.IsStatic) {
					var _jwt_obj = JWTManager.Check (_req.m_headers ["X-API-Key"]);
					_obj = m_auth_method.Invoke (null, new object [] { _jwt_obj });
				}
				//
				var _params = new object [m_params.Count];
				bool _ignore_return = false;
				for (int i = 0; i < m_params.Count; ++i) {
					if (m_params [i].Item1 != null) {
						_params [i] = _req.get_type_value (m_params [i].Item1, m_params [i].Item2);
					} else {
						switch (m_params [i].Item2) {
							case "FawRequest":
								_params [i] = _req;
								break;
							case "FawResponse":
								_params [i] = _res;
								_ignore_return = true;
								break;
							case ":IP":
								_params [i] = _req.m_ip;
								break;
							case ":AgentIP":
								_params [i] = _req.m_agent_ip;
								break;
							case ":Option":
								_params [i] = _req.m_option;
								break;
							default:
								throw new Exception ("Request parameter types that are not currently supported");
						}
					}
				}
				var _ret = m_method.Invoke (_obj, _params);
				//if (_ret.GetType () == typeof (Task<>)) // 始终为False
				if (_ret is Task _t) {
					if (_ret.GetType () != typeof (Task)) {
						_ret = _ret.GetType ().InvokeMember ("Result", BindingFlags.GetProperty, null, _ret, null);
					} else {
						_t.Wait ();
						_ret = null;
					}
				}
				if (m_jwt_type == "Gen") {
					var (_o, _exp) = ((JObject, DateTime)) _ret;
					_ret = JWTManager.Generate (_o, _exp);
				}
				if (!_ignore_return) {
					if (_ret is byte _byte) {
						_res.write (_byte);
					} else if (_ret is byte [] _bytes) {
						_res.write (_bytes);
					} else {
						string _content = (_ret.GetType ().IsPrimitive ? _ret.to_str () : _ret.to_json ());
						object _o;
						if (_content == "") {
							_o = new { result = "success" };
						} else if (_content[0] != '[' && _content[0] != '{') {
							_o = new { result = "success", content = _content };
						} else {
							_o = new { result = "success", content = JToken.Parse (_content) };
						}
						_res.write (_o.to_json ());
					}
				}
			} catch (TargetInvocationException ex) {
				throw ex.InnerException;
			}
		}

		private MethodInfo m_method = null, m_auth_method = null;
		private List<(Type, string)> m_params = new List<(Type, string)> ();
		private string m_jwt_type = "";
	}
}
