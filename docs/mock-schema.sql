-- Public-safe mock schema for Search-Pro portfolio documentation.
-- This file does not contain real table names, real column names,
-- private entity data, internal IPs, or production connection details.

DROP TABLE IF EXISTS mock_observations;
DROP TABLE IF EXISTS mock_entities;

CREATE TABLE mock_entities (
    entity_id INT NOT NULL PRIMARY KEY,
    region_name VARCHAR(64) NOT NULL,
    age_band VARCHAR(32) NOT NULL,
    segment_label VARCHAR(64) NOT NULL
);

CREATE TABLE mock_observations (
    observation_id INT NOT NULL PRIMARY KEY,
    entity_id INT NOT NULL,
    topic_name VARCHAR(80) NOT NULL,
    item_name VARCHAR(80) NOT NULL,
    response_value VARCHAR(80) NULL,
    observed_month DATE NOT NULL,
    CONSTRAINT fk_mock_observations_entity
        FOREIGN KEY (entity_id) REFERENCES mock_entities(entity_id)
);

INSERT INTO mock_entities (entity_id, region_name, age_band, segment_label) VALUES
    (1, 'North', '20-29', 'Segment A'),
    (2, 'North', '30-39', 'Segment B'),
    (3, 'South', '20-29', 'Segment A'),
    (4, 'South', '40-49', 'Segment C'),
    (5, 'West',  '30-39', 'Segment B');

INSERT INTO mock_observations (
    observation_id,
    entity_id,
    topic_name,
    item_name,
    response_value,
    observed_month
) VALUES
    (101, 1, 'Awareness', 'Item A', 'Yes', '2026-01-01'),
    (102, 2, 'Awareness', 'Item B', 'No',  '2026-01-01'),
    (103, 3, 'Usage',     'Item A', 'Yes', '2026-02-01'),
    (104, 4, 'Usage',     'Item C', 'Yes', '2026-02-01'),
    (105, 5, 'Retention', 'Item B', 'No',  '2026-03-01');

-- Example safe query:
-- Count unique entities by region and topic.
SELECT
    e.region_name,
    o.topic_name,
    COUNT(DISTINCT e.entity_id) AS entity_count
FROM mock_entities e
JOIN mock_observations o ON o.entity_id = e.entity_id
GROUP BY e.region_name, o.topic_name
ORDER BY e.region_name, o.topic_name;
