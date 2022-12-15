Run Tests (from solution root & test root)
dotnet test --collect:"XPlat Code Coverage"

Generate coverage reports
reportgenerator -reports:lobbyServerTest/TestResults/**/*.xml -targetdir:"coverage" -reporttypes:Html