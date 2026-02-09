// ═══════════════════════════════════════════════════════════════════════════════
// SCIENTIFIC CALCULATOR SERVICE
// ═══════════════════════════════════════════════════════════════════════════════
// Provides mathematical operations used by calculator commands.
// Registered as singleton in DI container.

namespace EndpointCalculator.Services;

/// <summary>
/// Scientific calculator operations interface.
/// </summary>
public interface IScientificCalculator
{
  long Factorial(int n);
  bool IsPrime(int n);
  long Fibonacci(int n);
}

/// <summary>
/// Implementation of scientific calculator operations.
/// </summary>
public class ScientificCalculator : IScientificCalculator
{
  public long Factorial(int n)
  {
    if (n < 0)
    {
      throw new ArgumentException("Factorial not defined for negative numbers");
    }

    if (n == 0 || n == 1)
    {
      return 1;
    }

    long result = 1;
    for (int i = 2; i <= n; i++)
    {
      result *= i;
    }

    return result;
  }

  public bool IsPrime(int n)
  {
    if (n <= 1)
    {
      return false;
    }

    if (n == 2)
    {
      return true;
    }

    if (n % 2 == 0)
    {
      return false;
    }

    for (int i = 3; i * i <= n; i += 2)
    {
      if (n % i == 0)
      {
        return false;
      }
    }

    return true;
  }

  public long Fibonacci(int n)
  {
    if (n < 0)
    {
      throw new ArgumentException("Fibonacci not defined for negative numbers");
    }

    if (n <= 1)
    {
      return n;
    }

    long a = 0;
    long b = 1;
    for (int i = 2; i <= n; i++)
    {
      long temp = a + b;
      a = b;
      b = temp;
    }

    return b;
  }
}
