# Research Notes: Text-to-SQL as Explainability and Evaluation

## Motivation

The practical challenge in Text-to-SQL is not only producing SQL. A generated query can be syntactically valid and still be untrustworthy if the user cannot inspect how the system selected tables, columns, filters, joins, and aggregations.

This project treats Text-to-SQL as a human-verification problem:

- The model should be grounded in a known schema.
- The system should reject unsafe or unsupported SQL.
- Execution results should carry trace metadata.
- Failures should be collected and turned into regression cases.

## Engineering Observation

The current codebase uses a schema-grounded plan-first architecture:

1. A user asks a natural-language question.
2. The backend resolves screen/type context and conversation state.
3. The prompt builder injects schema context, value examples, few-shot examples, and prior turns.
4. The LLM returns a structured semantic plan or a clarification request.
5. The backend renders SQL from the plan.
6. Validators check semantic intent, SQL safety, object policy, shape, and result contract.
7. The query runs against SQL Server only after passing the guards.
8. The response includes result trace fields for human review.

This design gives a useful research hook: the explanation is not only a post-hoc natural-language summary. It is a structured trace of route, plan, validation, SQL rendering, execution, and result contract decisions.

## Research Questions

### 1. Human-verifiable reasoning trace

How much trace is enough for a human to verify a generated query without exposing private schema details?

Possible trace units:

- selected source family
- selected result shape
- route confidence
- applied filters
- row and column axes
- measure intent
- validation status
- missing or unsupported schema objects
- safe trace string

### 2. Schema-grounded failure modes

What failures remain even when the model is grounded in a schema catalog?

Examples:

- wrong source selection
- semantically similar but invalid field
- invalid join path
- unsupported aggregation
- ambiguous user phrase
- correct SQL that returns a misleading empty result

### 3. Execution feedback and trust

Can validation and execution feedback improve trust?

Candidate feedback signals:

- SQL safety rejection
- result shape mismatch
- zero-row or suspiciously sparse result
- missing requested axis
- invalid filter value
- clarification generated from validation failure

### 4. Evaluation beyond exact match

Exact-match SQL is too narrow for production-style Text-to-SQL. A safer evaluation should include:

- executable SQL rate
- read-only safety pass rate
- schema grounding accuracy
- result contract pass rate
- clarification quality
- failure trace usefulness
- regression stability across repeated runs
- human debuggability score

## Current Implementation vs Planned Research Work

Implemented in the codebase:

- schema-aware prompt construction
- semantic plan response schema
- direct model-SQL blocking
- server-side SQL rendering
- read-only SQL validation
- execution preflight and row limiting
- result trace and trust contract
- failure collection and golden draft tooling
- regression-oriented test files

Planned research packaging:

- public mock-data demo
- benchmark suite without internal data
- trace-quality rubric
- explanation panel for non-engineer reviewers
- comparison against exact-match-only evaluation

## Publication Boundary

Public materials must use mock schema, dummy data, and placeholder configuration only. Real database identifiers, credentials, internal schema names, uploaded files, email exports, and private test data are outside the public boundary.

