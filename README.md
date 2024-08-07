<!-- PROJECT LOGO -->
<br />
<div align="center">
  <a href="https://github.com/othneildrew/Best-README-Template">
    <img src="media/logo.png" alt="Logo" width="178" height="71">
  </a>

<h3 align="center">CsvMole.NET</h3>
  <p align="center">
    A source generated CSV Parser for .NET
    <br />
    <br />
    <a href="https://github.com/pippinmole/CsvMole.NET/issues/new?assignees=&labels=&projects=&template=bug_report.md&title=">Report Bug</a>
    ·
    <a href="https://github.com/pippinmole/CsvMole.NET/issues/new?assignees=&labels=&projects=&template=feature_request.md&title=">Request Feature</a>
  </p>
</div>

<!-- GETTING STARTED -->
## Getting Started

There is currently no package on NuGet, but you can clone the repo and build the project yourself.

<p align="right">(<a href="#readme-top">back to top</a>)</p>

<!-- USAGE EXAMPLES -->
## Usage

```csharp
// Define your parser. Make sure the class and method definitions are partial.
[CsvParser]
public partial class CustomParser
{
    public partial IEnumerable<CustomModel> Parse(StringReader stringReader, CsvOptions? options);
}

// Define your model
[CsvDelimiter(",")] // If you don't define this attribute, it will default to ","
public sealed class CustomModel
{
    [CsvOrder(0)]
    public string Id { get; set; } = null!;

    [CsvOrder(1)]
    [CsvConverter(typeof(CsvDateTimeConverter))]
    public DateTime? Date { get; set; }
}

// Use your parser
var csv = "Id,Date\n1,2021-01-01";
var stringReader = new StringReader(csv);
var parser = new CustomParser();

var models = parser.Parse(stringReader, null);

foreach (var model in models)
{
    Console.WriteLine(model.Id);
    Console.WriteLine(model.Date);
}
```



<p align="right">(<a href="#readme-top">back to top</a>)</p>



<!-- CONTRIBUTING -->
## Contributing

Contributions are what make the open source community such an amazing place to learn, inspire, and create. Any contributions you make are **greatly appreciated**.

If you have a suggestion that would make this better, please fork the repo and create a pull request. You can also simply open an issue with the tag "enhancement".
Don't forget to give the project a star! Thanks again!

1. Fork the Project
2. Create your Feature Branch (`git checkout -b feature/AmazingFeature`)
3. Commit your Changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the Branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

<p align="right">(<a href="#readme-top">back to top</a>)</p>


<!-- LICENSE -->
## License

Distributed under the MIT License. See `LICENSE.txt` for more information.

<p align="right">(<a href="#readme-top">back to top</a>)</p>
