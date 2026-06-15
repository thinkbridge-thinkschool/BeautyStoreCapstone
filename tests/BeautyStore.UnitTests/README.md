# BeautyStore.UnitTests

Fast, in-memory unit tests covering Domain and Application layer logic.

What lives here:
- Aggregate invariant tests: `Order`, `OrderLine`, state transitions.
- Value Object equality tests: `Money`, `Address`, `Quantity`.
- Domain Event publication tests: verify the right events are raised on aggregate mutations.
- Application service / use-case tests with mocked repository and messaging ports.

No database, no HTTP — all dependencies are test doubles.
