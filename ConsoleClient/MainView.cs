using System.Collections.ObjectModel;
using System.Text.Json;
using ConsoleClient.RoutingService;
using ORC.Models;
using Terminal.Gui;

namespace ConsoleClient;

public class MainView : Window
{
    private readonly HttpClient client = new();

    private TextField DepartureText { get; }
    private TextField ArrivalText { get; }

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
            X = Pos.Center()
        };
        btnSearch.Accept += async (_, _) => await Search();

        var line = new LineView { Y = Pos.Bottom(btnSearch) + 1, Width = Dim.Fill() };

        var routeSteps = new ListView
        {
            Y = Pos.Bottom(line),
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            Source = new ListWrapper<string>(RouteSegments)
        };

        ActiveMqHelper.MessageReceived += message =>
        {
            switch (message.Properties.GetString("tag"))
            {
                case "routing_error":
                    MessageBox.ErrorQuery("Erreur", JsonSerializer.Deserialize<string>(message.Text), "Ok");
                    break;
                case "instruction":
                    RouteSegments.Add(JsonSerializer.Deserialize<RouteSegment>(message.Text)?.InstructionText ?? "ERREUR");
                    _ = ActiveMqHelper.SendMessage("MORE PLEASE");
                    break;
                default:
                    //MessageBox.Query("Message", JsonSerializer.Deserialize<string>(message.Text), "Ok");
                    break;
            }
        };
        Closing += async (_, _) => await ActiveMqHelper.DisconnectFromActiveMq();

        Add(departureLabel, DepartureText, arrivalLabel, ArrivalText, btnSearch, line, routeSteps); // Add the views to the Window
    }

    private async Task Search()
    {
        RouteSegments.Clear();

        var (departureAddress, departureLat, departureLon) = await FindAddress(DepartureText.Text);
        DepartureText.Text = departureAddress;
        var (arrivalAddress, arrivalLat, arrivalLon) = await FindAddress(ArrivalText.Text);
        ArrivalText.Text = arrivalAddress;

        try
        {
            await ActiveMqHelper.DisconnectFromQueues();
            var routingService = new RoutingServiceClient(RoutingServiceClient.EndpointConfiguration.BasicHttpBinding_IRoutingService);
            var response = await routingService.CalculateRouteAsync(departureLon, departureLat, arrivalLon, arrivalLat);
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

    private async Task<(string matchedAddress, double lat, double lon)> FindAddress(string address)
    {
        var response = await client.GetStringAsync(
            $"https://api.openrouteservice.org/geocode/autocomplete?api_key=5b3ce3597851110001cf624846c93be49c1f44f0949187d18b1d653c&layers=address,neighbourhood,locality,borough&text={address}");
        var json = JsonDocument.Parse(response).RootElement;
        var result = json.GetProperty("features")[0];
        var matchedAddress = result.GetProperty("properties").GetProperty("label").GetString();
        var coords = result.GetProperty("geometry").GetProperty("coordinates");
        var lon = coords[0].GetDouble();
        var lat = coords[1].GetDouble();
        return (matchedAddress ?? address, lat, lon);
    }
}
