using System;
using System.Collections.Generic;
using System.Text;

namespace SparrowServer.Monitor.Counter {
	public class Guages<T> {
		public Guages (string _name, T _full, params string [] _labels) {
			m_name = _name;
			m_full = _full;
			m_labels = _labels;
			if ((_labels?.Length ?? 0) < 1)
				throw new Exception ("label length must >= 1");
		}

		private string m_name;
		private T m_full;
		private string [] m_labels;

		private List<T []> m_data = new List<T []> ();
	}
}
