using Newtonsoft.Json.Linq;
using SparrowServer;
using SparrowServer.Attributes;
using SparrowServer.WSProtocol;
using System;

namespace TestServer {
	// TODO: This module is not finished, and the function is temporarily unavailable
	// TODO: 此模块暂未完成，功能暂时无法使用
	[WSModule ("webservice module")]
	public class WSTestModule : WSObserver {
		[PureConnect]
		public static WSTestModule _check_auth () {
			return new WSTestModule { n = 100 };
		}

		[JWTConnect]
		public static WSTestModule _check_auth (JObject _jwt) {
			return new WSTestModule { n = 200 };
		}

		[WSMethod]
		public static string on_hello () {
			return "hello websocket";
		}

		[WSMethod]
		public void set_n_value (int value) {
			n = value;
		}

		[WSMethod]
		public int get_n_value () {
			return n;
		}

		public override void OnConnect (JObject _obj_jwt) {}
		public override void OnPong () {
			send_jwt_token (new { name = "faw" }.json (), DateTime.Now.AddMinutes (2));
		}
		public override void OnRecv (byte [] _data) {}
		public override void OnError () {}
		public override void OnClose () {}

		private int n = 0;
	}
}
