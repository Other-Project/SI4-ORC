using Terminal.Gui;

namespace ConsoleClient;

public class MainView : Window
{
    private TextField DepartureText { get; }
    private TextField ArrivalText { get; }
    private ScrollBarView RouteSteps { get; }

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
            Width = Dim.Fill(),
        };
        btnSearch.Accept += (s, e) => Search();

        RouteSteps = new ScrollBarView
        {
            Y = Pos.Bottom(btnSearch) + 1,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
        };

        Add(departureLabel, DepartureText, arrivalLabel, ArrivalText, btnSearch, RouteSteps); // Add the views to the Window
    }

    private void Search()
    {
        RouteSteps.Clear();

        var success = false;
        if (!success)
        {
            MessageBox.ErrorQuery("Erreur", "Aucun itinéraire n'a été trouvé", "Ok");
        }
    }
}
