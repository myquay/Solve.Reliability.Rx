using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Solve.Reliability.Rx.Infrastructure.States
{
    public class CircuitBreakerStateHalfOpen : ICircuitBreakerState
    {
        private CircuitBreaker _circuitBreaker;
        private int _successes;

        public CircuitBreakerStateHalfOpen(CircuitBreaker circuitBreaker)
        {
            _circuitBreaker = circuitBreaker;
        }

        public void Enter()
        {
            _successes = 0;
        }

        public void OperationFailed()
        {
            _circuitBreaker.OpenCircuit(this);
        }

        public void OperationSucceeded()
        {
            if (Interlocked.Increment(ref _successes) == _circuitBreaker.Policy.RequiredSuccessfulCallsToClose)
            {
                _circuitBreaker.CloseCircuit(this);
            }
        }

        public bool IsTripped
        {
            get
            {
                return false;
            }
        }

        public CircuitBreakerState State
        {
            get
            {
                return CircuitBreakerState.HalfOpen;
            }
        }
    }
}
