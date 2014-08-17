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
	public class SEMotd : PluginBase, IChatEventHandler
	{
		
		#region "Attributes"


		private DateTime m_lastupdate;
		private DateTime m_ruleslastupdate;
		private Thread mainloop;
		private bool m_running;
		private uint m_messagecounter = 0;
		private uint m_currentindex = 0;
		SEMotdSettings settings = new SEMotdSettings();

		#endregion

		#region "Constructors and Initializers"

		public void Core()
		{
			Console.WriteLine("SE Motd Plugin '" + Id.ToString() + "' constructed!");	
		}

		public override void Init()
		{
			settings.interval = 300;
			settings.enable = true;
			settings.events = new List<SEMotdEvents>();
			Console.WriteLine("SE Motd Plugin '" + Id.ToString() + "' initialized!");
			loadXML();
			m_lastupdate = DateTime.UtcNow;
			m_ruleslastupdate = DateTime.UtcNow;
			m_running = true;
			m_messagecounter = 0;
			m_currentindex = 0;
			mainloop = new Thread(main);
			mainloop.Priority = ThreadPriority.BelowNormal;
			mainloop.Start();

		}

		#endregion

		#region "Properties"

		[Category("SE Motd")]
		[Description("Message of the day")]
		[Browsable(true)]
		[ReadOnly(false)]
		public string motd
		{
			get { return settings.motd; }
			set { settings.motd = value.ToString(); }
		}
		[Category("SE Rules")]
		[Description("Rules")]
		[Browsable(true)]
		[ReadOnly(false)]
		public string rules
		{
			get { return settings.rules; }
			set { settings.rules = value.ToString(); }
		}
		[Browsable(true)]
		[ReadOnly(true)]
		public string DefaultLocation
		{
			get { return System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\"; }

		}		
		[Browsable(true)]
		[ReadOnly(true)]
		public string Location
		{
			get { return SandboxGameAssemblyWrapper.Instance.GetServerConfig().LoadWorld  + "\\"; }
		
		}
		[Category("SE Motd")]
		[Description("interval in seconds")]
		[Browsable(true)]
		[ReadOnly(false)]
		public double interval
		{
			get { return settings.interval; }
			set { if (value > 0) settings.interval = value; }
		}

		[Category("SE Motd")]
		[Description("Enabled")]
		[Browsable(true)]
		[ReadOnly(false)]
		public bool enable
		{
			get { if(settings.enable) return true; else return false; }
			set { settings.enable = value; }
		}

		[Category("SE Motd")]
		[Description("On Join Notification")]
		[Browsable(true)]
		[ReadOnly(false)]
		public bool onJoinMessage
		{
			get { if (settings.onJoinMessage) return true; else return false; }
			set { settings.onJoinMessage = value; }
		}
		[Category("SE Motd")]
		[Description("Motd Repeat Suppress in seconds. Seconds until MOTD can be repeated")]
		[Browsable(true)]
		[ReadOnly(false)]
		public int motdRepeatSuppress
		{
			get { return settings.motdRepeatSuppress; }
			set { settings.motdRepeatSuppress = value; }
		}
		[Category("SE Rules")]
		[Description("Rules Repeat Suppress in seconds. Seconds until rules can be repeated")]
		[Browsable(true)]
		[ReadOnly(false)]
		public int rulesRepeatSuppress
		{
			get { return settings.rulesRepeatSuppress; }
			set { settings.rulesRepeatSuppress = value; }
		}
		[Category("SE Motd Rotation")]
		[Description("Rotating Message of the day")]
		[Browsable(true)]
		[ReadOnly(false)]
		public List<SEMotdEvents> events
		{
			get { return settings.events; }
		}

		[Category("SE Motd Rotation")]
		[Description("Frequency of rotating message 1 = every other message is from the rotation list, 2 = 2 rotating messages then 1 motd message and so on. 0 turns the rotation off")]
		[Browsable(true)]
		[ReadOnly(false)]
		public uint rotationFrequency
		{
			get { return settings.rotatingFrequency; }
			set { settings.rotatingFrequency = value; }
		}
		#endregion

		#region "Methods"

		public void saveXML()
		{

			XmlSerializer x = new XmlSerializer(typeof(SEMotdSettings));
			TextWriter writer = new StreamWriter(Location + "SEMotd-Config.xml");
			x.Serialize(writer, settings);
			writer.Close();

		}
		public void loadXML(bool defaults = false)
		{
			try
			{
				if (File.Exists(Location + "SEMotd-Config.xml") && !defaults)
				{

					XmlSerializer x = new XmlSerializer(typeof(SEMotdSettings));
					TextReader reader = new StreamReader(Location + "SEMotd-Config.xml");
					settings = (SEMotdSettings)x.Deserialize(reader);
					reader.Close();
					return;
				}
			}
			catch (Exception ex)
			{
				LogManager.APILog.WriteLineAndConsole("Could not load configuration: " + ex.ToString());
			}
			try
			{
				if (File.Exists(DefaultLocation + "SEMotd-Config.xml"))
				{
					XmlSerializer x = new XmlSerializer(typeof(SEMotdSettings));
					TextReader reader = new StreamReader(DefaultLocation + "SEMotd-Config.xml");
					settings = (SEMotdSettings)x.Deserialize(reader);
					reader.Close();
				}
			}
			catch (Exception ex)
			{
				LogManager.APILog.WriteLineAndConsole("Could not load configuration: " + ex.ToString());
			}

		}
		public void main()
		{
			//main execution loop
			while(m_running)
			{
				Thread.Sleep(1000);
				if (m_lastupdate + TimeSpan.FromSeconds(interval) < DateTime.UtcNow)
				{
					m_lastupdate = DateTime.UtcNow;
					m_messagecounter++;
					if (enable)
					{
						if (m_messagecounter > rotationFrequency)
						{
							sendMotd();
							m_messagecounter = 0;
						}
						else
							sendRotatingMessage();
					}
					
				}
			}
		}
		public void sendRotatingMessage()
		{

			if (settings.events.Count > 0)
			{
				int count = 0;
				if (m_currentindex >= settings.events.Count) m_currentindex = 0;
				foreach (SEMotdEvents seMotdEvent in settings.events)
				{
					if (count == m_currentindex)
					{
						if (seMotdEvent.message != "")
							ChatManager.Instance.SendPublicChatMessage(seMotdEvent.ToString());
						else
							sendMotd();
						break;
					}
					count++;
				}
				m_currentindex++;
			}
			else
				sendMotd();
		}
		public void sendMotd()
		{
			if(motd != "")
				ChatManager.Instance.SendPublicChatMessage(motd);
		}
		public void sendRules()
		{
			if (rules != "")
				ChatManager.Instance.SendPublicChatMessage(rules);
		}
		#region "EventHandlers"

		public override void Update()
		{

		}

		public override void Shutdown()
		{
			m_running = false;
			saveXML();
			//shut down main loop
			mainloop.Join(1000);
			mainloop.Abort();

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
					if (m_lastupdate + TimeSpan.FromSeconds(motdRepeatSuppress) < DateTime.UtcNow)
					{
						m_lastupdate = DateTime.UtcNow;
						sendMotd();
						return;
					}
				}
				if (words[0] == "/rules")
				{
					if (m_ruleslastupdate + TimeSpan.FromSeconds(rulesRepeatSuppress) < DateTime.UtcNow)
					{
						m_ruleslastupdate = DateTime.UtcNow;
						sendRules();
						return;
					}
				}				
				if(words.Count() > 1)
				{ 
					if (isadmin && words[0] == "/set" && words[1] == "motd")
					{
						rem = String.Join(" ", words, 2, words.Count() - 2);
						motd = rem;
						LogManager.APILog.WriteLineAndConsole("Motd set: " + motd);
						sendMotd();
						return;
					}
					if (isadmin && words[0] == "/set" && words[1] == "rules")
					{
						rem = String.Join(" ", words, 2, words.Count() - 2);
						rules = rem;
						LogManager.APILog.WriteLineAndConsole("Rules set: " + rules);
						sendRules();
						return;
					}
				}

				if (isadmin && words[0] == "/motd-enable")
				{
					enable = true;
					return;
				}

				if (isadmin && words[0] == "/motd-disable")
				{
					enable = false;
					return;
				}

				if (isadmin && words[0] == "/motd-save")
				{

					saveXML();
					ChatManager.Instance.SendPrivateChatMessage(obj.sourceUserId, "Motd Configuration Saved.");
					return;
				}
				if (isadmin && words[0] == "/motd-load")
				{
					loadXML(false);
					ChatManager.Instance.SendPrivateChatMessage(obj.sourceUserId, "Motd Configuration Loaded.");
					return;
				}
				if (isadmin && words[0] == "/motd-loaddefault")
				{
					loadXML(true);
					ChatManager.Instance.SendPrivateChatMessage(obj.sourceUserId, "Motd Configuration Defaults Loaded.");
					return;
				}
			}
			return; 
		}

		public void OnChatSent(SEModAPIExtensions.API.ChatManager.ChatEvent obj)
		{
			return; //no handling for motd right now
		}

/*		public void OnPlayerJoined(ulong steamid, CharacterEntity character)
		{
			if(onJoinMessage)
			{
				LogManager.APILog.WriteLineAndConsole("Motd: Player connected, onPlayerJoined fired: " + steamid.ToString());
				try
				{
					if (steamid > 0)
					{
						Thread T = new Thread(() => ChatManager.Instance.SendPrivateChatMessage(steamid, motd));
						T.Start();
					}
				}
				catch (Exception ex)
				{
					if (SandboxGameAssemblyWrapper.IsDebugging)
						LogManager.APILog.WriteLineAndConsole("Could not start private message thread. " + ex.ToString());
				}
			}
		}
		public void OnPlayerLeft(ulong nothing, CharacterEntity character)
		{
			return;
		}*/

		public void OnPlayerJoined(ulong steamId)
		{
			if (onJoinMessage)
			{
				LogManager.APILog.WriteLineAndConsole("Motd: Player connected, onPlayerJoined fired: " + steamId.ToString());
				try
				{
					if (steamId > 0)
					{
						Thread T = new Thread(() => ChatManager.Instance.SendPrivateChatMessage(steamId, motd));
						T.Start();
					}
				}
				catch (Exception ex)
				{
					if (SandboxGameAssemblyWrapper.IsDebugging)
						LogManager.APILog.WriteLineAndConsole("Could not start private message thread. " + ex.ToString());
				}
			}
		}
		public void OnPlayerLeft(ulong steamId)
		{
			return;
		}
		#endregion



		#endregion
	}
}
