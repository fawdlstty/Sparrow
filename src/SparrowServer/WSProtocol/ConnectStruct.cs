using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace SparrowServer.WSProtocol {
	internal class ConnectStruct {
		public ConnectStruct (string _name, Type _observer_type) {
			m_name = _name;
			m_observer_type = _observer_type;
			if (!_observer_type.IsSubclassOf (typeof (WSObserver)))
				throw new Exception ($"{m_name}: [WSModule] class must inherits from WSObserver");
		}

		public void add_pure_auth_method (MethodInfo _pure_auth_method) {
			if (m_pure_auth_method != null)
				throw new Exception ($"{m_name}.{_pure_auth_method.Name}: pure auth is already exists");
			if (!_pure_auth_method.IsStatic)
				throw new Exception ($"{m_name}.{_pure_auth_method.Name}: pure auth method must is static");
			if (_pure_auth_method.ReturnType != m_observer_type)
				throw new Exception ($"{m_name}.{_pure_auth_method.Name}: pure auth must return the object of the module in which it resides");
			if ((_pure_auth_method.GetParameters ()?.Length ?? 0) > 0)
				throw new Exception ($"{m_name}.{_pure_auth_method.Name}: pure auth must has no argument");
			m_pure_auth_method = _pure_auth_method;
		}

		public void add_auth_method (MethodInfo _auth_method) {
			if (m_auth_method != null)
				throw new Exception ($"{m_name}.{_auth_method.Name}: jwt auth is already exists");
			if (!m_auth_method.IsStatic)
				throw new Exception ($"{m_name}.{_auth_method.Name}: jwt auth method must is static");
			if (m_auth_method.ReturnType != m_observer_type)
				throw new Exception ($"{m_name}.{_auth_method.Name}: jwt auth must return the object of the module in which it resides");
			var _params = m_auth_method.GetParameters ();
			if ((_params?.Length ?? 0) != 1 || _params [0].ParameterType != typeof (JObject))
				throw new Exception ($"{m_name}.{_auth_method.Name}: jwt auth must has 1 argument and type is JObject");
			m_auth_method = _auth_method;
		}

		public WSObserver get_pure_connection () {
			if (m_pure_auth_method == null)
				throw new MyHttpException (403);
			return m_pure_auth_method.Invoke (null, new object [] {}) as WSObserver;
		}

		public WSObserver get_jwt_connection (string _api_key) {
			if (m_auth_method == null)
				throw new MyHttpException (403);
			var _jwt_o = JWTManager.Check (_api_key);
			return m_auth_method.Invoke (null, new object [] { _jwt_o }) as WSObserver;
		}

		public void add_ws_method (MethodInfo _ws_method) {
			if (m_ws_methods.ContainsKey (_ws_method.Name))
				throw new Exception ($"{m_name}.{_ws_method.Name}: ws method is already exist");
			if (_ws_method.IsStatic)
				throw new Exception ($"{m_name}.{_ws_method.Name}: ws method must not static");
			m_ws_methods.Add (_ws_method.Name, _ws_method);
		}

		private string m_name = "";
		private Type m_observer_type = null;
		private MethodInfo m_pure_auth_method = null;
		private MethodInfo m_auth_method = null;
		private Dictionary<string, MethodInfo> m_ws_methods = new Dictionary<string, MethodInfo> ();
	}
}
