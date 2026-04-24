using System;
using System.Threading;

namespace SharedLogic
{
    public class CriticalResource
    {
        private int _currentAccessCount = 0;
        private int _maxAccessLimit;
        private readonly object _lockObject = new object();

        public CriticalResource(int maxLimit)
        {
            _maxAccessLimit = maxLimit;
        }

        public string DoSomething(int taskId)
        {
            try
            {
                lock (_lockObject)
                {
                    if (_currentAccessCount >= _maxAccessLimit)
                    {
                        throw new InvalidOperationException($"Limit exceeded by Task {taskId}!");
                    }
                    _currentAccessCount++;
                }

                Random rng = new Random();
                int duration = rng.Next(500, 1500);
                Thread.Sleep(duration);

                return $"Task {taskId} success ({duration}ms).";
            }
            finally
            {
                lock (_lockObject)
                {
                    _currentAccessCount--;
                }
            }
        }
    }
}