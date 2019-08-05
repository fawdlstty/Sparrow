using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SparrowServer.Swagger {
	class _DocModule {
		public string m_name;
		public string m_prefix;
		public string m_description;
		public List<_DocMethod> m_methods = new List<_DocMethod> ();
	}

	class _DocMethod {
		public string m_request_type;
		public string m_name;
		public string m_summary;
		public string m_description;
		public List<_DocParam> m_params = new List<_DocParam> ();
	}

	class _DocParam {
		public string m_name;
		public string m_type;
		public string m_description;
	}

	public class DocBuilder {
		public DocBuilder (WEBDocInfo _doc_info) {
			m_obj = new JObject {
				["swagger"] = "2.0",
				["schemes"] = new JArray { _doc_info.Scheme },
				["host"] = _doc_info.Host,
				["basePath"] = "/",
				["info"] = new JObject {
					["title"] = _doc_info.DocName,
					["version"] = _doc_info.Version,
					["description"] = _doc_info.Description,
					//["termsOfService"] = "",
					["contact"] = new JObject { ["email"] = _doc_info.Contact, },
					//["license"] = new JObject { ["name"] = "Unlicense", ["url"] = "#", },
				},
				//["externalDocs"] = new JObject { ["description"] = "查看更多文档", ["url"] = "#", },
			};
			//m_obj = new JObject {
			//	["openapi"] = "3.0.0",
			//	["info"] = _doc_info.DocName,
			//	["description"] = _doc_info.Description,
			//	["version"] = _doc_info.Version,
			//	["servers"] = new JArray { new JObject {
			//		["url"] = $"{_doc_info.Scheme}://{_doc_info.Host}",
			//		["description"] = "测试接口",
			//	}}
			//};
		}

		public void add_module (string _module_name, string _module_prefix, string _description) {
			if (_module_name.is_null ())
				throw new Exception ("Name format error / 名称格式错误");
			if ((from p in m_modules where p.m_name == _module_name || p.m_prefix == _module_prefix select 1).Count () > 0)
				throw new Exception ("Repeated addition of modules / 模块重复添加");
			m_modules.Add (new _DocModule { m_name = _module_name, m_prefix = _module_prefix, m_description = _description });
		}

		public void add_method (string _module_name, string _request_type, string _method_name, string _summary, string _description = "") {
			if (_module_name.is_null () || _method_name.is_null ())
				throw new Exception ("Name format error / 名称格式错误");
			if (_description.is_null ())
				_description = _summary;
			for (int i = m_modules.Count - 1; i >= 0; --i) {
				if (m_modules [i].m_name == _module_name) {
					if ((from p in m_modules [i].m_methods where p.m_request_type == _request_type && p.m_name == _method_name select 1).Count () > 0)
						throw new Exception ("Repeated addition of methods / 方法重复添加");
					m_modules [i].m_methods.Add (new _DocMethod { m_request_type = _request_type, m_name = _method_name, m_summary = _summary, m_description = _description });
					return;
				}
			}
			throw new Exception ("Module is not added / 模块未添加");
		}

		public void add_param (string _module_name, string _request_type, string _method_name, string _param_name, string _param_type, string _description = "") {
			if (_module_name.is_null () || _method_name.is_null () || _param_name.is_null ())
				throw new Exception ("Name format error / 名称格式错误");
			for (int i = m_modules.Count - 1; i >= 0; --i) {
				if (m_modules [i].m_name == _module_name) {
					for (int j = 0; j < m_modules [i].m_methods.Count; ++j) {
						if (m_modules [i].m_methods [j].m_request_type == _request_type && m_modules [i].m_methods [j].m_name == _method_name) {
							if ((from p in m_modules [i].m_methods [j].m_params where p.m_name == _param_name select 1).Count () > 0)
								throw new Exception ("Repeated addition of params / 参数重复添加");
							m_modules [i].m_methods [j].m_params.Add (new _DocParam () { m_name = _param_name, m_type = _param_type, m_description = _description });
							return;
						}
					}
				}
			}
			throw new Exception ("Module or method is not added / 模块或方法未添加");
		}

		public string build () {
			var _tags = new JArray ();
			var _paths = new JObject ();
			foreach (var _module in m_modules) {
				_tags.Add (new JObject {
					["name"] = _module.m_name,
					["description"] = _module.m_description,
				});
				foreach (var _method in _module.m_methods) {
					var _parameters = new JArray ();
					foreach (var _param in _method.m_params) {
						_parameters.Add (new JObject {
							["in"] = "query",
							["name"] = _param.m_name,
							["description"] = _param.m_description,
							["required"] = true,
							["type"] = _param.m_type,
						});
					}
					_paths [$"/api/{_module.m_prefix}/{_method.m_name}"] = new JObject {
						[_method.m_request_type.ToLower ()] = new JObject {
							["tags"] = new JArray () { _module.m_name },
							["summary"] = _method.m_summary,
							["description"] = _method.m_description,
							["operationId"] = "",
							["consumes"] = new JArray { "application/json", "application/x-www-form-urlencoded" },
							["produces"] = new JArray { "application/json" },
							["parameters"] = _parameters,
							["responses"] = new JObject {
								["200"] = new JObject { ["description"] = "success" },
								["500"] = new JObject { ["description"] = "fail" },
							},
							//["security"] = new JArray {},
						}
					};
				}
			}
			m_obj ["tags"] = _tags;
			m_obj ["paths"] = _paths;
			//foreach (var _module in m_modules) {
			//	foreach (var _method in _module.m_methods) {
			//		m_obj ["paths"] = new JObject {
			//			[$"/api/{_module.m_prefix}/{_method.m_name}"] = new JObject {
			//				[_method.m_request_type.ToLower ()] = new JObject {
			//					["summary"] = _method.m_summary,
			//					["description"] = _method.m_description,
			//					["content"] = new JObject {
			//						["application/json"] = new JObject {
			//							["schema"] = new JObject {
			//							},
			//						},
			//					},
			//				},
			//			},
			//		};
			//	}
			//}
			return m_obj.to_str ();
		}

		private JObject m_obj = null;
		private List<_DocModule> m_modules = new List<_DocModule> ();
	}
}
