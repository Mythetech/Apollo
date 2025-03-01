using Apollo.Components.Solutions;
using Apollo.Contracts.Solutions;

namespace Apollo.Components.Library.SampleProjects;

public static class TestingSampleProject
{
    public static SolutionModel Create()
    {
        return new SolutionModel
        {
            Name = "TestingSample",
            ProjectType = ProjectType.ClassLibrary,
            Description = "Sample project with multiple test frameworks",
            Items =
            [
                new SolutionFile
                {
                    Name = "Calculator.cs",
                    Uri = "virtual/TestingSample/Calculator.cs",
                    Data = @"
public class Calculator
{
    public int Add(int a, int b) => a + b;
    public int Subtract(int a, int b) => a - b;
}",
                },

                new SolutionFile
                {
                    Name = "CalculatorXunitTests.cs",
                    Uri = "virtual/TestingSample/CalculatorXunitTests.cs",
                    Data = @"
using Xunit;

public class CalculatorXunitTests 
{
    private readonly Calculator _calc = new();

    [Fact]
    public void Add_ReturnsSum()
    {
        Assert.Equal(3, _calc.Add(1, 2));
    }

    [Fact(Skip = ""Not implemented"")]
    public void Subtract_ReturnsDifference_Skipped()
    {
        Assert.Equal(1, _calc.Subtract(3, 2));
    }

    [Fact]
    public void TestFail()
    {
        Assert.Equal(""fizz"", _calc.Add(1, 2).ToString());
    }
}",
                },

                new SolutionFile
                {
                    Name = "CalculatorNUnitTests.cs",
                    Uri = "virtual/TestingSample/CalculatorNUnitTests.cs",
                    Data = @"
using NUnit.Framework;

[TestFixture]
public class CalculatorNUnitTests
{
    private Calculator _calc;

    [SetUp]
    public void Setup()
    {
        _calc = new Calculator();
    }

    [Test]
    public void Add_ReturnsSum()
    {
        Assert.That(_calc.Add(1, 2), Is.EqualTo(3));
    }

    [Test]
    [Ignore(""Not implemented"")]
    public void Subtract_ReturnsDifference_Ignored()
    {
        Assert.That(_calc.Subtract(3, 2), Is.EqualTo(1));
    }

    [Test]
    public void TestFail()
    {
        Assert.That(_calc.Add(1, 2).ToString(), Is.EqualTo(""fizz""));
    }
}",
                }
            ]
        };
    }
} 