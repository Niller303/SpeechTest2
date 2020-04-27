using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SpeechTest2 {
	class Program {
		public static Settings config;
		private static Dictionary<string,Command> cmds;
		private static bool keepRunning = true;
		public static Speaker spk;
		public static Streamer strm;

		class Command {
			public Action<CommandMessage> callback { get; private set; }
			//Could be replaced with an int and bitlevel ands, meh
			public bool RequireMod { get; private set; }
			public bool RequireBroadcaster { get; private set; }
			public Command(bool ReqMod, bool ReqBroad, Action<CommandMessage> callback) {
				this.RequireMod = ReqMod;
				this.RequireBroadcaster = ReqBroad;
				this.callback = callback;
			}
		}

		static void Main(string[] args) {
			//TODO: Google TTS, Pico TTS, Polly TTS, VoiceRSS TTS
			//TODO: Better config system, or maybe just a dedicated section in the json for each
			cmds = new Dictionary<string, Command>();

			config = Settings.LoadConfig();
			config.SaveConfig();

			spk = new Speaker();
			strm = new Streamer((m) => {
				if (m.Text.StartsWith(config.command_prerequisite)) {
					string command = m.Text.Substring(config.command_prerequisite.Length); //TODO: Check length
					int idx = command.IndexOf(" ");
					if (idx != -1) { //Happens on no argument commands
						command = command.Substring(0, command.IndexOf(" "));
					}

					if (cmds.ContainsKey(command)) {
						Command c = cmds[command];
						bool reqmod = c.RequireMod && (m.IsModerator || m.IsBroadcaster);
						bool reqbrd = c.RequireBroadcaster && m.IsBroadcaster;
						bool reqnon = (!c.RequireMod && !c.RequireBroadcaster);
						if (reqnon || reqmod || reqbrd) {
							string arg = m.Text.Substring(m.Text.IndexOf(command) + command.Length);
							if (arg.Length != 0 && arg[0] == ' ') {
								arg = arg.Substring(1);
							}
							CommandMessage cm = new CommandMessage(m, arg);
							c.callback(cm);
						}
					}
				} else {
					spk.Speak(m);
				}
			});

			cmds.Add("setvoice", new Command(true, false, async (m) => {
				string[] cmd = m.Command.Split(' ');
				if (cmd.Length < 2) {
					strm.bot.Say("Wrong command usage!");
					return;
				}
				
				var resp = await strm.monitor.API.V5.Users.GetUserByNameAsync(cmd[0]);
				if (resp.Total <= 0) {
					strm.bot.Say("No such user!");
					return;
				}

				string username = cmd[0];
				string userid = resp.Matches[0].Id;
				string voice = string.Join(" ", cmd).Substring(username.Length + 1);

				//Contains is dumb
				if (Array.IndexOf(spk.speaker.GetVoices(), voice) < 0) {
					strm.bot.Say("Invalid voice!");
					return;
				}
				
				Console.WriteLine(username + "=" + voice);
				config.custom_voices[userid] = voice;
				config.SaveConfig();
			}));
			cmds.Add("removevoice", new Command(true, false, async (m) => {
				var cmd = m.Command;
				
				var resp = await strm.monitor.API.V5.Users.GetUserByNameAsync(cmd);
				if (resp.Total <= 0) {
					strm.bot.Say("No such user!");
					return;
				}

				string username = cmd;
				string userid = resp.Matches[0].Id;
				
				if (config.custom_voices.ContainsKey(userid)) {
					config.custom_voices.Remove(userid);
					Console.WriteLine(username + " removed");
					config.SaveConfig();
				} else {
					strm.bot.Say("Given user does not have a custom voice!");
				}
				
			}));
			cmds.Add("getvoices", new Command(true, false, async (m) => {
				string voices = string.Join(",", spk.speaker.GetVoices());
				strm.bot.Say(voices);
			}));
			cmds.Add("getsetvoices", new Command(true, false, (m) => {
				strm.bot.Say(string.Join(",", config.custom_voices.Keys.Select((id) => {
					var resp = strm.monitor.API.V5.Users.GetUserByIDAsync(id).Result;
					return resp.DisplayName + "=" + config.custom_voices[id];
				})));
			}));
			cmds.Add("help", new Command(true, false, async (m) => {
				strm.bot.Say("setvoice SomeUser Samantha, removevoice SomeUser, getvoices, getsetvoices, help");
			}));

			Console.CancelKeyPress += delegate (object sender, ConsoleCancelEventArgs e) {
				e.Cancel = true;
				keepRunning = false;
			};
			//TODO: I dont think ProcessExit works
			AppDomain.CurrentDomain.ProcessExit += delegate (object sender, EventArgs e) {
				keepRunning = false;
			};

			Console.WriteLine("Running until ctrl+c");
			while (keepRunning) {
				Thread.Sleep(1 * 1000);
			}
			
			Console.WriteLine("Dispose streamer");
			strm.Dispose();
			Console.WriteLine("Dispose speaker");
			spk.Dispose();
			
			Console.WriteLine(config.username);
			Console.WriteLine("Done!");
		}
	}
}
