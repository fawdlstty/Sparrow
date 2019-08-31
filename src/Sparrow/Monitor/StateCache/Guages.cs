using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sparrow.Monitor.StateCache {
	internal class Guages<T> {
		public Guages (string _name, T _min, T _max, params string [] _labels) {
			m_name = _name;
			m_min = _min;
			m_max = _max;
			m_labels = _labels;
			if ((_labels?.Length ?? 0) < 1)
				throw new Exception ("标签数量错误");
		}

		public void set_callback (Func<T []> _func) {
			_TimeInc.add_action (() => {
				var _value = _func ();
				if (_value.Length != m_labels.Length)
					throw new Exception ("值数量错误");
				lock (m_values) {
					m_values.Add (_value);
					if (m_values.Count > 600)
						m_values.RemoveAt (0);
				}
			});
		}

		//public void set_value (params T [] _value) {
		//	if (_value.Length != m_labels.Length)
		//		throw new Exception ("值数量错误");
		//	lock (m_values) {
		//		m_values.Add (_value);
		//		if (m_values.Count > 600)
		//			m_values.RemoveAt (0);
		//	}
		//}

		public JObject get_values (int _count) {
			_count = (_count < 1 ? m_values.Count : Math.Min (_count, m_values.Count));
			lock (m_values)
				return new { type = "guages", name = m_name, min = m_min, max = m_max, labels = m_labels, values = m_values.Skip (m_values.Count - _count) }.json ();
		}

		private string m_name;
		private T m_min;
		private T m_max;
		private string [] m_labels;
		//
		private List<T []> m_values = new List<T []> ();
	}
}
