using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace NUnit.Framework
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class TestAttribute : Attribute
    {
    }

    public abstract class Constraint
    {
        internal abstract bool Matches(object actual);
        internal abstract string Describe();
    }

    public static class Assert
    {
        public static void That(object actual, Constraint constraint, string message = null)
        {
            if (constraint == null)
            {
                throw new ArgumentNullException(nameof(constraint));
            }

            if (constraint.Matches(actual))
            {
                return;
            }

            var detail = $"Expected {constraint.Describe()}, actual: {Format(actual)}.";
            if (!string.IsNullOrWhiteSpace(message))
            {
                detail += " " + message;
            }

            throw new InvalidOperationException(detail);
        }

        private static string Format(object value)
        {
            return value == null ? "<null>" : value.ToString();
        }
    }

    public static class Is
    {
        public static Constraint True { get; } = new PredicateConstraint(
            actual => actual is bool value && value,
            "true");

        public static Constraint False { get; } = new PredicateConstraint(
            actual => actual is bool value && !value,
            "false");

        public static Constraint Empty { get; } = new PredicateConstraint(
            actual => actual switch
            {
                null => false,
                string text => text.Length == 0,
                IEnumerable enumerable => !enumerable.GetEnumerator().MoveNext(),
                _ => false
            },
            "an empty value");

        public static Constraint EqualTo(object expected)
        {
            return new PredicateConstraint(
                actual => Equals(actual, expected),
                $"equal to {expected ?? "<null>"}");
        }

        public static Constraint GreaterThanOrEqualTo(IComparable expected)
        {
            return new PredicateConstraint(
                actual => actual is IComparable comparable && comparable.CompareTo(expected) >= 0,
                $"greater than or equal to {expected}");
        }
    }

    public static class Does
    {
        public static Constraint Contain(object expected)
        {
            return new PredicateConstraint(
                actual => Contains(actual, expected),
                $"a value containing {expected ?? "<null>"}");
        }

        private static bool Contains(object actual, object expected)
        {
            if (actual is string text && expected is string expectedText)
            {
                return text.Contains(expectedText, StringComparison.Ordinal);
            }

            if (actual is IEnumerable enumerable)
            {
                foreach (var item in enumerable)
                {
                    if (Equals(item, expected))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }

    internal sealed class PredicateConstraint : Constraint
    {
        private readonly Func<object, bool> predicate;
        private readonly string description;

        public PredicateConstraint(Func<object, bool> predicate, string description)
        {
            this.predicate = predicate;
            this.description = description;
        }

        internal override bool Matches(object actual)
        {
            return predicate(actual);
        }

        internal override string Describe()
        {
            return description;
        }
    }
}

internal static class DomainTestModuleInitializer
{
    [ModuleInitializer]
    internal static void Run()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var tests = assembly
            .GetTypes()
            .SelectMany(type => type
                .GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(method => method.GetCustomAttribute<NUnit.Framework.TestAttribute>() != null)
                .Select(method => (Type: type, Method: method)))
            .OrderBy(test => test.Type.FullName, StringComparer.Ordinal)
            .ThenBy(test => test.Method.Name, StringComparer.Ordinal)
            .ToArray();

        if (tests.Length == 0)
        {
            throw new InvalidOperationException("No Project 77 domain tests were discovered.");
        }

        var failures = 0;
        foreach (var test in tests)
        {
            try
            {
                var instance = test.Method.IsStatic ? null : Activator.CreateInstance(test.Type);
                test.Method.Invoke(instance, null);
                Console.WriteLine($"[PASS] {test.Type.FullName}.{test.Method.Name}");
            }
            catch (TargetInvocationException exception)
            {
                failures++;
                var cause = exception.InnerException ?? exception;
                Console.Error.WriteLine($"[FAIL] {test.Type.FullName}.{test.Method.Name}: {cause.Message}");
                Console.Error.WriteLine(cause.StackTrace);
            }
            catch (Exception exception)
            {
                failures++;
                Console.Error.WriteLine($"[FAIL] {test.Type.FullName}.{test.Method.Name}: {exception.Message}");
                Console.Error.WriteLine(exception.StackTrace);
            }
        }

        Console.WriteLine($"Project 77 domain tests: {tests.Length - failures} passed, {failures} failed, {tests.Length} total.");
        if (failures != 0)
        {
            throw new InvalidOperationException($"{failures} Project 77 domain test(s) failed.");
        }
    }
}
