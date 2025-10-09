#!/usr/bin/dotnet --
// calc-mediator - Calculator using Mediator pattern for testability
#:project ../../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj

using TimeWarp.Nuru;
using TimeWarp.Mediator;
using Microsoft.Extensions.DependencyInjection;
using static System.Console;

var builder = new NuruAppBuilder()
    .AddDependencyInjection(config => config.RegisterServicesFromAssembly(typeof(AddCommand).Assembly));

// Register services
builder.Services.AddSingleton<ICalculatorService, CalculatorService>();

// Define routes - no return type needed, just console output
builder.AddRoute<AddCommand>("add {x:double} {y:double}");
builder.AddRoute<SubtractCommand>("subtract {x:double} {y:double}");
builder.AddRoute<MultiplyCommand>("multiply {x:double} {y:double}");
builder.AddRoute<DivideCommand>("divide {x:double} {y:double}");
builder.AddRoute<RoundCommand>("round {value:double} --mode {mode}");
builder.AddRoute<RoundCommand>("round {value:double}");

// Help commands
builder.AddRoute("add help", 
    () => WriteLine("Usage: ./calc-mediator add <x> <y>\nAdds two numbers together."));
builder.AddRoute("round help", 
    () => WriteLine(
        "Usage: ./calc-mediator round <value> [--mode <mode>]\n" +
        "Rounds a number using the specified mode.\n" +
        "Modes:\n" +
        "  up         - Always round up (ceiling)\n" +
        "  down       - Always round down (floor)\n" +
        "  nearest    - Round to nearest integer (default)\n" +
        "  banker     - Banker's rounding (round half to even)"));
builder.AddRoute("help", 
    () => WriteLine(
        "Calculator Commands:\n" +
        "  add <x> <y>              - Add two numbers\n" +
        "  subtract <x> <y>         - Subtract y from x\n" +
        "  multiply <x> <y>         - Multiply two numbers\n" +
        "  divide <x> <y>           - Divide x by y\n" +
        "  round <value> --mode (up|down|nearest|banker)   - Round a number\n" +
        "\n" +
        "Add 'help' after any command for detailed usage."));

var app = builder.Build();
return await app.RunAsync(args);

// Command definitions with nested handlers
public class AddCommand : IRequest 
{ 
    public double X { get; set; }
    public double Y { get; set; }
    
    public class Handler(ICalculatorService calc) : IRequestHandler<AddCommand>
    {
        public async Task Handle(AddCommand request, CancellationToken cancellationToken)
        {
            var result = calc.Add(request.X, request.Y);
            WriteLine($"{request.X} + {request.Y} = {result}");
            await Task.CompletedTask;
        }
    }
}

public class SubtractCommand : IRequest
{
    public double X { get; set; }
    public double Y { get; set; }
    
    public class Handler(ICalculatorService calc) : IRequestHandler<SubtractCommand>
    {
        public async Task Handle(SubtractCommand request, CancellationToken cancellationToken)
        {
            var result = calc.Subtract(request.X, request.Y);
            WriteLine($"{request.X} - {request.Y} = {result}");
            await Task.CompletedTask;
        }
    }
}

public class MultiplyCommand : IRequest
{
    public double X { get; set; }
    public double Y { get; set; }
    
    public class Handler(ICalculatorService calc) : IRequestHandler<MultiplyCommand>
    {
        public async Task Handle(MultiplyCommand request, CancellationToken cancellationToken)
        {
            var result = calc.Multiply(request.X, request.Y);
            WriteLine($"{request.X} × {request.Y} = {result}");
            await Task.CompletedTask;
        }
    }
}

public class DivideCommand : IRequest
{
    public double X { get; set; }
    public double Y { get; set; }
    
    public class Handler(ICalculatorService calc) : IRequestHandler<DivideCommand>
    {
        public async Task Handle(DivideCommand request, CancellationToken cancellationToken)
        {
            var (result, error) = calc.Divide(request.X, request.Y);
            if (error != null)
                WriteLine($"Error: {error}");
            else
                WriteLine($"{request.X} ÷ {request.Y} = {result}");
            await Task.CompletedTask;
        }
    }
}

public class RoundCommand : IRequest
{
    public double Value { get; set; }
    public string? Mode { get; set; } = "nearest";
    
    public class Handler(ICalculatorService calc) : IRequestHandler<RoundCommand>
    {
        public async Task Handle(RoundCommand request, CancellationToken cancellationToken)
        {
            var (result, error) = calc.Round(request.Value, request.Mode ?? "nearest");
            if (error != null)
            {
                WriteLine($"Error: {error}");
                WriteLine("Valid modes: up, down, nearest, banker/accountancy");
            }
            else
            {
                WriteLine($"Round({request.Value}, {request.Mode ?? "nearest"}) = {result}");
            }
            await Task.CompletedTask;
        }
    }
}

// Service interface
public interface ICalculatorService
{
    double Add(double x, double y);
    double Subtract(double x, double y);
    double Multiply(double x, double y);
    (double result, string? error) Divide(double x, double y);
    (double result, string? error) Round(double value, string mode);
}

// Service implementation
public class CalculatorService : ICalculatorService
{
    public double Add(double x, double y) => x + y;
    
    public double Subtract(double x, double y) => x - y;
    
    public double Multiply(double x, double y) => x * y;
    
    public (double result, string? error) Divide(double x, double y) => 
        y == 0 
            ? (0, "Division by zero") 
            : (x / y, null);
    
    public (double result, string? error) Round(double value, string mode) =>
        mode.ToLower() switch
        {
            "up" => (Math.Ceiling(value), null),
            "down" => (Math.Floor(value), null),
            "nearest" => (Math.Round(value), null),
            "banker" or "accountancy" => (Math.Round(value, MidpointRounding.ToEven), null),
            _ => (0, $"Unknown rounding mode '{mode}'")
        };
}
