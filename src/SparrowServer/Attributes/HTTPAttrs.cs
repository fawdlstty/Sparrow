using System;
using System.Collections.Generic;
using System.Text;

namespace SparrowServer.Attributes {
	// Module
	public class HTTPModuleAttribute : Attribute {
		public HTTPModuleAttribute (string description) { m_description = description; }
		public string m_description { get; private set; } = "";
	}

	// Method
	public interface IHTTPMethod { string Type { get; } string Summary { get; } string Description { get; } }
	public class HTTPAttribute : Attribute, IHTTPMethod {
		public HTTPAttribute (string summary, string description = "") { Summary = summary; Description = description; }
		public string Type { get { return ""; } }
		public string Summary { get; private set; } = "";
		public string Description { get; private set; } = "";
	}
	public class HTTP {
		public class GETAttribute : Attribute, IHTTPMethod {
			public GETAttribute (string summary, string description = "") { Summary = summary; Description = description; }
			public string Type { get { return "GET"; } }
			public string Summary { get; private set; } = "";
			public string Description { get; private set; } = "";
		}
		public class PUTAttribute : Attribute, IHTTPMethod {
			public PUTAttribute (string summary, string description = "") { Summary = summary; Description = description; }
			public string Type { get { return "PUT"; } }
			public string Summary { get; private set; } = "";
			public string Description { get; private set; } = "";
		}
		public class POSTAttribute : Attribute, IHTTPMethod {
			public POSTAttribute (string summary, string description = "") { Summary = summary; Description = description; }
			public string Type { get { return "POST"; } }
			public string Summary { get; private set; } = "";
			public string Description { get; private set; } = "";
		}
		public class DELETEAttribute : Attribute, IHTTPMethod {
			public DELETEAttribute (string summary, string description = "") { Summary = summary; Description = description; }
			public string Type { get { return "DELETE"; } }
			public string Summary { get; private set; } = "";
			public string Description { get; private set; } = "";
		}
	}

	// JWT
	public interface IJWTMethod { string Type { get; } }
	public class JWTGenAttribute: Attribute, IJWTMethod { public string Type { get { return "Gen"; } } }
	public class JWTRequestAttribute : Attribute, IJWTMethod { public string Type { get { return "Request"; } } }

	// Parameter
	public interface IReqParam { string Name { get; } }
	public class ReqParam {
		public class IPAttribute : Attribute, IReqParam { public string Name { get { return ":IP"; } } }
		public class AgentIPAttribute : Attribute, IReqParam { public string Name { get { return ":AgentIP"; } } }
		public class OptionAttribute : Attribute, IReqParam { public string Name { get { return ":Option"; } } }
	}
	public class ParamAttribute : Attribute {
		public ParamAttribute (string description) { m_description = description; }
		public string m_description { get; private set; } = "";
	}
}
