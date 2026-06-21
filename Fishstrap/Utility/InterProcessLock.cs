using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fishstrap.Utility
{
    public class InterProcessLock : IDisposable
    {
        public Mutex Mutex { get; private set; }

        public bool IsAcquired { get; private set; }

        public InterProcessLock(string name) : this(name, TimeSpan.Zero) { }

        public InterProcessLock(string name, TimeSpan timeout)
        {
            Mutex = new Mutex(false, "Fishstrap-" + name);

            try
            {
                IsAcquired = Mutex.WaitOne(timeout);
            }
            catch (AbandonedMutexException)
            {
                IsAcquired = true;
            }
        }

        public void Dispose()
        {
            if (IsAcquired)
            {
                Mutex.ReleaseMutex();
                IsAcquired = false;
            }

            GC.SuppressFinalize(this);
        }
    }
}
