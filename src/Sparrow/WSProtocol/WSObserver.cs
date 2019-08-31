using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sparrow.WSProtocol {
	public class WSObserver {
		public virtual void OnConnect (JObject _obj_jwt) {}
		public virtual void OnPong () {}
		public virtual void OnRecv (byte [] _data) {}
		public virtual void OnError () {}
		public virtual void OnClose () {}

		public void send (byte [] _data) {
			// TODO
		}

		public void send (string _data) {
			send (new { result = "success", type = "data", content = _data }.to_json ().to_bytes ());
		}

		public void send_jwt_token (object _obj_jwt, DateTime _exp) {
			send (new { result = "success", type = "jwt_token", content = JWTManager.Generate (_obj_jwt, _exp) }.to_json ().to_bytes ());
		}

		public long m_id { get; private set; } = ++s_last_id;
		private static long s_last_id = -1;
	}
}
