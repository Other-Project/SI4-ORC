using System.Collections.ObjectModel;
using System.Text.Json;
using ConsoleClient.RoutingService;
using ORC.Models;
using Terminal.Gui;

namespace ConsoleClient;

public class MainView : Window
{
    private TextField DepartureText { get; }
    private TextField ArrivalText { get; }
    private ListView RouteSteps { get; }

    private ObservableCollection<string> RouteSegments { get; } = [];

    public MainView()
    {
        Title = $"OuvertRueCarte ({Application.QuitKey} pour quitter)";

        // Create input components and labels
        var departureLabel = new Label { Text = "Départ :" };
        var arrivalLabel = new Label { Text = "Destination :", X = Pos.Left(departureLabel), Y = Pos.Bottom(departureLabel) + 1 };
        DepartureText = new TextField
        {
            X = Pos.Right(arrivalLabel) + 1, // Position text field adjacent to the label
            Width = Dim.Fill() // Fill remaining horizontal space
        };
        ArrivalText = new TextField
        {
            X = Pos.Left(DepartureText),
            Y = Pos.Top(arrivalLabel),
            Width = Dim.Fill()
        };

        var btnSearch = new Button
        {
            Text = "Valider",
            Y = Pos.Bottom(arrivalLabel) + 1,

            // center the login button horizontally
            X = Pos.Center(),
            Width = Dim.Fill()
        };
        btnSearch.Accept += async (_, _) => await Search();

        RouteSteps = new ListView
        {
            Y = Pos.Bottom(btnSearch) + 1,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            Source = new ListWrapper<string>(RouteSegments)
        };

        ActiveMqHelper.MessageReceived += message =>
        {
            switch (message.Properties.GetString("tag"))
            {
                case "routing_error":
                    MessageBox.ErrorQuery("Erreur", message.Text, "Ok");
                    break;
                case "instruction":
                    RouteSegments.Add(JsonSerializer.Deserialize<RouteSegment>(message.Text)?.InstructionText ?? "ERREUR");
                    _ = ActiveMqHelper.SendMessage("MORE PLEASE");
                    break;
                default:
                    //MessageBox.Query("Message", message.Text, "Ok");
                    break;
            }
        };
        Closing += async (_, _) => await ActiveMqHelper.DisconnectFromActiveMq();

        Add(departureLabel, DepartureText, arrivalLabel, ArrivalText, btnSearch, RouteSteps); // Add the views to the Window
    }

    private async Task Search()
    {
        RouteSegments.Clear();

        try
        {
            await ActiveMqHelper.DisconnectFromQueues();
            var routingService = new RoutingServiceClient(RoutingServiceClient.EndpointConfiguration.BasicHttpBinding_IRoutingService);
            var response = await routingService.CalculateRouteAsync(4.79450, 45.73590, 4.91913, 45.78584);
            await ActiveMqHelper.ConnectToReceiveQueue(response.Item1);
            await ActiveMqHelper.ConnectToSendQueue(response.Item2);
            await ActiveMqHelper.SendMessage("MORE PLEASE");
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
            MessageBox.ErrorQuery("Erreur", e.Message, "Ok");
        }
    }
}
