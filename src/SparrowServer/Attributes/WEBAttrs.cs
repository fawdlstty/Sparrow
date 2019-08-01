using System;
using System.Collections.Generic;
using System.Text;

namespace SparrowServer.Attributes {
	// Module / 模块
	public class WEBModuleAttribute : Attribute {
		public WEBModuleAttribute (string description) {
			m_description = description;
		}

		public string m_description { get; private set; } = "";
	}

	// Method 方法
	public class WEBMethod {
		public interface IWEBMethod { string Type { get; } string Summary { get; } string Description { get; } }
		public class GETAttribute : Attribute, IWEBMethod {
			public GETAttribute (string summary, string description = "") { Summary = summary; Description = description; }
			public string Type { get { return "GET"; } }
			public string Summary { get; private set; }
			public string Description { get; private set; }
		}
		public class PUTAttribute : Attribute, IWEBMethod {
			public PUTAttribute (string summary, string description = "") { Summary = summary; Description = description; }
			public string Type { get { return "PUT"; } }
			public string Summary { get; private set; }
			public string Description { get; private set; }
		}
		public class POSTAttribute : Attribute, IWEBMethod {
			public POSTAttribute (string summary, string description = "") { Summary = summary; Description = description; }
			public string Type { get { return "POST"; } }
			public string Summary { get; private set; }
			public string Description { get; private set; }
		}
		public class DELETEAttribute : Attribute, IWEBMethod {
			public DELETEAttribute (string summary, string description = "") { Summary = summary; Description = description; }
			public string Type { get { return "DELETE"; } }
			public string Summary { get; private set; }
			public string Description { get; private set; }
		}
	}

	// Parameter / 参数
	public class WEBParam {
		public interface IWEBParam { string Name { get; } }
		public class IPAttribute : Attribute, IWEBParam { public string Name { get { return "IP"; } } }
		public class AgentIPAttribute : Attribute, IWEBParam { public string Name { get { return "AgentIP"; } } }
	}
}
