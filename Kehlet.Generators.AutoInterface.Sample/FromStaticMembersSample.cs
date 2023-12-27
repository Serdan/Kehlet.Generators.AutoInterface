﻿using Kehlet.Functional.Generators;

namespace Kehlet.Generators.AutoInterface.Sample;

public static class FromStaticMembersSample
{
    public static void DoSomething(int number)
    {
    }

    public static int GetNumber(string str) => str.Length;
}

[FromStaticMembers(typeof(FromStaticMembersSample), implement: true, voidType: typeof(Unit))]
public partial interface IInstance;

public readonly record struct Unit;