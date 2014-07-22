using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Timers;
using System.Reflection;
using System.Threading;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.VRageData;

using SEModAPIExtensions.API.Plugin;
using SEModAPIExtensions.API.Plugin.Events;
using SEModAPIExtensions.API;

using SEModAPIInternal.API.Common;
using SEModAPIInternal.API.Entity;
using SEModAPIInternal.API.Entity.Sector.SectorObject;
using SEModAPIInternal.API.Entity.Sector.SectorObject.CubeGrid;
using SEModAPIInternal.API.Entity.Sector.SectorObject.CubeGrid.CubeBlock;
using SEModAPIInternal.API.Server;
using SEModAPIInternal.Support;

using SEModAPI.API;

using VRageMath;
using VRage.Common.Utils;



namespace SEMotd
{
	[Serializable()]
	public class SEMotd : PluginBase, IChatEventHandler
	{
		
		#region "Attributes"
		[field: NonSerialized()]
		private string m_motd = "";
		[field: NonSerialized()]
		private DateTime m_lastupdate;
		[field: NonSerialized()]
		private double m_interval = 300;
		[field: NonSerialized()]
		private bool m_enable = true;	

		#endregion

		#region "Constructors and Initializers"

		public void Core()
		{
			Console.WriteLine("SE Motd Plugin '" + Id.ToString() + "' constructed!");	
		}

		public override void Init()
		{
			m_interval = 300;//5 minutes by default
			m_enable = true;
			Console.WriteLine("SE Motd Plugin '" + Id.ToString() + "' initialized!");
			loadXML();
			m_lastupdate = DateTime.UtcNow;
		}

		#endregion

		#region "Properties"

		[Category("SE Motd")]
		[Description("Message of the day")]
		[Browsable(true)]
		[ReadOnly(false)]
		public string motd
		{
			get { return m_motd; }
			set { m_motd = value.ToString(); }
		}
		
		[Browsable(true)]
		[ReadOnly(true)]
		public string Location
		{
			get { return System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\"; }
		
		}
		[Category("SE Motd")]
		[Description("interval in seconds")]
		[Browsable(true)]
		[ReadOnly(false)]
		public double interval
		{
			get { return m_interval; }
			set { if (m_interval > 0) m_interval = value; }
		}

		[Category("SE Motd")]
		[Description("Enabled")]
		[Browsable(true)]
		[ReadOnly(false)]
		public bool enable
		{
			get { if(m_enable) return true; else return false; }
			set { m_enable = value; }
		}
		#endregion

		#region "Methods"

		public void saveXML()
		{

			XmlSerializer x = new XmlSerializer(typeof(SEMotd));
			TextWriter writer = new StreamWriter(Location + "Configuration.xml");
			x.Serialize(writer, this);
			writer.Close();

		}
		public void loadXML()
		{
			try
			{
				if (File.Exists(Location + "Configuration.xml"))
				{
					XmlSerializer x = new XmlSerializer(typeof(SEMotd));
					TextReader reader = new StreamReader(Location + "Configuration.xml");
					SEMotd obj = (SEMotd)x.Deserialize(reader);
					motd = obj.motd;
					interval = obj.interval;
					enable = obj.enable;
					reader.Close();
				}
			}
			catch (Exception ex)
			{
				LogManager.APILog.WriteLineAndConsole("Could not load configuration: " + ex.ToString());
			}

		}

		public void sendMotd()
		{
			if(m_motd != "")
				ChatManager.Instance.SendPublicChatMessage(m_motd);
		}

		#region "EventHandlers"

		public override void Update()
		{
			//prevent multiple update threads to run at once.
			if(m_lastupdate + TimeSpan.FromSeconds(m_interval) < DateTime.UtcNow )
			{
				m_lastupdate = DateTime.UtcNow;
				if(m_enable)
					sendMotd();
			}
		}

		public override void Shutdown()
		{
			saveXML();
			return;
		}

		public void OnChatReceived(SEModAPIExtensions.API.ChatManager.ChatEvent obj)
		{

			if (obj.sourceUserId == 0)
				return;
			bool isadmin = SandboxGameAssemblyWrapper.Instance.IsUserAdmin(obj.sourceUserId);

			if( obj.message[0] == '/' )
			{

				string[] words = obj.message.Split(' ');
				string rem;
				//proccess
				if (words[0] == "/motd")
				{
					if (m_lastupdate + TimeSpan.FromMinutes(1) < DateTime.UtcNow)
					{
						m_lastupdate = DateTime.UtcNow;
						sendMotd();
						return;
					}
				}
				
				if(words.Count() > 1)
					if (isadmin && words[0] == "/set" && words[1] == "motd")
					{
						rem = String.Join(" ", words, 2, words.Count() - 2);
						m_motd = rem;
						LogManager.APILog.WriteLineAndConsole("Motd set: " + m_motd);
						sendMotd();
						return;
					}

				if (isadmin && words[0] == "/motd-enable")
				{
					m_enable = true;
					return;
				}

				if (isadmin && words[0] == "/motd-disable")
				{
					m_enable = false;
					return;
				}
			}
			return; 
		}

		public void OnChatSent(SEModAPIExtensions.API.ChatManager.ChatEvent obj)
		{
			return; //no handling for motd right now
		}
		#endregion



		#endregion
	}
}
