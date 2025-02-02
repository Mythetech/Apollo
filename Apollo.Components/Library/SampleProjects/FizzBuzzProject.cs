using Apollo.Components.Solutions;
using Apollo.Contracts.Solutions;

namespace Apollo.Components.Library.SampleProjects;

public static class FizzBuzzProject
{
    public static SolutionModel Create()
    {
        return new SolutionModel
        {
            Name = "FizzBuzz",
            ProjectType = ProjectType.Console,
            Description = "Sample FizzBuzz project with XUnit tests.",
            Items = new List<ISolutionItem>
            {
                new Folder
                {
                    Name = "FizzBuzz",
                    Uri = "virtual/FizzBuzz"
                },
                new SolutionFile
                {
                    Name = "Program.cs",
                    Uri = "virtual/FizzBuzz/Program.cs",
                    Data = @"
using System;

class Program
{
    static void Main()
    {
        var fizzBuzz = new FizzBuzz();
        for (int i = 1; i <= 100; i++)
        {
            Console.WriteLine(fizzBuzz.GetValue(i));
        }
    }
}
",
                    CreatedAt = DateTimeOffset.Now,
                    ModifiedAt = DateTimeOffset.Now,
                },
                new SolutionFile
                {
                    Name = "FizzBuzz.cs",
                    Uri = "virtual/FizzBuzz/FizzBuzz.cs",
                    Data = @"
public class FizzBuzz
{
    public string GetValue(int number)
    {
        if (number % 15 == 0)
            return ""FizzBuzz"";
        if (number % 3 == 0)
            return ""Fizz"";
        if (number % 5 == 0)
            return ""Buzz"";
        return number.ToString();
    }
}
",
                    CreatedAt = DateTimeOffset.Now,
                    ModifiedAt = DateTimeOffset.Now,
                },
                new SolutionFile
                {
                    Name = "FizzBuzzTests.cs",
                    Uri = "virtual/FizzBuzz/FizzBuzzTests.cs",
                    Data = @"
using Xunit;

public class FizzBuzzTests
{
    [Fact]
    public void TestFizz()
    {
        var fizzBuzz = new FizzBuzz();
        Assert.Equal(""Fizz"", fizzBuzz.GetValue(3));
    }

    [Fact]
    public void TestBuzz()
    {
        var fizzBuzz = new FizzBuzz();
        Assert.Equal(""Buzz"", fizzBuzz.GetValue(5));
    }

    [Fact]
    public void TestFizzBuzz()
    {
        var fizzBuzz = new FizzBuzz();
        Assert.Equal(""FizzBuzz"", fizzBuzz.GetValue(15));
    }

    [Fact]
    public void TestNonFizzBuzzNumber()
    {
        var fizzBuzz = new FizzBuzz();
        Assert.Equal(""1"", fizzBuzz.GetValue(1));
    }

    [Fact]
    public void TestFail()
    {
        var fizzBuzz = new FizzBuzz();
        Assert.Equal(""fizz"", fizzBuzz.GetValue(1));
    }
}
"
                }
            }
        };
    }
}