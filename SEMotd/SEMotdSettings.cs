using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace SEMotd
{
	[Serializable()]
	public class SEMotdSettings
	{
		private string m_rules = "";
		private string m_motd = "";
		private double m_interval = 300;
		private bool m_enable = true;
		private bool m_onJoinMessage = true;
		private int m_motdRepeatSuppress = 60;
		private int m_rulesRepeatSuppress = 60;
		private uint m_rotatingfrequency = 0;
		private List<SEMotdEvents> m_events = new List<SEMotdEvents>(1);

		public string rules
		{
			get { return m_rules; }
			set { m_rules = value; }
		}
		public string motd
		{
			get { return m_motd; }
			set { m_motd = value; }
		}
		public double interval
		{
			get { return m_interval; }
			set { m_interval = value; }
		}
		public bool enable
		{
			get { return m_enable; }
			set { m_enable = value; }
		}
		public bool onJoinMessage
		{
			get { return m_onJoinMessage; }
			set { m_onJoinMessage = value; }
		}
		public int rulesRepeatSuppress
		{
			get { return m_rulesRepeatSuppress; }
			set { if (value > 0) m_rulesRepeatSuppress = value; else m_rulesRepeatSuppress = 1; }
		}
		public int motdRepeatSuppress
		{
			get { return m_motdRepeatSuppress; }
			set { if (value > 0) m_motdRepeatSuppress = value; else m_motdRepeatSuppress = 1; }
		}
		public List<SEMotdEvents> events
		{
			get { return m_events; }
			set { m_events = value; }
		}
		public uint rotatingFrequency
		{
			get { return m_rotatingfrequency; }
			set { m_rotatingfrequency = value; }
		}
	}
}
