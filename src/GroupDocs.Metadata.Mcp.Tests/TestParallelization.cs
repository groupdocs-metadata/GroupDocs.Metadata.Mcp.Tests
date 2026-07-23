using Xunit;

// Each test class gets its own fresh MCP server (IClassFixture<McpServerFixture>) to
// stay under GroupDocs.Metadata's 15-open evaluation-mode cap. Serialise the classes so
// those per-class dnx servers start one at a time instead of ~7 concurrently on a CI
// runner (avoids resource contention + the fixture's 3-minute startup timeout under load).
[assembly: CollectionBehavior(DisableTestParallelization = true)]
