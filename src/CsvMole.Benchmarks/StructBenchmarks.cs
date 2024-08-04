using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;

namespace CsvMole.Benchmarks;

public class RefStructEnumerator
{
    private int _index;
    
    public RefStruct Current
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(this);
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public bool MoveNext()
    {
        ++_index;

        return _index < 100_000;
    }

    public RefStructEnumerator GetEnumerator() => this;
} 

public readonly ref struct RefStruct
{
    internal readonly RefStructEnumerator _enumerator;

    public RefStruct(RefStructEnumerator enumerator)
    {
        _enumerator = enumerator;
    }
    
    public string Id { get; init; }
}

public struct NormalStruct
{
    public string Id { get; init; }
}

[MemoryDiagnoser(false)]
public class StructBenchmarks
{
    private readonly RefStructEnumerator _enumerator = new();
    private readonly List<NormalStruct> _normalStructs = new(10_000);
    
    [Benchmark]
    public List<string> IterateNormalStruct()
    {
        var result = new List<string>();

        foreach ( var normal in _normalStructs )
        {
            result.Add(normal.Id);
        }
        
        return result;
    }
    
    [Benchmark]
    public List<string> IterateObject()
    {
        var result = new List<string>();

        foreach ( var refStruct in _enumerator )
        {
            result.Add(refStruct.Id);
        }
        
        return result;
    }
}