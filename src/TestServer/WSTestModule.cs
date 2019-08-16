using Newtonsoft.Json.Linq;
using SparrowServer;
using SparrowServer.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace TestServer {
	[WSModule ("webservice module")]
	public class WSTestModule {
		[JWTConnect]
		public static WSTestModule _check_auth (JObject _jwt) {
			return new WSTestModule { n = 100 };
		}

		public void on_pong () {
			//send_new_auth ();
		}

		public void on_recv (byte [] _data) {
			var _str = _data.to_str ();
		}

		private int n = 0;
	}
}
