[assembly: ArgumentDisplayFormatter<VerbatimStringFormatter>]

// TUnit names a test after its arguments, and in a string argument swaps every '.' for '·'
// (U+00B7). On net48 the testing platform sends that character to the IDE as raw utf-8, where
// on net10.0 it is escaped, and Rider's discovery never finishes reading a test list holding
// one. The host it launched then stays running with bin\Debug\net48 open, and the next build
// cannot copy into it. Naming the argument as written keeps the test list ascii.
class VerbatimStringFormatter :
    ArgumentDisplayFormatter
{
    public override bool CanHandle(object? value) =>
        value is string;

    public override string FormatValue(object? value) =>
        (string)value!;
}
