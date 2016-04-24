using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Solve.Reliability.Rx.Infrastructure
{
    public class CircuitBreakerPolicy
    {
        
        private static readonly Lazy<CircuitBreakerPolicy> _defaultPolicy = new Lazy<CircuitBreakerPolicy>(() => new CircuitBreakerPolicy
        {
             CircuitResetTimeout = TimeSpan.FromSeconds(10),
             InvocationTimeout = TimeSpan.FromSeconds(2),
             MaxErrors = 3,
             RequiredSuccessfulCallsToClose = 1
        });

        /// <summary>
        /// Get or set the default policy used by new circuit breakers
        /// </summary>
        public static CircuitBreakerPolicy Default
        {
            get { return _defaultPolicy.Value; }
            set
            {
                _defaultPolicy.Value.CircuitResetTimeout = value.CircuitResetTimeout;
                _defaultPolicy.Value.InvocationTimeout = value.InvocationTimeout;
                _defaultPolicy.Value.MaxErrors = value.MaxErrors;
                _defaultPolicy.Value.RequiredSuccessfulCallsToClose = value.RequiredSuccessfulCallsToClose;
            }
        }

        /// <summary>
        /// Number of consecutive errors that wil trip the ciruit
        /// </summary>
        public int MaxErrors { get; set; }

        /// <summary>
        /// Number of successful calls in the half-open state to close
        /// </summary>
        public int RequiredSuccessfulCallsToClose { get; set; }

        /// <summary>
        /// Length of time before the circuit resets
        /// </summary>
        public TimeSpan CircuitResetTimeout { get; set; }

        /// <summary>
        /// Length of time to wait before tripping the circuit on a long running request
        /// </summary>
        public TimeSpan InvocationTimeout { get; set; }
        
    }
}
