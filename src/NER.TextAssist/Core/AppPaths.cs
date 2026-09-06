namespace NER.TextAssist.Core;

public static class AppPaths
{
    public static string UserDataRoot => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "NER Text Assist");

    public static string SourcesDirectory => Path.Combine(UserDataRoot, "Sources");
    public static string IndexDirectory => Path.Combine(UserDataRoot, "Index");
    public static string SettingsDirectory => Path.Combine(UserDataRoot, "Settings");

    public static void EnsureCreated()
    {
        Directory.CreateDirectory(UserDataRoot);
        Directory.CreateDirectory(SourcesDirectory);
        Directory.CreateDirectory(IndexDirectory);
        Directory.CreateDirectory(SettingsDirectory);
    }
}
