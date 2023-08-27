## Syntax Transformer

## Description

Tool containing a collection of `Syntax Transformers` capable of rewriting C# code syntax from the AST level. Transformers analyze syntax from the AST and perform updates based on the evaluated nodes / node types. Syntax Transformations are built using [Roslyn Analyzers](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.csharp.csharpsyntaxrewriter?view=roslyn-dotnet-4.3.0)

### Use Case

The transformers are intended to allow quickly creating new projects by auto-generating boiler plate code. 


## Install the Nuget package

After adding the package source the project can be installed similar to any nuget package.

`dotnet tool install SyntaxTransformer  -v <target_version>`


## Usage

The tool can then be ran using the `dotnet` cli such as:

`dotnet syntax-transformer <target-directory>`

### Example Usage

Given an ASP.NET Controller class such as:

```csharp
public HomeController : ControllerBase
{
    private readonly DataContext _context;
    ...
    public IActionResult GetViewModel() => Ok(_context.ViewModels.FirstOrDefault());
}
```

Will be transformed to

```csharp
[Authorize]
[ApiController]
[Route("api/[controller]")]
public HomeController : ControllerBase
{
    private readonly DataContext _context;
    ...
    [HttpGet]
    [ProducesResponseType(typeof(OkObjectResult), 200)]
    public IActionResult GetViewModel() => Ok(_context.ViewModels.FirstOrDefault());
}
```
