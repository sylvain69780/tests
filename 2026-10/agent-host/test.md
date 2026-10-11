Normalized relational databases remain well suited to modern architectures. Their role is not to be the application's only storage system, but to provide a **reliable source of truth** for data that must remain consistent.

### Where they are especially useful

- **Structured business data**: customers, orders, payments, inventory, or accounts. Related tables and integrity constraints reduce duplication and prevent inconsistencies.
- **Transactional operations**: with ACID transactions, a multi-step operation—for example, recording an order and reducing inventory—can either succeed in full or be rolled back in full.
- **Relational queries and reporting**: SQL makes it possible to filter, join, and aggregate data precisely.
- **Controlled model evolution**: migrations, foreign keys, and constraints make changes explicit and verifiable.

### How to integrate them into modern architectures

A normalized database can sit behind application services or APIs. The service applies business rules, while the database guarantees persistence and integrity. To meet scalability or performance needs, it can be complemented by:

- **Caches** for frequently read data;
- **Read replicas** to distribute queries;
- **Indexes** and partitioning to speed up access;
- **Materialized views** or analytical databases for heavy reporting;
- **Denormalized projections**, updated through events, for read-optimized screens.

This is one of the ideas behind **CQRS**: keep writes in a consistent relational model, then build read models suited to specific use cases. In an event-driven architecture, a service can also publish an event after a transaction is committed—often using the *transactional outbox* pattern to avoid losing the event.

### A trade-off, not an either-or choice

Normalization limits anomalies and inconsistent updates, but some reads may require several joins. You can denormalize **in targeted places**—in a cache or projection, for example—instead of giving up the consistency of the primary model.

In practice, PostgreSQL, MySQL, and SQL Server integrate with containers, cloud platforms, observability tools, and modern data pipelines. The choice therefore depends less on the age of the relational model than on the requirements: transactional consistency, query patterns, volume, latency, and scaling strategy.