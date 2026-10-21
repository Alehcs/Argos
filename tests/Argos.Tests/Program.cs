using System.Reflection;

namespace Argos.Tests;

[AttributeUsage(AttributeTargets.Method)]
public sealed class TestAttribute : Attribute;

/// <summary>A test that needs something this PC lacks (OBS Studio, the captions engine): skipped, not failed.</summary>
public sealed class SkipException(string what) : Exception($"needs {what}");

public static class Assert
{
    /// <summary>Skips the test when what it needs is not on this PC.</summary>
    public static void Needs(bool present, string what)
    {
        if (!present)
            throw new SkipException(what);
    }

    public static void Equal<T>(T expected, T actual, string? what = null)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new Exception($"{what ?? "value"}: expected <{expected}> but was <{actual}>");
    }

    public static void True(bool condition, string message)
    {
        if (!condition)
            throw new Exception(message);
    }

    public static void Near(double expected, double actual, double tolerance, string? what = null)
    {
        if (Math.Abs(expected - actual) > tolerance)
            throw new Exception($"{what ?? "value"}: expected {expected}±{tolerance} but was {actual}");
    }
}

public static class Program
{
    public static int Main(string[] args)
    {
        string? filter = args.Length > 0 ? args[0] : null;
        var tests = typeof(Program).Assembly.GetTypes()
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Static))
            .Where(m => m.GetCustomAttribute<TestAttribute>() != null)
            .Where(m => filter == null || $"{m.DeclaringType!.Name}.{m.Name}".Contains(filter, StringComparison.OrdinalIgnoreCase))
            .OrderBy(m => m.DeclaringType!.Name).ThenBy(m => m.Name)
            .ToList();

        int failed = 0, skipped = 0;
        foreach (var test in tests)
        {
            string name = $"{test.DeclaringType!.Name}.{test.Name}";
            try
            {
                test.Invoke(null, null);
                Console.WriteLine($"PASS {name}");
            }
            catch (TargetInvocationException ex) when (SkipReason(ex.InnerException) is { } reason)
            {
                skipped++;
                Console.WriteLine($"SKIP {name}: {reason}");
            }
            catch (TargetInvocationException ex)
            {
                failed++;
                Console.WriteLine($"FAIL {name}: {ex.InnerException?.Message}");
                if (Environment.GetEnvironmentVariable("ARGOS_TEST_TRACE") == "1")
                    Console.WriteLine(ex.InnerException);
            }
        }

        Console.WriteLine($"{tests.Count - skipped - failed}/{tests.Count - skipped} passed" + (skipped > 0 ? $", {skipped} skipped" : ""));
        return failed;
    }

    /// <summary>Why a test was skipped (also when a type's initializer asked for it), or null when it failed.</summary>
    static string? SkipReason(Exception? e)
    {
        for (; e != null; e = e.InnerException)
            if (e is SkipException skip)
                return skip.Message;
        return null;
    }
}
