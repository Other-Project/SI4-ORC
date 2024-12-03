# ORC (OuvertRueCarte)

<p align=center>
  <span>Project realized by <a href="https://github.com/AlbanFALCOZ">Alban Falcoz</a> and <a href="https://github.com/06Games">Evan Galli</a> <br/>as part of the <b>Middleware and Service Oriented Computing</b> course.</span>
</p>

## Requirements

* .NET Runtime 8.0
* ASP.NET Core Runtime 8.0

## How to run

1. Copy the `appsettings.template.json` file that you'll find in `Cache`. Name the copy `appsettings.json` and set the token values. 
2. Build the solution with `dotnet publish`
3. Launch the cache service `.\Cache\bin\Release\net8.0\*\publish\Cache.exe`
4. Launch the routing service `.\RoutingService\bin\Release\net8.0\*\publish\RoutingService.exe`
5. Launch the web server `.\WebFrontend\bin\Release\net8.0\*\publish\WebFrontend.exe`

## How to use

* In your web browser go to [http://localhost:5000](http://localhost:5000)
* Use the TUI client : `.\ConsoleClient\bin\Release\net8.0\*\publish\ConsoleClient.exe`
