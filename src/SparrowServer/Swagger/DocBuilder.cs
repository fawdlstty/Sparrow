using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SparrowServer.Swagger {
	internal class _DocModule {
		public string m_name;
		public string m_prefix;
		public string m_description;
		public List<_DocMethod> m_methods = new List<_DocMethod> ();
	}

	internal class _DocMethod {
		public string m_request_type;
		public string m_name;
		public bool m_api_key;
		public string m_summary;
		public string m_description;
		public List<_DocParam> m_params = new List<_DocParam> ();
	}

	internal class _DocParam {
		public string m_name;
		public string m_type;
		public string m_description;
		public string m_in;
	}

	internal class DocBuilder {
		public DocBuilder (WEBDocInfo _doc_info, string _schema) {
			//m_obj = new JObject {
			//	["swagger"] = "2.0",
			//	["schemes"] = new JArray { _doc_info.Scheme },
			//	["host"] = _doc_info.Host,
			//	["basePath"] = "/",
			//	["info"] = new JObject {
			//		["title"] = _doc_info.DocName,
			//		["version"] = _doc_info.Version,
			//		["description"] = _doc_info.Description,
			//		//["termsOfService"] = "",
			//		["contact"] = new JObject { ["email"] = _doc_info.Contact, },
			//		//["license"] = new JObject { ["name"] = "Unlicense", ["url"] = "#", },
			//	},
			//	["securityDefinitions"] = new JObject {
			//		["APIKeyHeader"] = new JObject {
			//			["type"] = "apiKey",
			//			["in"] = "header",
			//			["name"] = "X-API-Key",
			//		}
			//	},
			//	["security"] = new JArray { new JObject { ["apiKey"] = new JArray () } },
			//	//["externalDocs"] = new JObject { ["description"] = "查看更多文档", ["url"] = "#", },
			//};
			m_obj = new JObject {
				["openapi"] = "3.0.0",
				["info"] = new JObject {
					["title"] = _doc_info.DocName,
					["description"] = _doc_info.Description,
					["version"] = _doc_info.Version,
				},
				["servers"] = new JArray { new JObject {
					["url"] = $"{_schema}://%-faq-host-%",
					["description"] = "",
				}},
				["components"] = JObject.Parse ("{\"securitySchemes\":{\"ApiKeyAuth\":{\"type\":\"apiKey\",\"in\":\"header\",\"name\":\"X-API-Key\"}}}"),
				["paths"] = new JObject (),
			};
		}

		public void add_module (string _module_name, string _module_prefix, string _description) {
			if (_module_name.is_null ())
				throw new Exception ("Name format error");
			if ((from p in m_modules where p.m_name == _module_name || p.m_prefix == _module_prefix select 1).Count () > 0)
				throw new Exception ("Repeated addition of modules");
			m_modules.Add (new _DocModule { m_name = _module_name, m_prefix = _module_prefix, m_description = _description });
		}

		public void add_method (string _module_name, string _request_type, string _method_name, bool _api_key, string _summary, string _description = "") {
			if (_module_name.is_null () || _method_name.is_null ())
				throw new Exception ("Name format error");
			if (_description.is_null ())
				_description = _summary;
			for (int i = m_modules.Count - 1; i >= 0; --i) {
				if (m_modules [i].m_name == _module_name) {
					if ((from p in m_modules [i].m_methods where p.m_request_type == _request_type && p.m_name == _method_name select 1).Count () > 0)
						throw new Exception ("Repeated addition of methods");
					m_modules [i].m_methods.Add (new _DocMethod { m_request_type = _request_type, m_name = _method_name, m_api_key = _api_key, m_summary = _summary, m_description = _description });
					return;
				}
			}
			throw new Exception ("Module is not added");
		}

		public void add_param (string _module_name, string _request_type, string _method_name, string _param_name, string _param_type, string _description = "") {
			if (_module_name.is_null () || _method_name.is_null () || _param_name.is_null ())
				throw new Exception ("Name format error");
			for (int i = m_modules.Count - 1; i >= 0; --i) {
				if (m_modules [i].m_name == _module_name) {
					for (int j = 0; j < m_modules [i].m_methods.Count; ++j) {
						if (m_modules [i].m_methods [j].m_request_type == _request_type && m_modules [i].m_methods [j].m_name == _method_name) {
							if ((from p in m_modules [i].m_methods [j].m_params where p.m_name == _param_name select 1).Count () > 0)
								throw new Exception ("Repeated addition of params");
							m_modules [i].m_methods [j].m_params.Add (new _DocParam () { m_name = _param_name, m_type = _param_type, m_description = _description, m_in = (_request_type == "POST" ? "body" : "query") });
							return;
						}
					}
				}
			}
			throw new Exception ("Module or method is not added");
		}

		public string build () {
			//var _tags = new JArray ();
			//var _paths = new JObject ();
			//foreach (var _module in m_modules) {
			//	_tags.Add (new JObject {
			//		["name"] = _module.m_name,
			//		["description"] = _module.m_description,
			//	});
			//	foreach (var _method in _module.m_methods) {
			//		var _parameters = new JArray ();
			//		foreach (var _param in _method.m_params) {
			//			_parameters.Add (new JObject {
			//				["in"] = _param.m_in,
			//				["name"] = _param.m_name,
			//				["description"] = _param.m_description,
			//				["required"] = true,
			//				["type"] = _param.m_type,
			//			});
			//		}
			//		string _key1 = $"/api/{_module.m_prefix}/{_method.m_name}", _key2 = _method.m_request_type.ToLower ();
			//		_paths [_key1] = new JObject {
			//			[_key2] = new JObject {
			//				["tags"] = new JArray () { _module.m_name },
			//				["summary"] = _method.m_summary,
			//				["description"] = _method.m_description,
			//				["operationId"] = "",
			//				["consumes"] = new JArray { "application/json", "application/x-www-form-urlencoded" },
			//				["produces"] = new JArray { "application/json" },
			//				["parameters"] = _parameters,
			//				["responses"] = new JObject {
			//					["200"] = new JObject { ["description"] = "success" },
			//					["500"] = new JObject { ["description"] = "fail" },
			//				},
			//			}
			//		};
			//		if (_method.m_api_key)
			//			_paths [_key1] [_key2] ["security"] = new JArray { new JObject { ["APIKeyHeader"] = new JArray () } };
			//	}
			//}
			//m_obj ["tags"] = _tags;
			//m_obj ["paths"] = _paths;
			var _tags = new JArray ();
			foreach (var _module in m_modules) {
				_tags.Add (new JObject {
					["name"] = _module.m_name,
					["description"] = _module.m_description,
				});
				foreach (var _method in _module.m_methods) {
					string _key1 = $"/api/{_module.m_prefix}/{_method.m_name}", _key2 = _method.m_request_type.ToLower ();
					if (_key2.is_null ()) {
						_key2 = "get";
						_method.m_description = $"{_method.m_description}\r\n\r\nThis method also supports GET/PUT/POST/DELETE requests";
					}
					var _parameters = new JArray ();
					foreach (var _param in _method.m_params) {
						_parameters.Add (new JObject {
							["in"] = _param.m_in,
							["name"] = _param.m_name,
							["description"] = _param.m_description,
							["required"] = true,
							["type"] = _param.m_type,
						});
					}
					m_obj ["paths"] [_key1] = new JObject {
						[_key2] = new JObject {
							["tags"] = new JArray () { _module.m_name },
							["summary"] = _method.m_summary,
							["description"] = _method.m_description,
							["parameters"] = _parameters,
							["responses"] = new JObject {
								["200"] = new JObject { ["description"] = "success" },
								["500"] = new JObject { ["description"] = "failure" },
							},
						},
					};
					if (_method.m_api_key) {
						m_obj ["paths"] [_key1] [_key2] ["security"] = JArray.Parse ("[{\"ApiKeyAuth\":[]}]");
						m_obj ["paths"] [_key1] [_key2] ["responses"] ["401"] = new JObject { ["description"] = "Unauthorized" };
					}
				}
			}
			m_obj ["tags"] = _tags;
			return m_obj.to_str ();
		}

		private JObject m_obj = null;
		private List<_DocModule> m_modules = new List<_DocModule> ();
	}
}
