using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SparrowServer.Monitor.StateCache {
	struct _ElapsedSavedItem {
		public long elapsed_min;
		public long elapsed_max;
		public long request_count;
		public long elapsed_sum;
	}

	struct _ElapsedReportdItem {
		public long elapsed_min;
		public long elapsed_max;
		public long elapsed_average;
	}

	public class Elapsed {
		public Elapsed (string _name, params string [] _labels) {
			m_name = _name;
			m_labels = _labels;
			if ((_labels?.Length ?? 0) < 1)
				throw new Exception ("标签数量错误");
			m_values = new _ElapsedSavedItem [_labels.Length];
			for (int i = 0; i < _labels.Length; ++i)
				m_values [i] = new _ElapsedSavedItem () { elapsed_min = 0, elapsed_max = 0, request_count = 0, elapsed_sum = 0 };
		}

		public void add_value (int _index, long _elapsed_ms) {
			lock (m_values) {
				if (_index >= 0 && _index < m_labels.Length) {
					if (m_values [_index].elapsed_min == -1) {
						m_values [_index].elapsed_min = m_values [_index].elapsed_max = _elapsed_ms;
					} else {
						m_values [_index].elapsed_min = Math.Min (m_values [_index].elapsed_min, _elapsed_ms);
						m_values [_index].elapsed_max = Math.Max (m_values [_index].elapsed_max, _elapsed_ms);
					}
					m_values [_index].request_count++;
					m_values [_index].elapsed_sum += _elapsed_ms;
				}
			}
		}

		public JObject get_values () {
			lock (m_values) {
				var _values = (from p in m_values select new _ElapsedReportdItem () { elapsed_min = p.elapsed_min, elapsed_max = p.elapsed_max, elapsed_average = (long) (p.elapsed_sum / (double) p.request_count + 0.5000001) });
				return new { type = "elapsed", name = m_name, labels = m_labels, values = m_values }.json ();
			}
		}

		private string m_name;
		private string [] m_labels;
		//
		private _ElapsedSavedItem [] m_values;
	}
}
