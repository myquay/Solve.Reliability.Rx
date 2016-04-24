# Solve.Reliability.Rx

This library adds support for the circuit breaker pattern with reactive extensions .NET

It's designed to be super simple and lightweight.

##Usage

```csharp
 sourceObservable
     .RecoverWith(backupObservable, "circuit-breaker-name")
     .Subscribe(...);
```

##Advanced options

Setting the circuit breaker policy for a given circuit breaker

```csharp
  CircuitBreaker
    .GetInstance("circuit-breaker-name")
    .ApplyPolicy(new CircuitBreakerPolicy{
      CircuitResetTimeout = TimeSpan.FromMilliseconds(100), //Amount to time before shifting from Open to HalfOpen
      InvocationTimeout = TimeSpan.FromSeconds(1),          //Amount of time before an observable timesout
      MaxErrors = 3,                                        //Max times an observable can fail before the breaker is tripped
      RequiredSuccessfulCallsToClose = 1                    //Number of sucessful runs beofre shifting from HalfOpen to Closed
    });
```
