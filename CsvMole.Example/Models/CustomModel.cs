using CsvMole.Abstractions.Attributes;

namespace CsvMole.Example.Models;

public class CustomModel
{
    [CsvOrder(0)]
    public string Id { get; set; }
}