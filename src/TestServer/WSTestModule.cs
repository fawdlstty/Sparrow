using Newtonsoft.Json.Linq;
using SparrowServer.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace TestServer {
	[WSModule ("webservice module")]
	public class WSTestModule {
		[JWTRequest]
		public static WSTestModule _check_auth (JObject _jwt) {
			return new WSTestModule { n = 100 };
		}

		//[WSOnMsg]
		public void on_msg (byte [] _data) {
			
		}

		private int n = 0;
	}
}
