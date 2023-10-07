﻿using System.ComponentModel;
using CsvMole.Abstractions.Attributes;
using CsvMole.Example.Converters;

namespace CsvMole.Example.Models;

public class CustomModel
{
    [CsvOrder(0)]
    public string Id { get; set; } = null!;

    [CsvOrder(1)]
    [CsvConverter(typeof(DateTimeCsvConverter))]
    public DateTime? Date { get; set; }
}