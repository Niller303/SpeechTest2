using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpeechTest2.SpeechSynths {
	class PollyTTS : DefaultTTS {
		public override string[] GetVoices() {
			throw new NotImplementedException();
		}

		public override DefaultConfig Prep(DefaultConfig cfg) {
			throw new NotImplementedException();
		}

		public override void Skip() {
			throw new NotImplementedException();
		}

		public override void Speak(DefaultConfig cfg) {
			throw new NotImplementedException();
		}
	}
}
