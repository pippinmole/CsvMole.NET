// See https://aka.ms/new-console-template for more information

using BenchmarkDotNet.Running;
using CsvMole.Benchmarks;
using CsvMole.Benchmarks.Models;
using nietras.SeparatedValues;

BenchmarkRunner.Run<ParserBenchmarks>();