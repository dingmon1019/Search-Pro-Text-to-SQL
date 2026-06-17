using System.Text.RegularExpressions;

namespace SearchProPublic;

public static class Program
{
    public static void Main(string[] args)
    {
        var question = args.Length > 0
            ? string.Join(' ', args)
            : "How many observations are there by region?";

        var schema = SchemaCatalog.PublicMockSchema();
        var prompt = PromptBuilder.Build(question, schema);
        var plan = MockSemanticPlanner.Plan(question);
        var sql = SqlRenderer.Render(plan);
        var validation = SqlValidator.Validate(sql, schema);
        var result = validation.IsAllowed
            ? MockSqlExecutor.Execute(sql)
            : Array.Empty<IReadOnlyDictionary<string, object>>();

        var trace = new QueryTrace(
            Question: question,
            PromptPreview: prompt,
            Plan: plan,
            Sql: sql,
            Validation: validation,
            ResultPreview: result);

        Console.WriteLine(trace.ToDisplayString());
    }
}

public sealed record SchemaCatalog(IReadOnlySet<string> Tables, IReadOnlySet<string> Columns)
{
    public static SchemaCatalog PublicMockSchema() => new(
        Tables: new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "mock_entities",
            "mock_observations"
        },
        Columns: new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "entity_id",
            "region_name",
            "age_band",
            "segment_label",
            "observation_id",
            "topic_name",
            "item_name",
            "response_value",
            "observed_month"
        });
}

public static class PromptBuilder
{
    public static string Build(string question, SchemaCatalog schema)
    {
        var tableList = string.Join(", ", schema.Tables);
        var columnList = string.Join(", ", schema.Columns);

        return $"""
        Task: Convert the user question into a schema-grounded semantic plan.
        User question: {question}
        Allowed tables: {tableList}
        Allowed columns: {columnList}
        Safety rule: return a plan or clarification. Do not return mutation SQL.
        """;
    }
}

public sealed record SemanticPlan(
    string Shape,
    IReadOnlyList<string> RowAxes,
    IReadOnlyList<string> ColumnAxes,
    string Measure,
    string? FilterField = null,
    string? FilterValue = null,
    string? Clarification = null);

public static class MockSemanticPlanner
{
    public static SemanticPlan Plan(string question)
    {
        var normalized = question.ToLowerInvariant();

        if (Regex.IsMatch(normalized, @"\b(delete|drop|update|insert|exec|truncate)\b"))
        {
            return new SemanticPlan(
                Shape: "reject",
                RowAxes: Array.Empty<string>(),
                ColumnAxes: Array.Empty<string>(),
                Measure: "entity_count",
                Clarification: "This public assistant is read-only.");
        }

        if (normalized.Contains("age") && normalized.Contains("topic"))
        {
            return new SemanticPlan(
                Shape: "crosstab",
                RowAxes: new[] { "age_band" },
                ColumnAxes: new[] { "topic_name" },
                Measure: "entity_count");
        }

        if (normalized.Contains("region"))
        {
            return new SemanticPlan(
                Shape: "grouped",
                RowAxes: new[] { "region_name" },
                ColumnAxes: Array.Empty<string>(),
                Measure: "entity_count");
        }

        return new SemanticPlan(
            Shape: "clarify",
            RowAxes: Array.Empty<string>(),
            ColumnAxes: Array.Empty<string>(),
            Measure: "entity_count",
            Clarification: "Please specify a field such as region, age band, or topic.");
    }
}

public static class SqlRenderer
{
    public static string Render(SemanticPlan plan) => plan.Shape switch
    {
        "reject" or "clarify" => "-- no executable SQL generated",
        "crosstab" => """
            SELECT
                e.age_band,
                o.topic_name,
                COUNT(DISTINCT e.entity_id) AS entity_count
            FROM mock_entities e
            JOIN mock_observations o ON o.entity_id = e.entity_id
            GROUP BY e.age_band, o.topic_name
            ORDER BY e.age_band, o.topic_name;
            """,
        _ => """
            SELECT
                e.region_name,
                COUNT(DISTINCT e.entity_id) AS entity_count
            FROM mock_entities e
            JOIN mock_observations o ON o.entity_id = e.entity_id
            GROUP BY e.region_name
            ORDER BY e.region_name;
            """
    };
}

public sealed record SqlValidationResult(bool IsAllowed, string Code, string Message);

public static class SqlValidator
{
    private static readonly Regex ForbiddenTokens =
        new(@"\b(insert|update|delete|drop|alter|truncate|merge|exec|execute|select\s+into)\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static SqlValidationResult Validate(string sql, SchemaCatalog schema)
    {
        if (sql.StartsWith("-- no executable SQL", StringComparison.OrdinalIgnoreCase))
        {
            return new SqlValidationResult(false, "no_executable_sql", "The request requires clarification or rejection.");
        }

        if (!Regex.IsMatch(sql.TrimStart(), @"^(select|with)\b", RegexOptions.IgnoreCase))
        {
            return new SqlValidationResult(false, "not_read_only", "Only SELECT-style queries are allowed.");
        }

        if (ForbiddenTokens.IsMatch(sql))
        {
            return new SqlValidationResult(false, "forbidden_token", "The query contains a disallowed SQL operation.");
        }

        foreach (var table in Regex.Matches(sql, @"\b(from|join)\s+(?<table>[a-z_][a-z0-9_]*)", RegexOptions.IgnoreCase)
                     .Select(match => match.Groups["table"].Value))
        {
            if (!schema.Tables.Contains(table))
            {
                return new SqlValidationResult(false, "unknown_table", $"Table is not in the public schema: {table}");
            }
        }

        return new SqlValidationResult(true, "read_only_select", "The query passed the public read-only checks.");
    }
}

public static class MockSqlExecutor
{
    public static IReadOnlyList<IReadOnlyDictionary<string, object>> Execute(string sql)
    {
        if (sql.Contains("age_band", StringComparison.OrdinalIgnoreCase) &&
            sql.Contains("topic_name", StringComparison.OrdinalIgnoreCase))
        {
            return new[]
            {
                new Dictionary<string, object> { ["age_band"] = "20-29", ["topic_name"] = "Awareness", ["entity_count"] = 2 },
                new Dictionary<string, object> { ["age_band"] = "30-39", ["topic_name"] = "Retention", ["entity_count"] = 1 }
            };
        }

        return new[]
        {
            new Dictionary<string, object> { ["region_name"] = "North", ["entity_count"] = 2 },
            new Dictionary<string, object> { ["region_name"] = "South", ["entity_count"] = 2 },
            new Dictionary<string, object> { ["region_name"] = "West", ["entity_count"] = 1 }
        };
    }
}

public sealed record QueryTrace(
    string Question,
    string PromptPreview,
    SemanticPlan Plan,
    string Sql,
    SqlValidationResult Validation,
    IReadOnlyList<IReadOnlyDictionary<string, object>> ResultPreview)
{
    public string ToDisplayString()
    {
        var rows = ResultPreview.Count == 0
            ? "[]"
            : string.Join(Environment.NewLine, ResultPreview.Select(row =>
                "  " + string.Join(", ", row.Select(pair => $"{pair.Key}={pair.Value}"))));

        return $"""
        Question:
          {Question}

        Prompt preview:
          {PromptPreview.Replace(Environment.NewLine, Environment.NewLine + "  ")}

        Semantic plan:
          shape={Plan.Shape}; rows=[{string.Join(", ", Plan.RowAxes)}]; columns=[{string.Join(", ", Plan.ColumnAxes)}]; measure={Plan.Measure}; clarification={Plan.Clarification ?? "none"}

        SQL:
          {Sql.Replace(Environment.NewLine, Environment.NewLine + "  ")}

        Validation:
          allowed={Validation.IsAllowed}; code={Validation.Code}; message={Validation.Message}

        Result preview:
        {rows}
        """;
    }
}
