# Performance Optimization Recommendations for PK-Sim

> **Analysis Date**: 2026-02-27
> **Scope**: Complete PK-Sim solution (17 projects)
> **Total Issues Identified**: 150+

## Executive Summary

This document summarizes performance optimization opportunities identified across the entire PK-Sim solution. Issues are categorized by severity and potential impact, with specific file locations and actionable recommendations.

For detailed analysis with code examples and implementation guidelines, see the comprehensive report: `/tmp/performance_optimization_analysis.md`

---

## Critical Priority Issues (Fix Immediately) ⚠️

### 1. Blocking Async Calls - 10 instances
**Impact**: Thread pool starvation, potential deadlocks, poor responsiveness

**Files Affected**:
- `src/PKSim.CLI/Program.cs:53` - `.Wait()` on CLI entry point
- `src/PKSim.R/Api.cs:32-40` - 5 instances of `.Wait()` in R API
- `src/PKSim.Presentation/UICommands/GeneratePKMLTemplatesCommand.cs:41` - `.Wait()` in heavy work manager
- `src/PKSim.CLI.Core/Services/QualificationRunner.cs:242` - `.Wait()` on markdown export
- `src/PKSim.Core/Services/GlobalPKAnalysisRunner.cs:57` - `.Wait()` on simulation
- `src/PKSim.Infrastructure/ORM/Repositories/RemoteTemplateRepository.cs:118` - `Task.Run().Result` anti-pattern
- `src/PKSim.Core/Services/DefaultIndividualRetriever.cs:60` - `.Result` on mapper
- `src/PKSim.R/Services/IndividualFactory.cs:117` - `.Result` on mapper
- `src/PKSim.R/Services/PopulationFactory.cs:45-47` - 2x `.Result` calls

**Recommendation**: Replace all `.Wait()` and `.Result` with proper `await`, propagate async up call stack

---

### 2. Async Void Methods - 22 instances
**Impact**: Unhandled exceptions, difficult testing, cannot be awaited

**Files Affected** (PKSim.Presentation/UICommands/):
- RunSimulationCommand.cs, ExportSnapshotUICommand.cs, ExportODEForRUICommand.cs
- LoadBuildingBlockFromTemplateUICommand.cs, ExportSimulationToCppUICommand.cs
- ExportSimulationResultsToExcelCommand.cs, ExportSimulationResultsToCSVCommand.cs
- ExportSimulationSnapshotUICommand.cs, StopSimulationCommand.cs, ExportMarkdownUICommand.cs
- ExportODEForMatlabUICommand.cs, CloneSimulationComparisonCommand.cs
- ExportSimulationToSimModelXmlUICommand.cs, SimulationXmlExportCommand.cs
- ExportProjectToSnapshotCommand.cs, RunSimulationsCommand.cs
- LoadPopulationAnalysisWorkflowFromTemplateUICommand.cs
- ExportParameterIdentificationToRUICommand.cs, ExportPopulationSimulationPKAnalysesCommand.cs
- Also: `src/PKSim.Presentation/Presenters/Populations/CreatePopulationPresenter.cs:140`

**Recommendation**: Add try-catch wrapper pattern, extract testable Task-returning helper methods

---

### 3. Disabled Parallel Processing - 1 instance (QUICK WIN!)
**Impact**: Sequential processing when parallelization is possible

**File**: `src/PKSim.CLI.Core/Services/QualificationRunner.cs:223-226`
```csharp
//TODO Enable parallel runs once https://github.com/Open-Systems-Pharmacology/OSPSuite.Utility/issues/26 is fixed
```

**Recommendation**:
- Check if OSPSuite.Utility issue #26 is resolved
- Re-enable: `return await Task.WhenAll(configuration.Inputs.Select(x => exportInput(project, configuration, x)));`
- **Potential for immediate performance improvement in qualification runs**

---

## High Priority Issues

### 4. Inefficient Collection Operations - 40+ instances
**Impact**: High memory usage, unnecessary allocations

**Common Patterns**:
- Materializing with `.ToList()` before filtering
- Multiple enumeration of same collection
- Using `.Contains()` on `List<T>` instead of `HashSet<T>` (O(n) vs O(1))

**Key Files**:
- `src/PKSim.Infrastructure/ORM/Repositories/FormulationRepository.cs:38-44`
- `src/PKSim.Infrastructure/ORM/Repositories/ParameterMetaDataRepository.cs:55`
- `src/PKSim.Infrastructure/ORM/Repositories/StaticReactionRepository.cs:46-61`
- `src/PKSim.CLI.Core/Services/ExportSimulationRunner.cs:74`
- `src/PKSim.CLI.Core/Services/QualificationRunner.cs:153`

**Recommendation**: Use lazy evaluation, cache results, prefer `HashSet<T>` for lookup-heavy operations

---

### 5. Count() in Loops - 10+ instances
**Impact**: Repeated enumeration overhead

**Files**:
- `src/PKSim.Core/Services/CreateIndividualAlgorithm.cs:284,360,436,447`
- `src/PKSim.Infrastructure/Services/GeneExpressionQueries.cs:244,278,282`

**Pattern**:
```csharp
for (int i = 0; i < items.Count(); i++)  // ❌ Enumerates every iteration
```

**Recommendation**: Use `.Count` property or cache count before loop

---

### 6. Synchronous File I/O - 8+ instances
**Impact**: Thread blocking on I/O operations

**Files**:
- `src/PKSim.Infrastructure/Services/MarkdownReporterTask.cs:22` - Wrapping sync I/O in Task.Run
- `src/PKSim.Infrastructure/Serialization/Json/JsonSerializer.cs:26-37` - Sync file writes
- `src/PKSim.BatchTool/Presenters/FolderListSnapshotPresenter.cs:79-90` - File.ReadAllText + JsonConvert

**Recommendation**: Use `File.WriteAllTextAsync`, `StreamWriter.WriteAsync`, streaming JSON

---

## Medium Priority Issues

### 7. Repository Caching - 50+ instances
**Impact**: Repeated database/file access, slow initialization

**Pattern**: Multiple calls to repository `.All()` methods without caching

**Recommendation**:
- Implement lazy initialization
- Cache `.All()` results for read-only data
- Consider `ImmutableArray<T>` for immutable collections

---

### 8. Nested LINQ Operations - 10+ instances
**Impact**: Complex O(n²) or worse operations

**Examples**:
- `src/PKSim.Core/Services/DistributedParametersUpdater.cs:28-30` - Double GroupBy
- `src/PKSim.Core/Services/PKAnalysesTask.cs:412` - Nested GroupBy + Each
- `src/PKSim.Infrastructure/ORM/Repositories/FlatPopulationContainerRepository.cs:27-37` - Nested loops

**Recommendation**: Use composite keys, pre-computed dictionaries, or single-pass algorithms

---

### 9. String Operations - 5+ instances
**Impact**: Memory allocations, GC pressure

**Files**:
- `src/PKSim.Core/Services/InteractionKineticUpdater.cs:203` - String concatenation

**Recommendation**: Use `StringBuilder` for complex string building in loops

---

## Quick Win Opportunities

1. **Enable Parallel Processing** (QualificationRunner.cs:223) - Check external dependency status
2. **Replace List.Contains() with HashSet** (CreateIndividualAlgorithm.cs) - Simple refactor
3. **Fix Count() in Loops** - Simple property access change
4. **R API blocking calls** - Use `GetAwaiter().GetResult()` instead of `.Wait()`

---

## Implementation Priority Order

### Phase 1: Critical (Do First)
1. Fix blocking async calls (10 instances)
2. Check and enable parallel processing (1 instance)
3. Convert async void methods (22 instances)

**Estimated Impact**: Prevents deadlocks, significantly improves responsiveness

---

### Phase 2: Performance
4. Replace inefficient Contains() with HashSet (20+ instances)
5. Fix Count() in loops (10+ instances)
6. Convert file I/O to async (8+ instances)

**Estimated Impact**: Reduces memory pressure, improves I/O throughput

---

### Phase 3: Optimization
7. Implement repository caching strategy
8. Optimize LINQ queries and collections
9. Streaming JSON for large files

**Estimated Impact**: Faster startup, lower memory usage

---

## Code Pattern Examples

### Fixing Blocking Calls

**Before**:
```csharp
public void DoWork()
{
    DoWorkAsync().Wait();  // ❌ Deadlock risk
}
```

**After**:
```csharp
public async Task DoWork()
{
    await DoWorkAsync();  // ✅ Proper async
}
```

**For R API** (non-async context):
```csharp
// Use GetAwaiter().GetResult() instead of .Wait()
public static void RunSnapshot(SnapshotRunOptions runOptions)
{
    resolveTask<IBatchRunner<SnapshotRunOptions>>()
        .RunBatchAsync(runOptions)
        .GetAwaiter()
        .GetResult();  // ✅ Better than .Wait()
}
```

---

### Fixing Async Void

**Before**:
```csharp
protected override async void PerformExecute()
{
    await _task.ExportAsync();  // ❌ Exceptions may be lost
}
```

**After**:
```csharp
protected override async void PerformExecute()
{
    try
    {
        await PerformExecuteAsync();
    }
    catch (Exception ex)
    {
        _logger.AddToLog($"Error: {ex.Message}", LogLevel.Error, ex.StackTrace);
        _dialogCreator.MessageBoxError(ex.Message);
    }
}

protected virtual Task PerformExecuteAsync()
{
    return _task.ExportAsync();  // ✅ Testable
}
```

---

### Optimizing Collections

**Before**:
```csharp
var list = new List<string> { ... };
if (list.Contains(item))  // ❌ O(n)
```

**After**:
```csharp
var set = new HashSet<string> { ... };
if (set.Contains(item))  // ✅ O(1)
```

---

### Fixing Count() in Loops

**Before**:
```csharp
for (int i = 0; i < items.Count(); i++)  // ❌
```

**After**:
```csharp
var count = items.Count();  // or use .Count property
for (int i = 0; i < count; i++)  // ✅
```

---

## Testing Strategy

For each optimization:
1. **Add/update unit tests** for changed methods
2. **Run integration tests** to verify workflows
3. **Measure performance** before/after:
   - Application startup time
   - Operation duration
   - Memory usage
   - Thread pool usage
4. **Verify no regressions** in existing functionality

---

## Metrics to Track

- **Startup Time**: Repository initialization improvements
- **Qualification Run Time**: Parallel processing impact
- **Memory Usage**: Collection optimization impact
- **Thread Pool Usage**: Async/await improvements
- **UI Responsiveness**: Blocking call elimination

---

## Related Issues

- **OSPSuite.Utility #26**: Blocks parallel input processing in QualificationRunner
- **Language Version**: C# 7.3 (no C# 8.0 features)

---

## References

- Comprehensive analysis: `/tmp/performance_optimization_analysis.md`
- Repository memories: QualificationRunner async issues previously identified
- Previous fix: ExportSimulationRunner.cs C# 7.3 compatibility (commit cdf3db1)

---

## Next Steps

1. **Review** this analysis with development team
2. **Prioritize** Phase 1 critical fixes
3. **Create issues** for tracking individual optimizations
4. **Implement incrementally** with proper testing
5. **Measure results** and adjust priorities based on impact
6. **Document learnings** for future development

---

*This is a living document. Update as issues are addressed and new optimizations are identified.*
