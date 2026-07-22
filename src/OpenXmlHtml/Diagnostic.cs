// Reports html that was recognised and then discarded. Only the deliberate drop sites report: a
// declaration the converter parsed and could not express, an attribute it read and could not apply,
// an element it renders nothing for. Unknown css is left alone, since reporting every `cursor` and
// `float` an ordinary stylesheet carries would bury the signal.
//
// Nothing is built while HtmlConvertSettings.OnDiagnostic is null, so a conversion that does not
// subscribe pays a null check.
static class Diagnostic
{
    internal static void DroppedProperty(HtmlConvertSettings? settings, string name, string? value, string reason) =>
        Report(settings, HtmlDiagnosticKind.DroppedProperty, name, value, reason);

    internal static void IgnoredAttribute(HtmlConvertSettings? settings, string name, string? value, string reason) =>
        Report(settings, HtmlDiagnosticKind.IgnoredAttribute, name, value, reason);

    internal static void UnsupportedElement(HtmlConvertSettings? settings, string name, string reason) =>
        Report(settings, HtmlDiagnosticKind.UnsupportedElement, name, null, reason);

    static void Report(HtmlConvertSettings? settings, HtmlDiagnosticKind kind, string name, string? value, string reason)
    {
        if (settings?.OnDiagnostic is not { } sink)
        {
            return;
        }

        sink(new(kind, name, value, reason));
    }
}
