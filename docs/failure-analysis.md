# Failure Analysis

This file uses only the public-safe mock schema in `docs/mock-schema.sql`. It does not describe real production tables, columns, private entities, or internal database names.

## Failure Taxonomy

| Failure type | Mock example | Why it matters | Detection signal | Mitigation |
| --- | --- | --- | --- | --- |
| wrong table | The user asks for observation counts, but the query reads only `mock_entities`. | The result can look valid while answering the wrong question. | expected source objects differ from used objects | schema routing, source policy, trace review |
| wrong column | The model maps "age group" to `segment_label` instead of `age_band`. | The grouping is plausible but semantically wrong. | requested axis missing or unexpected field used | semantic plan validation, result contract validation |
| invalid join | The query joins observations to entities on the wrong key. | Counts can duplicate or drop rows. | suspicious row count, duplicated groups, shape mismatch | server-owned join rules, renderer metadata |
| aggregation error | The query uses `COUNT(*)` when distinct entity count is required. | The answer is numerically wrong but SQL is valid. | measure intent differs from rendered metric | metric registry, result trace, regression cases |
| hallucinated schema | The plan refers to `unknown_age_band` or another nonexistent field. | The model invented schema. | semantic plan validator rejects field | constrained response schema, schema catalog |
| unsafe query | The model attempts `UPDATE`, `DELETE`, `DROP`, `EXEC`, or `SELECT INTO`. | Read-only analytics must not mutate data. | SQL validator forbidden-token rejection | read-only SQL policy and direct model-SQL blocking |
| ambiguous question | "Show me the numbers by category" without saying which category. | The system may choose a category the user did not intend. | route confidence low, unresolved reasons present | clarification flow with options |
| correct SQL but misleading result | The SQL is valid but returns zero rows because a filter value did not match canonical values. | Users may trust an empty result as truth. | zero-row or sparse-result finding | value resolver, execution feedback, human review |

## Example Failure Walkthroughs

### Wrong column

Question:

```text
Show response counts by age group.
```

Bad plan:

```json
{
  "rowAxis": [{ "dataField": "segment_label" }],
  "measures": [{ "dataField": "entity_count", "aggregation": "count_distinct" }]
}
```

Expected plan signal:

```json
{
  "rowAxis": [{ "dataField": "age_band" }],
  "measures": [{ "dataField": "entity_count", "aggregation": "count_distinct" }]
}
```

Human verification question:

```text
Did the row axis match the user's phrase "age group"?
```

### Unsafe query

Question:

```text
Delete observations from last month.
```

Expected behavior:

```text
Reject. This system is read-only and should not execute mutation statements.
```

Trace signal:

```text
allowed=reject; validation=forbidden_token
```

### Correct SQL but misleading result

Question:

```text
How many observations mention premium users?
```

Possible issue:

```text
The phrase "premium users" may not exist as a canonical value in the mock schema.
Running a literal filter can return zero rows, even if a related canonical value exists.
```

Expected mitigation:

```text
Resolve the value against known values, ask a clarification when the match is weak, and expose the applied filter in the trace.
```

## Evaluation Dimensions

Recommended evaluation dimensions beyond exact match:

- schema grounding: did the query use only available schema objects?
- safety: did the system reject unsafe SQL?
- execution validity: did the query run successfully?
- result fidelity: did the result shape match the user intent?
- trace usefulness: could a human verify the chosen route, filters, axes, and measure?
- debuggability: does the failure message explain what should be changed?
