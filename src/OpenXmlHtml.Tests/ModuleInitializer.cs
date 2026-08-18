public static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Init()
    {
        VerifierSettings.DontScrubDateTimes();
        VerifierSettings.DontScrubGuids();
        VerifierSettings.UseSsimForPng();
        // Pin the fallback paper size. An xlsx built by a test states no pageSetup/@paperSize,
        // so Morph would otherwise take the machine's region - A4 locally, Letter on CI - and the
        // spreadsheet pngs would only ever match on the machine that accepted them. The docx side
        // needs nothing here: WordHtmlConverter emits an explicit A4 pgSz.
        VerifyOpenXml.UseLetterPageSize = false;
        VerifierSettings.InitializePlugins();
        VerifierSettings.UniqueForRuntime();
        VerifyOpenXmlConverter.Initialize();
    }
}
