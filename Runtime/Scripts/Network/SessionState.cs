namespace Majingari.Network {
    public class SessionState {
        public string sessionName;
        public int maxPlayer;
        public int mapIndex;

        public SessionState(string sessionName) {
            this.sessionName = sessionName == "" ? $"Session {UnityEngine.Random.Range(1, 9999)}" : sessionName;
            this.maxPlayer = 5;
            this.mapIndex = 1;

            //ServiceLocator.Register(this.GetType(), this);
        }
    }
}