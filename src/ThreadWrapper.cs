using System.Threading;

namespace PythonCSharpTranslator
{
    public abstract class ThreadWrapper
    {
        private Thread _thread;
        private bool _running = false;

        protected ThreadWrapper()
        {
            _thread = new Thread(() =>
            {
                _running = true;
                while (_running)
                {
                    this.DoWork();
                }
            });
        }
        
        public void Start() => _thread.Start();
        public void Join() => _thread.Join();
        public bool IsAlive => _thread.IsAlive;
        public void Stop() => _running = false;

        protected abstract void DoWork();
    }
}