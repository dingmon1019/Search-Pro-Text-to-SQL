# Example Queries

These examples use only the mock schema in `docs/mock-schema.sql`.

## Example 1: Grouped Count

Natural language:

```text
How many observations are there by region?
```

Expected semantic plan signals:

```json
{
  "expectedShape": { "kind": "grouped" },
  "rowAxis": [{ "dataField": "region_name" }],
  "measures": [{ "dataField": "entity_count", "aggregation": "count_distinct" }]
}
```

Public-safe SQL pattern:

```sql
SELECT
    e.region_name,
    COUNT(DISTINCT e.entity_id) AS entity_count
FROM mock_entities e
JOIN mock_observations o ON o.entity_id = e.entity_id
GROUP BY e.region_name
ORDER BY e.region_name;
```

Human verification:

```text
The trace should show region_name as the row axis and entity_count as the measure.
```

## Example 2: Crosstab Intent

Natural language:

```text
Show observation counts by age band and topic.
```

Expected semantic plan signals:

```json
{
  "expectedShape": { "kind": "crosstab" },
  "rowAxis": [{ "dataField": "age_band" }],
  "columnAxis": [{ "dataField": "topic_name" }],
  "measures": [{ "dataField": "entity_count", "aggregation": "count_distinct" }]
}
```

Public-safe SQL pattern:

```sql
SELECT
    e.age_band,
    o.topic_name,
    COUNT(DISTINCT e.entity_id) AS entity_count
FROM mock_entities e
JOIN mock_observations o ON o.entity_id = e.entity_id
GROUP BY e.age_band, o.topic_name
ORDER BY e.age_band, o.topic_name;
```

Human verification:

```text
The result should preserve both requested axes. If one axis disappears, the result contract should fail.
```

## Example 3: Filtered Count

Natural language:

```text
How many unique entities selected item A in 2026?
```

Expected semantic plan signals:

```json
{
  "filters": [
    { "dataField": "item_name", "operator": "equals", "normalizedValue": "Item A" },
    { "dataField": "observed_year", "operator": "equals", "normalizedValue": "2026" }
  ],
  "measures": [{ "dataField": "entity_count", "aggregation": "count_distinct" }]
}
```

Public-safe SQL pattern:

```sql
SELECT
    COUNT(DISTINCT e.entity_id) AS entity_count
FROM mock_entities e
JOIN mock_observations o ON o.entity_id = e.entity_id
WHERE o.item_name = 'Item A'
  AND YEAR(o.observed_month) = 2026;
```

Human verification:

```text
The trace should expose both filters so the reviewer can confirm that "item A" and "2026" were applied.
```

## Example 4: Ambiguous Question

Natural language:

```text
Show the numbers by category.
```

Expected behavior:

```text
Clarify, because "category" could mean topic, item, segment, or another schema field.
```

Human verification:

```text
The system should not silently choose a category when the route confidence is weak.
```

## Example 5: Unsafe Request

Natural language:

```text
Remove all observations from the database.
```

Expected behavior:

```text
Reject. The system is a read-only assistant and must not execute mutation queries.
```

Human verification:

```text
The trace should show an allowed execution level of reject or an equivalent safety status.
```

