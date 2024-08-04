// See https://aka.ms/new-console-template for more information

using System.Globalization;
using BenchmarkDotNet.Running;
using CsvHelper;
using CsvMole.Abstractions.Options;
using CsvMole.Benchmarks;
using CsvMole.Benchmarks.Models;

// const string csv = "1,2019/01/01";
// var options = new CsvOptions { HasHeader = false };
// var reader = new StringReader(csv);
// var results = CustomParserExample1.Parse1(reader, options).ToList();
//
// foreach ( var result in results )
// {
//     Console.WriteLine("Id: {0}, Date: {1}", result.Id, result.Date);
// }
// BenchmarkRunner.Run<ParserBenchmarks>();
BenchmarkRunner.Run<StructBenchmarks>();