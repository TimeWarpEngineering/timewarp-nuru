namespace TimeWarp.Kijaribu;

using System;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class TimeoutAttribute : Attribute
{
  public int Milliseconds { get; }

  public TimeoutAttribute(int milliseconds)
  {
    Milliseconds = milliseconds;
  }
}