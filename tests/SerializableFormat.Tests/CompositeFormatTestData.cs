namespace SerializableFormat.Tests;

public static class CompositeFormatTestData
{
    public static IEnumerable<object[]> ValidFormats()
    {
        // Adapted from System.Tests.StringTests.Format_Valid_TestData in dotnet/runtime.
        yield return [""];
        yield return [", "];
        yield return [", Foo {0  }"];
        yield return ["Foo {0}"];
        yield return ["Foo {0} Baz {1}"];
        yield return ["Foo {0} Baz {1} Bar {2}"];
        yield return ["Foo {0} Baz {1} Bar {2} Foo {3}"];

        yield return ["Foo {0,2}"];
        yield return ["Foo {0,3}"];
        yield return ["Foo {0,     3}"];
        yield return ["Foo {0,0}"];
        yield return ["Foo {0,  2 }"];
        yield return ["Foo {0,-2}"];
        yield return ["Foo {0,-3}"];
        yield return ["Foo {0,     -3}"];
        yield return ["Foo {0, -2  }"];

        yield return ["Foo {0:D6}"];
        yield return ["Foo {0     :D6}"];
        yield return ["Foo {0:}"];
        yield return ["Foo {0,9:D6}"];
        yield return ["Foo {0,-9:D6}"];

        yield return ["Foo {{{0}"];
        yield return ["Foo }}{0}"];
        yield return ["Foo {0} {{0}}"];
        yield return ["{{"];
        yield return ["}}"];
        yield return ["{{text}}"];

        yield return ["{0}"];
        yield return ["{0}{1}"];
        yield return ["{0}{1}{2}"];
        yield return ["{0}{1}{2}{3}"];
        yield return ["{0}{1}{2}{3}{4}"];
        yield return ["{1}{0}"];
        yield return ["{2}{1}{0}"];
        yield return ["{3}{2}{1}{0}"];
        yield return ["{4}{3}{2}{1}{0}"];
        yield return ["0 = {0} 1 = {1} 2 = {2} 3 = {3} 4 = {4}"];

    }

    public static IEnumerable<object[]> InvalidFormats()
    {
        // Adapted from System.Tests.StringTests.Format_Invalid_FormatExceptionFromFormat_MemberData.
        yield return ["{-1}"];
        yield return ["{"];
        yield return ["{a"];
        yield return ["}"];
        yield return ["}a"];
        yield return ["{0:}}"];
        yield return ["{\0"];
        yield return ["{0     "];
        yield return ["{1000000"];
        yield return ["{0,"];
        yield return ["{0,   "];
        yield return ["{0,-"];
        yield return ["{0,-\0"];
        yield return ["{0,-a"];
        yield return ["{0,1000000"];
        yield return ["{0:"];
        yield return ["{0:    "];
        yield return ["{0:{"];
        yield return ["{0:{}"];
    }

    public static IEnumerable<object[]> RoundTripFormats()
    {
        yield return [""];
        yield return ["literal only"];
        yield return ["{{escaped}}"];
        yield return ["{0}"];
        yield return ["prefix {{escaped}} {2,-8:X2} / {0:yyyy-MM-dd} suffix"];
        yield return ["{34} {1,10:N2} {34,-3:}"];
    }
}