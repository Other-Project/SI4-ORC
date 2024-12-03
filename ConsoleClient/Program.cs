using ConsoleClient;
using Terminal.Gui;

Application.Init();
ConfigurationManager.Themes!.Theme = "Dark";
ConfigurationManager.Apply();

try
{
    Application.Run(new MainView());
}
finally
{
    Application.Shutdown();
}
