using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Solve.Reliability.Rx.Infrastructure.States
{
    public class CircuitBreakerStateOpen : ICircuitBreakerState
    {
        private CircuitBreaker _circuitBreaker;
        private Timer _timer;

        public CircuitBreakerStateOpen(CircuitBreaker circuitBreaker)
        {
            _circuitBreaker = circuitBreaker;
        }

        public void Enter()
        {
            _timer = new Timer(_=>
            {
                _circuitBreaker.HalfCloseCircuit(this);
            }, _timer, (int)_circuitBreaker.Policy.CircuitResetTimeout.TotalMilliseconds, Timeout.Infinite);
        }

        public void OperationFailed()
        {}

        public void OperationSucceeded()
        {}

        public bool IsTripped
        {
            get
            {
                return true;
            }
        }

        public CircuitBreakerState State
        {
            get
            {
                return CircuitBreakerState.Open;
            }
        }
    }
}
