using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TwitchLib.Api;
using TwitchLib.Api.Services;
using TwitchLib.Api.Services.Events;
using TwitchLib.Api.Services.Events.FollowerService;
using TwitchLib.Api.Services.Events.LiveStreamMonitor;
using TwitchLib.Client;
using TwitchLib.Client.Enums;
using TwitchLib.Client.Events;
using TwitchLib.Client.Extensions;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;

namespace SpeechTest2 {
	class Streamer : IDisposable {
		internal class Bot : IDisposable {
			public Streamer parent;
			public TwitchClient client;

			public Bot(string username, string access_token, string channel) {
				ConnectionCredentials credentials = new ConnectionCredentials(username, access_token);
				var clientOptions = new ClientOptions {
					MessagesAllowedInPeriod = 750,
					ThrottlingPeriod = TimeSpan.FromSeconds(30)
				};
				WebSocketClient customClient = new WebSocketClient(clientOptions);
				client = new TwitchClient(customClient);
				client.Initialize(credentials, channel);

				client.OnLog += Client_OnLog;
				client.OnJoinedChannel += Client_OnJoinedChannel;
				client.OnMessageReceived += Client_OnMessageReceived;
				//client.OnWhisperReceived += Client_OnWhisperReceived;
				client.OnNewSubscriber += Client_OnNewSubscriber;
				client.OnConnected += Client_OnConnected;
				client.OnUserTimedout += Client_OnUserTimedOut;
				client.OnUserBanned += Client_OnUserBanned;

				client.Connect();
			}

			private void Client_OnUserBanned(object sender, OnUserBannedArgs e) {
				var resp = this.parent.monitor.API.V5.Users.GetUserByNameAsync(e.UserBan.Username).Result;
				if (resp.Total <= 0) {
					Console.WriteLine($"OnUserBanned failed to find {e.UserBan.Username}");
					return;
				}

				if (Program.spk.currentMessage != null && Program.spk.currentMessage.User == resp.Matches[0].Id) {
					Program.spk.SkipSpeak();
				}
			}

			private void Client_OnUserTimedOut(object sender, OnUserTimedoutArgs e) {
				var resp = this.parent.monitor.API.V5.Users.GetUserByNameAsync(e.UserTimeout.Username).Result;
				if (resp.Total <= 0) {
					Console.WriteLine($"OnUserTimedOut failed to find {e.UserTimeout.Username}");
					return;
				}

				if (Program.spk.currentMessage != null && Program.spk.currentMessage.User == resp.Matches[0].Id) {
					Program.spk.SkipSpeak();
				}
			}

			public void Say(string message) {
				int startidx = 0;
				while (startidx < message.Length) {
					this.client.SendMessage(this.client.JoinedChannels[0], message.Substring(startidx, (startidx+500) > message.Length ? (message.Length - startidx) : (startidx+500)));
					startidx += 500;
				}
			}

			private void Client_OnLog(object sender, OnLogArgs e) {
				Console.WriteLine($"{e.DateTime.ToString()}: {e.BotUsername} - {e.Data}");
			}

			private void Client_OnConnected(object sender, OnConnectedArgs e) {
				Console.WriteLine($"Connected to {e.AutoJoinChannel}");
			}

			private void Client_OnJoinedChannel(object sender, OnJoinedChannelArgs e) {
				Console.WriteLine("Hey guys! I am a bot connected via TwitchLib!");

				if (Program.config.format_onjoin != null) {
					string msg = String.Format(Program.config.format_onjoin, e.Channel);
					client.SendMessage(e.Channel, msg);
					parent.callback(new Message(msg));
				}
			}

			private void Client_OnMessageReceived(object sender, OnMessageReceivedArgs e) {
				foreach (var badword in Program.config.badwords) {
					if (e.ChatMessage.Message.Contains(badword)) {
						client.TimeoutUser(e.ChatMessage.Channel, e.ChatMessage.Username, TimeSpan.FromMinutes(Program.config.badword_penalty), "Bad word found!");
						return;
					}
				}

				if (e.ChatMessage.Message.StartsWith(Program.config.speaker_prerequisite)) {
					parent.callback(new Message(e.ChatMessage.UserId, e.ChatMessage.Message,e.ChatMessage.IsSubscriber,e.ChatMessage.IsModerator,e.ChatMessage.IsBroadcaster));
				}
			}

			private void Client_OnWhisperReceived(object sender, OnWhisperReceivedArgs e) {
				/*if (e.WhisperMessage.Username == "my_friend")
					client.SendWhisper(e.WhisperMessage.Username, "Hey! Whispers are so cool!!");*/
			}

			private void Client_OnNewSubscriber(object sender, OnNewSubscriberArgs e) {
				if (Program.config.format_onsub != null) {
					string msg;
					if (Program.config.format_onsubprime != null && e.Subscriber.SubscriptionPlan == SubscriptionPlan.Prime) {
						msg = String.Format(Program.config.format_onsubprime, e.Subscriber.DisplayName);
					} else { 
						msg = String.Format(Program.config.format_onsub, e.Subscriber.DisplayName);
					}

					client.SendMessage(e.Channel, msg);
					parent.callback(new Message(msg));
				}
			}

			public void Dispose() {
				client.Disconnect();
			}
		}

		internal class LiveMonitor : IDisposable {
			public Streamer parent;
			public LiveStreamMonitorService Monitor;
			public TwitchAPI API;
			public FollowerService Service;

			public LiveMonitor(string clientid, string access_token, string channel) {
				Task.Run(() => ConfigLiveMonitorAsync(clientid, access_token, channel));
			}

			private async Task ConfigLiveMonitorAsync(string clientid, string access_token, string channel) {
				API = new TwitchAPI();

				API.Settings.ClientId = clientid;
				API.Settings.AccessToken = access_token;

				Monitor = new LiveStreamMonitorService(API, 60);

				Monitor.SetChannelsByName(new List<string> { channel });
				/*
				Monitor.OnStreamOnline += Monitor_OnStreamOnline;
				Monitor.OnStreamOffline += Monitor_OnStreamOffline;
				Monitor.OnStreamUpdate += Monitor_OnStreamUpdate;

				Monitor.OnServiceStarted += Monitor_OnServiceStarted;
				Monitor.OnChannelsSet += Monitor_OnChannelsSet;
				*/
				Service = new FollowerService(API);
				if (Program.config.format_onfollow != null) {
					Service.OnNewFollowersDetected += Monitor_onNewFollowersDetected;
				}

				Service.Start();
				Monitor.Start(); //Keep at the end!

				await Task.Delay(-1);
			}

			private void Monitor_onNewFollowersDetected(object sender, OnNewFollowersDetectedArgs e) {
				foreach (var i in e.NewFollowers) {
					string msg = String.Format(Program.config.format_onfollow, i.FromUserName);
					parent.bot.client.SendMessage(e.Channel, msg);
					parent.callback(new Message(msg));
				}
			}

			private void Monitor_OnStreamOnline(object sender, OnStreamOnlineArgs e) {
				throw new NotImplementedException();
			}

			private void Monitor_OnStreamUpdate(object sender, OnStreamUpdateArgs e) {
				throw new NotImplementedException();
			}

			private void Monitor_OnStreamOffline(object sender, OnStreamOfflineArgs e) {
				throw new NotImplementedException();
			}

			private void Monitor_OnChannelsSet(object sender, OnChannelsSetArgs e) {
				throw new NotImplementedException();
			}

			private void Monitor_OnServiceStarted(object sender, OnServiceStartedArgs e) {
				throw new NotImplementedException();
			}

			public void Dispose() {
				try {
					Service.Stop();
				} catch (Exception e) {
					Console.WriteLine("Service.Stop(); failed");
				}
				try {
					Monitor.Stop();
				} catch (Exception) {
					Console.WriteLine("Monitor.Stop(); failed");
				}
			}
		}

		private Action<Message> callback;
		public Bot bot;
		public LiveMonitor monitor;
		
		public Streamer(Action<Message> callback) {
			this.callback = callback;
			string username = Program.config.username;
			string access_token = Program.config.access_token;
			string channel = username; //Yeah lets not allow that
			string clientid = Program.config.clientid;

			if (username == null || access_token == null || clientid == null) {
				throw new Exception("Streamer config entry missing!");
			}
			
			bot = new Bot(username, access_token, channel);
			monitor = new LiveMonitor(clientid, access_token, channel);

			Console.WriteLine("started them");

			bot.parent = this;
			monitor.parent = this;
		}

		public void Dispose() {
			bot.Dispose();
			monitor.Dispose();
		}
	}
}
