using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Sparrow.Monitor.StateCache {
	internal class _TimeInc {
		private _TimeInc () {
			m_thread = new Thread (() => {
				var _time = DateTime.Now;
				while (true) {
					lock (s_object.m_actions) {
						m_actions.ForEach ((_a) => _a.Invoke ());
					}
					_time = _time.AddSeconds (1);
					var _ts = _time - DateTime.Now;
					if (_ts.TotalMilliseconds > 0)
						Thread.Sleep (_ts);
				}
			});
			m_thread.Start ();
		}

		public static void add_action (Action _action) {
			lock (s_object.m_actions) {
				s_object.m_actions.Add (_action);
			}
		}

		private static _TimeInc s_object = new _TimeInc ();
		private Thread m_thread;
		private List<Action> m_actions = new List<Action> ();
	}
}
