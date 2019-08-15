using System;
using System.Collections.Generic;
using System.Text;

namespace SparrowServer.Attributes {
	// Module
	public class WSModuleAttribute : Attribute {
		public WSModuleAttribute (string description) { m_description = description; }
		public string m_description { get; private set; } = "";
	}

	public class WSMethodAttribute : Attribute, IHTTPMethod {
		public WSMethodAttribute (string summary, string description = "") { Summary = summary; Description = description; }
		public string Type { get { return ""; } }
		public string Summary { get; private set; } = "";
		public string Description { get; private set; } = "";
	}
}
