namespace Apollo.Components.Library.Snippets;

public static class Emojis
{
    public const string FruitArray = @"
using System;

string[] items = { ""🍎 Apple"", ""🍌 Banana"", ""🍒 Cherry"", ""🍇 Grape"", ""🍍 Pineapple"" };

foreach (var item in items)
{
    Console.WriteLine(item);
}
";
}