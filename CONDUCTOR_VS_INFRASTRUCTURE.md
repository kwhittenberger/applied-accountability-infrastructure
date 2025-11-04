# Conductor vs Infrastructure Libraries - Decision Guide

## Quick Decision Matrix

| **You Need...** | **Use This** | **Why** |
|----------------|-------------|---------|
| Job scheduling (cron, recurring tasks) | **Conductor** | Production-ready with Quartz.NET, clustering, admin UI |
| Background task processing | **Conductor** | Built-in job queue with retry/timeout handling |
| Distributed workflows / Sagas | **Conductor** | Saga orchestration engine with compensation transactions |
| Message publishing/consuming | **Conductor** | MassTransit + RabbitMQ with dead letter queue handling |
| Event-driven architecture | **Conductor** | Message bus integration with saga coordination |
| Simple audit logging | **EventStore** (when built) | Lightweight event persistence without orchestration |
| Change tracking / history | **EventStore** (when built) | Append-only event storage for compliance |
| Domain events (no workflows) | **EventStore** (when built) | Event persistence without message bus coupling |
| Data access patterns | **Data** | Repository pattern, EF Core, Dapper helpers |
| Authentication / Authorization | **Security** | JWT, OAuth, encryption, PII protection |
| File storage | **Files** | Local, MinIO, or optional cloud storage |
| Email / SMS / Push notifications | **Notifications** | Multi-channel notification system |
| PDF / Excel / CSV reports | **Reporting** | Report generation and templating |
| Full-text search | **Search** | PostgreSQL FTS, Elasticsearch, OpenSearch |
| Rate limiting / API versioning | **ApiGateway** | API management patterns |
| Third-party integrations | **Integration** | Stripe, Plaid, Auth0, etc. |

---

## Detailed Scenarios

### Scenario 1: Data Sync Job

**Requirements:**
- Import data from external API every 2 hours
- Retry on failures
- Track execution history
- Prevent concurrent runs

**Solution:** ✅ **Conductor**

**Why:**
- Job scheduling with cron expressions
- Built-in retry policies and timeout handling
- Execution history tracking
- Concurrent execution control
- Admin UI for monitoring and manual triggering

---

### Scenario 2: Order Processing Workflow

**Requirements:**
- Reserve inventory
- Charge payment
- If payment fails, release inventory
- Send confirmation email
- Multiple services involved

**Solution:** ✅ **Conductor (Sagas)**

**Why:**
- Long-running distributed workflow
- Compensation transactions (rollback on failure)
- State persistence across service boundaries
- Event-driven coordination

---

### Scenario 3: User Audit Trail

**Requirements:**
- Log all user actions for compliance
- Query user history by date
- No workflows or distributed coordination needed

**Solution:** ✅ **EventStore** (when available)

**Why:**
- Simple append-only event storage
- No need for message bus, jobs, or sagas
- Lightweight and focused on event persistence
- Can query events by stream/date

**Fallback:** Use Conductor if you also need to react to events with workflows

---

### Scenario 4: User Authentication

**Requirements:**
- JWT token generation
- Password hashing
- Role-based access control
- Secure key management

**Solution:** ✅ **Security Library**

**Why:**
- General-purpose security utilities
- No scheduling or workflows involved
- Reusable across any application type

---

### Scenario 5: Report Generation on Schedule

**Requirements:**
- Generate sales report every Monday at 9 AM
- Export to PDF and email to managers
- Track when reports were sent

**Solution:** ✅ **Conductor + Reporting + Notifications**

**Why:**
- **Conductor**: Schedule the job, track execution
- **Reporting**: Generate PDF
- **Notifications**: Send email
- Conductor orchestrates the workflow

---

### Scenario 6: File Upload Service

**Requirements:**
- Upload files to storage
- Generate thumbnails
- Validate file types
- Track file metadata

**Solution:** ✅ **Files Library** (+ optional Conductor for async processing)

**Why:**
- **Files Library**: Core upload/storage functionality
- **Optional Conductor**: If thumbnail generation is long-running, use Conductor to queue the work

---

## When to Combine Multiple Libraries

Most real-world applications will use **both** Conductor and Infrastructure libraries together.

### Example: E-commerce Platform

```
┌─────────────────────────────────────────────────────────────┐
│                    E-commerce Application                    │
├─────────────────────────────────────────────────────────────┤
│                                                               │
│  Conductor (Orchestration Layer)                             │
│  ├─ Order processing saga                                    │
│  ├─ Inventory sync job (every hour)                          │
│  ├─ Abandoned cart reminders (daily)                         │
│  ├─ Low stock alerts                                         │
│  └─ Monthly sales report generation                          │
│                                                               │
│  Infrastructure.Data (Data Access)                           │
│  ├─ Order repository                                         │
│  ├─ Product repository                                       │
│  ├─ Customer repository                                      │
│  └─ Soft delete & audit fields                               │
│                                                               │
│  Infrastructure.Security (Security)                          │
│  ├─ JWT authentication                                       │
│  ├─ Role-based authorization                                 │
│  ├─ Payment data encryption                                  │
│  └─ PII protection                                           │
│                                                               │
│  Infrastructure.Files (Storage)                              │
│  ├─ Product images                                           │
│  ├─ Invoice PDFs                                             │
│  └─ User uploads                                             │
│                                                               │
│  Infrastructure.Notifications (Communication)                │
│  ├─ Order confirmation emails                                │
│  ├─ Shipping notifications                                   │
│  ├─ SMS alerts for delivery                                  │
│  └─ Push notifications                                       │
│                                                               │
│  Infrastructure.Reporting (Reports)                          │
│  ├─ Sales reports (PDF)                                      │
│  ├─ Inventory exports (Excel)                                │
│  └─ Customer analytics                                       │
│                                                               │
│  Infrastructure.Search (Search)                              │
│  ├─ Product search                                           │
│  ├─ Customer search                                          │
│  └─ Order search                                             │
│                                                               │
└─────────────────────────────────────────────────────────────┘
```

**Flow Example: Order Placement**

1. **Security**: User authenticates with JWT
2. **Data**: Load product and inventory from database
3. **Conductor**: Start order processing saga
   - Reserve inventory
   - Charge payment (via Integration library - Stripe)
   - If payment succeeds:
     - **Data**: Create order record
     - **Notifications**: Send order confirmation email
     - **Reporting**: Generate invoice PDF
     - **Files**: Store invoice in blob storage
   - If payment fails:
     - Compensation: Release inventory reservation
     - **Notifications**: Send payment failed email
4. **EventStore** (optional): Log all order events for audit trail

---

## Key Rule of Thumb

> ### **Scheduling / Workflows / Coordination → Conductor**
>
> ### **General-Purpose Utilities → Infrastructure Libraries**

---

## Anti-Patterns to Avoid

### ❌ DON'T: Build your own job scheduler when Conductor exists
```csharp
// Bad: Reinventing the wheel
public class CustomJobScheduler
{
    // Implementing cron parsing, retry logic, execution tracking...
}
```

**Use Conductor instead** - it's production-ready with all these features.

---

### ❌ DON'T: Use Conductor for simple utilities
```csharp
// Bad: Using Conductor just for JWT generation
await conductor.ScheduleJob("GenerateJWT", "* * * * *");
```

**Use Security library instead** - Conductor is overkill for stateless utilities.

---

### ❌ DON'T: Mix concerns in a single library
```csharp
// Bad: Putting everything in one package
AppliedAccountability.Everything
  ├─ Data access
  ├─ Job scheduling
  ├─ Authentication
  ├─ File storage
  └─ Email sending
```

**Follow separation of concerns** - use focused libraries for each concern.

---

## Still Not Sure?

Ask yourself these questions:

1. **Does it need to run on a schedule?** → Conductor
2. **Does it involve multiple steps across services?** → Conductor (Sagas)
3. **Does it need to publish/consume messages?** → Conductor
4. **Is it a general-purpose utility (auth, storage, etc.)?** → Infrastructure
5. **Is it just storing events for audit?** → EventStore (when available)

---

## Summary

- **Conductor**: Orchestration, scheduling, workflows, messaging, sagas
- **Infrastructure Libraries**: Reusable utilities for common application needs
- **Most apps use both**: Conductor for coordination + Infrastructure for utilities
- **No hard cloud dependencies**: Self-hosted solutions first, cloud optional

For detailed feature lists, see [INFRASTRUCTURE_ROADMAP.md](./INFRASTRUCTURE_ROADMAP.md)
