### Big picture (how they fit together)
- **Unit tests**: verify *one class/function* in isolation (fast, no network/db).
- **Integration tests**: verify *multiple components* work together (real db/filesystem/http, slower).
- **Contract tests (Pact)**: verify *API compatibility* between consumer and provider without deploying both together.
- **PR validations**: the automated checks that run on every PR to prevent breaking changes from merging.

---

### Unit testing (step-by-step)
1. **Pick the unit**: a single method/class (e.g., `OrderService` business logic).
2. **Isolate dependencies**: replace DB/HTTP/time with mocks/fakes (so failures point to your code, not the environment).
3. **Write the test** (Arrange / Act / Assert):
   - **Arrange**: create inputs + mock dependency behavior
   - **Act**: call the method
   - **Assert**: verify returned value / thrown exception / state changes
4. **Run locally**:
   - `dotnet test`
5. **Keep them fast**: aim for milliseconds; run on every PR.

**What unit tests catch**: logic bugs, edge cases, regressions in small pieces of code.

---

### Integration testing (step-by-step)
1. **Pick a real flow**: “API endpoint + database” or “service + message broker”, etc.
2. **Use real dependencies** (or close equivalents):
   - real SQLite/Postgres, real HTTP pipeline, real serialization, etc.
3. **Control test data**:
   - create known records before the test
   - clean up after (or use disposable DB)
4. **Run the system under test**:
   - either in-memory test server or start the API as a process/container
5. **Assert end-to-end behavior**:
   - HTTP response + DB state + side effects
6. **Run locally + in CI**:
   - `dotnet test` (often slower and may need environment variables)

**What integration tests catch**: wiring/config issues, ORM mapping problems, serialization mismatches, routing/auth middleware mistakes.

---

### Contract testing (Pact) (step-by-step)
Contract tests split into **consumer** and **provider** responsibilities.

#### A) Consumer contract tests (generate pact files)
1. **Consumer defines expectations** of provider responses:
   - endpoint, request shape, response status/body/headers
2. **Run consumer tests** → pact JSON files are generated into `tests/*/pacts/`.
3. **Publish pacts** to the broker so providers can fetch them:
   - `.\publish-pacts.ps1 -Version "<consumer-version>" -Branch "main"`
4. **Result**: broker now has “Consumer X expects Provider Y to behave like this”.

#### B) Provider verification tests (verify provider against the pact)
1. **Provider starts** (your tests start the API and point PactNet verifier at it).
2. **Verifier fetches pacts** from the broker.
3. **Provider state setup** runs (so the provider is in the right DB state).
4. **Verifier sends the pact requests** to the running provider.
5. **Verifier compares actual vs expected responses**.
6. **Publish verification results** back to the broker (links provider version to contracts).
7. **Result**: broker knows “Provider version V satisfies contracts”.

#### C) Can-I-Deploy check (release gate)
1. Call the broker “matrix” check for a participant+version.
2. Broker answers: **SAFE TO DEPLOY** only if all required verifications exist and are successful.

**What contract tests catch**: breaking API changes (fields renamed/removed, status codes changed, request/response shape drift) *before deployment*.

---

### PR validations (step-by-step)
A good PR validation pipeline usually runs in this order:

1. **Restore + build**  
   - ensure code compiles and dependencies restore cleanly
2. **Static checks** (optional but common)  
   - formatting, linting, analyzers
3. **Unit tests**  
   - fast feedback; should run on every PR
4. **Integration tests** (if you have them)  
   - can be PR-gated or nightly depending on runtime
5. **Contract flow** (if PR-gated)  
   - generate pacts (consumer) → publish (often to a branch tag)  
   - provider verification (sometimes on main only, sometimes in PR too)
6. **Security checks** (optional but common)  
   - dependency scanning, secret scanning
7. **Publish artifacts** (optional)  
   - only if all checks pass
8. **Branch policies** enforce: “PR cannot merge unless pipeline is green”.

**Practical guidance for your setup (self-hosted broker)**:
- Use **Basic Auth** (`PACT_BROKER_USERNAME` / `PACT_BROKER_PASSWORD`) in CI.
- Only use `PACT_BROKER_TOKEN` if you move to PactFlow.

---

If you tell me what you want your PR gate to be (unit-only vs unit+integration+contract), I can outline the recommended PR pipeline stages and what should run on PR vs what should run on main.