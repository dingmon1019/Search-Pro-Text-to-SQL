# Search-Pro — Text-to-SQL in Daily Use

> The model never writes SQL. It returns a structured plan, the server renders the SQL,
> and a read-only gate decides whether it runs.

LLM은 SQL을 직접 쓰지 않습니다. 계획(JSON)만 내고, SQL은 서버가 렌더링하며, read-only 게이트를 통과해야만 실행됩니다.

## Result in Production

| | Before | After |
| --- | --- | --- |
| Quote-data lookup, per case | about 40 min | about 4 min |
| Cases per year | — | about 490 |
| Failure types under regression tests | — | 8 |

Built and used inside 한국리서치. These are workflow-time figures, not model-accuracy figures;
this repository does not claim private production accuracy.

## My Role

I led the internal AI contest team as its only engineer: defined requirements with the
people who run the queries, then designed and built the system and shipped it into their daily work.

## Public Snapshot Scope

This repository is a sanitized snapshot. It keeps the mock schema, example queries,
failure taxonomy, and a small mock reference pipeline in `src/SearchProPublic`.

The full application is not here because production files contain internal database names,
schema identifiers, uploaded files and evaluation artifacts. A runnable Python
re-implementation of the trust pipeline is in
[searchpro-py-demo](https://github.com/dingmon1019/searchpro-py-demo).

How the system was hardened over its private development history (guardrail comparison and
failure coverage) is summarized in [docs/engineering-evidence.md](docs/engineering-evidence.md).

## System Flow

1. User question
2. Schema/context retrieval
3. Prompt construction
4. SQL generation via server-rendered semantic plan
5. SQL validation/safety check
6. Query execution
7. Result formatting
8. Human verification / feedback

In the analyzed local implementation, the LLM is constrained to produce a structured semantic plan. Direct model-authored SQL responses are blocked, and SQL is rendered inside the application from schema-grounded plan objects.

## Key Features

These features were verified in the original local source before sanitization. The public snapshot keeps only safe documentation and mock reference code.

- ASP.NET Core MVC backend for a natural-language query lab.
- Authenticated query endpoint that receives screen/type context, user question, prior conversation id, attachments, and template-assist metadata.
- Schema-aware prompt builder that injects schema catalog context, value catalogs, few-shot examples, conversation history, and attachment context.
- LLM client with structured JSON response schema for semantic plans.
- Model response parser that accepts `plan` or `clarify` and blocks direct `sql` responses.
- Server-side semantic SQL renderer that converts a validated semantic plan into SQL.
- SQL validation layer that restricts execution to read-only `SELECT` / `WITH ... SELECT` patterns, rejects forbidden tokens, checks allowed objects, and applies row limits.
- Query execution service with shape preflight and formatted result rows.
- Clarification path for ambiguous or unsupported questions.
- Result trace and trust contract fields that expose route status, route source, validation code, result contract code, selected shape, allowed execution level, axes, filters, and a safe trace.
- SQL viewing is gated by a session-backed unlock flow, instead of being exposed by default.
- Failure collection, saved conversation review, golden draft generation, and regression-oriented test assets exist for development and evaluation workflows.

## Open Questions

Questions this work left me with:

- How can Text-to-SQL systems expose enough reasoning trace for human verification?
- What SQL generation failures appear in schema-grounded enterprise DBs?
- Can validation, execution feedback, and explanation traces improve trust?
- How should Text-to-SQL be evaluated beyond exact-match accuracy?

The interesting research direction is not only whether a query returns the right answer, but whether the system can explain why it chose a source, which fields it used, what constraints were applied, why a query was rejected, and how a human can inspect the result safely.

## Failure Analysis

Failure modes are documented with a public-safe mock schema in [docs/failure-analysis.md](docs/failure-analysis.md).

Covered categories:

- wrong table
- wrong column
- invalid join
- aggregation error
- hallucinated schema
- unsafe query
- ambiguous question
- correct SQL but misleading result

## Example Queries

Public-safe example queries are listed in [docs/example-queries.md](docs/example-queries.md). They use only the mock schema from [docs/mock-schema.sql](docs/mock-schema.sql).

## Sanitized Reference Code

The public code sample in [src/SearchProPublic](src/SearchProPublic) demonstrates the trust boundary without internal data:

- mock schema catalog
- prompt preview construction
- simple semantic planner
- server-side SQL renderer
- read-only SQL validator
- mock execution response
- trace output for human verification

Run it locally with:

```powershell
dotnet run --project src/SearchProPublic/SearchProPublic.csproj -- "Show observation counts by age band and topic"
```

## Public Data and Security Notes

This repository should be published only as a sanitized portfolio snapshot.

Do not publish:

- real company database names, hosts, connection strings, credentials, or internal network addresses
- provider tokens or SQL-view unlock tokens
- private entity, employee, or personally identifying data
- production uploaded files, email exports, spreadsheets, logs, or debug artifacts
- real table names, real column names, or proprietary schema mappings

Use `.env.example`, `appsettings.example.json`, and `docs/mock-schema.sql` for public configuration and examples. Any local `appsettings*.json` file must be scrubbed or replaced with placeholders before GitHub publication.

## Not in This Repository Yet

Listed so nothing here is mistaken for current functionality:

- A public demo mode backed only by the mock schema.
- A human-readable query explanation panel that maps natural language phrases to schema fields.
- A formal evaluation dashboard comparing exact match, execution accuracy, safety rejection, trace quality, and debuggability.
- A reproducible benchmark package that can run without internal data.

## Documentation Map

- [docs/research-notes.md](docs/research-notes.md): research framing and evaluation ideas
- [docs/engineering-evidence.md](docs/engineering-evidence.md): quantitative evidence, guardrail comparison, and failure coverage
- [docs/failure-analysis.md](docs/failure-analysis.md): failure taxonomy using mock schema
- [docs/example-queries.md](docs/example-queries.md): public-safe query examples
- [docs/mock-schema.sql](docs/mock-schema.sql): mock schema and dummy data
- [.env.example](.env.example): placeholder environment configuration
- [appsettings.example.json](appsettings.example.json): placeholder JSON configuration
- [src/SearchProPublic](src/SearchProPublic): mock reference pipeline
