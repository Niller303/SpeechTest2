using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpeechTest2 {
	class Message {
		public string User { private set; get; }
		public string Text { private set; get; }

		public bool IsSubscriber { private set; get; }
		public bool IsModerator { private set; get; }
		public bool IsBroadcaster { private set; get; }

		public Message(string User, string Text, bool IsSub, bool IsMod, bool IsBroad) {
			this.User = User;
			this.Text = Text;
			this.IsSubscriber = IsSub;
			this.IsModerator = IsMod;
			this.IsBroadcaster = IsBroad;
		}
		public Message(string User, string Text) {
			this.User = User;
			this.Text = Text;
		}
		public Message(string Text) {
			this.User = null;
			this.Text = Text;
		}
	}
	class CommandMessage : Message {
		public string Command { private set; get; }

		public CommandMessage(string User, string Text, string Command, bool IsSub, bool IsMod, bool IsBroad) : base(User, Text, IsSub, IsMod, IsBroad) {
			this.Command = Command;
		}
		public CommandMessage(Message m, string Command) : base(m.User, m.Text, m.IsSubscriber, m.IsModerator, m.IsBroadcaster) {
			this.Command = Command;
		}
		public void SetCommand(string Command) {
			this.Command = Command;
		}
	}
}
