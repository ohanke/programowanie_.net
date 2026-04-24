using System.Threading;

namespace SharedLogic
{
    public class GlobalSolution
    {
        private double _bestValue = double.MaxValue;
        private readonly ReaderWriterLockSlim _cacheLock = new ReaderWriterLockSlim();

        public double GetBestValue()
        {
            _cacheLock.EnterReadLock();
            try
            {
                return _bestValue;
            }
            finally
            {
                _cacheLock.ExitReadLock();
            }
        }

        public void UpdateBestValue(double newValue)
        {
            _cacheLock.EnterWriteLock(); 
            try
            {
                if (newValue < _bestValue)
                {
                    _bestValue = newValue;
                }
            }
            finally
            {
                _cacheLock.ExitWriteLock();
            }
        }
    }
}