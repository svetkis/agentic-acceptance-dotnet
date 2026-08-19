using DemoProject.MinimalApi.Features.Orders;
using DemoProject.MinimalApi.Features.Payments;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// TRAP: An agent might have dropped endpoint mapping during refactoring.
// GUARDRAIL: Integration tests verify that the endpoint responds.
app.MapOrderEndpoints();
app.MapPaymentEndpoints();

app.Run();

// For integration tests
public partial class Program { }
