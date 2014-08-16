using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace SEMotd
{
	[Serializable()]
	public class SEMotdEvents
	{
		private string m_message = "";
		[Browsable(true)]
		[ReadOnly(false)]
		public string message { get { return m_message; } set { m_message = value; } }

		public override string ToString()
		{
			return message.ToString();
		}
	}
}
