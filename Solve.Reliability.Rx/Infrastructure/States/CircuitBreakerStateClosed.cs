using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Solve.Reliability.Rx.Infrastructure.States
{
    public class CircuitBreakerStateClosed : ICircuitBreakerState
    {
        private CircuitBreaker _circuitBreaker;
        private int _errors;

        public CircuitBreakerStateClosed(CircuitBreaker circuitBreaker)
        {
            _circuitBreaker = circuitBreaker;
        }
        
        public void Enter()
        {
            _errors = 0;
        }

        public void OperationFailed()
        {
            if (Interlocked.Increment(ref _errors) == _circuitBreaker.Policy.MaxErrors)
            {
                _circuitBreaker.OpenCircuit(this);
            }
        }

        public void OperationSucceeded()
        {
            _errors = 0;
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
                return CircuitBreakerState.Closed;
            }
        }
    }
}
