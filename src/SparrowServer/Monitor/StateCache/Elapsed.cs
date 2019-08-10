using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SparrowServer.Monitor.StateCache {
	public class Elapsed {
		public Elapsed (string _name, params string [] _labels) {
			m_name = _name;
			m_labels = _labels;
			if ((_labels?.Length ?? 0) < 1)
				throw new Exception ("标签数量错误");
			_TimeInc.add_action (() => {
				var _val = new long [m_labels.Length];
				for (int i = 0; i < _val.Length; ++i)
					_val [i] = 0;
				lock (m_values) {
					m_values.Add (new Dictionary<int, int> [m_labels.Length]);
					if (m_values.Count > 600)
						m_values.RemoveAt (0);
				}
			});
		}

		public void add_value (int _index, int _elapsed_ms) {
			lock (m_values) {
				if (m_values.Count > 0 && _index >= 0 && _index < m_labels.Length) {
					if (m_values [m_values.Count - 1] [_index] == null) {
						m_values [m_values.Count - 1] [_index] = new Dictionary<int, int> () { [_elapsed_ms] = 1 };
					} else if (m_values [m_values.Count - 1] [_index].ContainsKey (_elapsed_ms)) {
						++m_values [m_values.Count - 1] [_index] [_elapsed_ms];
					} else {
						m_values [m_values.Count - 1] [_index].Add (_elapsed_ms, 1);
					}
				}
			}
		}

		public JObject get_values (int _count) {
			_count = (_count < 1 ? m_values.Count : Math.Min (_count, m_values.Count));
			lock (m_values) {
				var _count_items = m_values.Skip (m_values.Count - _count);
				var _values = new Dictionary<int, int> [m_labels.Length];
				// TODO: Counter
				return new { type = "counter", name = m_name, labels = m_labels, values = _values }.json ();
			}
		}

		private string m_name;
		private string [] m_labels;
		//
		private List<Dictionary<int, int> []> m_values = new List<Dictionary<int, int> []> ();
	}
}
