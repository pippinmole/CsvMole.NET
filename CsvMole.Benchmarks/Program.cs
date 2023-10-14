// See https://aka.ms/new-console-template for more information

using BenchmarkDotNet.Running;
using CsvMole.Benchmarks;

BenchmarkRunner.Run<ParserBenchmarks>();