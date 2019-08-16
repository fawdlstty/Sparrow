using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace SparrowServer.WSProtocol {
	public class WSObserver {
		public virtual void OnConnect (JObject _obj_jwt) {}
		public virtual void OnPong () {}
		public virtual void OnRecv (byte [] _data) {}
		public virtual void OnError () {}
		public virtual void OnClose () {}

		public void send (byte [] _data) {

		}

		public void send_jwt_token (JObject _obj_jwt, DateTime _invalid) {

		}

		public long m_id { get; private set; } = ++s_last_id;
		private static long s_last_id = -1;
	}
}
