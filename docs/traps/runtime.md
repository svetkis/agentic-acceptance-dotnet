# Runtime Traps

> Problems that surface only in production metrics or data: tail latency, leaked PII.


## P50 vs Max
### Scenario

The agent makes a perf commit and writes: "p99 latency: 2727ms → 209ms (13x improvement)". A beautiful number. But:

- **P50 = experience of a typical user** (median request)
- **Max = experience of the most unlucky user** (worst case)
- **P99 = 1% of users get THIS**

A query that "on average" responds in 6ms can hang for 4.4 seconds. The agent optimizes P50, but tail latency remains catastrophic.

### Data from Practice

#### Before Optimization (Spike test)

| Metric | Global | view_profile | view_slots |
|--------|--------|-------------|------------|
| **P50** | 5.94ms | 0.28ms | 5.4ms |
| **P99** | 1625ms | 868ms | 818ms |
| **Max** | **4400ms** | **3375ms** | **1667ms** |
| **P50→Max** | **741x** | **12057x** | **308x** |

**view_profile:** P50 = 0.28ms (lightning fast!), Max = 3375ms (3.4 seconds). Difference of **12 thousand times**.

#### After Optimization (projections + ExecuteUpdateAsync)

| Metric | Global | view_profile | view_slots |
|--------|--------|-------------|------------|
| **P50** | 0.48ms | 0.26ms | 0.18ms |
| **P99** | 24ms | 15ms | 9.9ms |
| **Max** | **157ms** | **135ms** | **101ms** |

#### Comparison

| Metric | Before | After | Improvement |
|--------|--------|-------|-------------|
| P50 | 5.94ms | 0.48ms | 12x |
| P99 | 1625ms | 24ms | **67x** |
| Max | 4400ms | 157ms | **28x** |

**Key:** P50 improved 12x, while P99 improved 67x. Tail latency is a different world.

### Why This Is Dangerous

The agent shows beautiful P50/P99, but:
- The user can hit tail latency
- Concurrent access (booking attack) shows different behavior
- Read-path optimization can break write-path (see `docs/traps/testing.md#silent-breakdown`)

### Patterns from the Data

#### 1. Divergence Grows with Load

| Scenario | RPS | P50→Max |
|----------|-----|---------|
| Baseline | 5 | 20x |
| Peak | 30 | 49x |
| Stress | 50→300 | 261x |
| Spike (before optimization) | burst 300 | **741x** |

#### 2. view_profile — Always the Most Problematic

Reason: `.Include()` chains loaded workspace + subscriptions + categories + services. After replacing with `.Select()` projections — Max dropped from 3375ms to 135ms.

#### 3. Write-path Is More Stable Than Read-path

`create_booking` shows less divergence (8-11x). The unique index makes the query predictable — either INSERT or 409 Conflict.

### Solution

1. **NBomber scenarios** — always look at $Max$, not just P50/P99
2. **Read + Write mix** — run both paths under load simultaneously
3. **Concurrent booking attack** — check race conditions under load
4. **AGENTS.md rule:** after a perf commit — check write-path and Max latency

### Pattern

See `tests/patterns/LoadTest.cs`


## Log Leak
### Scenario

The agent adds logging for debugging and doesn't think about sensitive data:

```csharp
// Agent: "I'll add a log to see why it fails"
_logger.LogInformation("User {Email} failed login from {Ip}", user.Email, ip);

// Or even worse — string interpolation:
_logger.LogError($"Payment failed for {order.Phone}, card: {order.CardLast4}");
```

### Consequences

- Emails, phones, IP addresses leak into the logging system (Elastic, Kibana, Seq)
- Logs are often more widely accessible than the DB (SRE, support, external systems)
- GDPR / compliance violation — fines
- Tokens and session IDs in logs = potential session compromise

### Why Standard Layers Don't Catch It

| Layer | Why it doesn't catch |
|-------|----------------------|
| Compiler | `LogInformation(string, object[])` — valid signature |
| Architecture | NetArchTest doesn't see string argument contents |
| Tests | Unit tests check logic, not what goes into `ILogger` |
| Code Review | The agent-reviewer sees "logging added" and doesn't check arguments |
| E2E | Application works, logs are written |

### Solution

#### Level 1. Compile-time guard (Roslyn / Analyzer)

Custom `DiagnosticAnalyzer` forbids `Log*` with interpolation (`$"..."`) and marked types:

```csharp
// Analyzer emits warning/error:
_logger.LogError($"Failed for {user.Email}"); // ERROR: interpolated string in logging
```

#### Level 2. Attribute-driven inventory (Ratchet)

All PII fields are marked with `[SensitiveData]`:

```csharp
public record UserDto(
    [SensitiveData] string Email,
    [SensitiveData] string Phone,
    string Name
);
```

Arch test checks:
1. All properties with `*Email*`, `*Phone*`, `*Password*` in the name have `[SensitiveData]`
2. The number of `[SensitiveData]` properties does not decrease (ratchet)
3. `Log*` calls do not pass parameters of types containing `[SensitiveData]`

#### Level 3. Runtime redaction (Serilog / Middleware)

```csharp
// Serilog destructuring policy
public class SensitiveDataDestructuringPolicy : IDestructuringPolicy
{
    public bool TryDestructure(object value, ILogEventPropertyValueFactory propertyFactory,
        out LogEventPropertyValue result)
    {
        var type = value.GetType();
        var properties = type.GetProperties()
            .Where(p => p.GetCustomAttribute<SensitiveDataAttribute>() != null);

        // Redact marked properties
        ...
    }
}
```

### Pattern

See `tests/patterns/PiiGuardTest.cs` and `docs/solutions/ai-patterns.md` (pattern #9)
