using System.Windows;
using NER.TextAssist.Core;

namespace NER.TextAssist;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        AppPaths.EnsureCreated();
        base.OnStartup(e);
    }
}
