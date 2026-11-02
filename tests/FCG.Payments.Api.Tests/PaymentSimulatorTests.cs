using FCG.Payments.Api.Application;

namespace FCG.Payments.Api.Tests;

public class PaymentSimulatorTests
{
    private readonly PaymentSimulator _simulator = new();

    [Fact]
    public void Simulate_ValorAcimaDoLimite_RetornaRejeitado()
    {
        var result = _simulator.Simulate(10000.01m);

        Assert.False(result.Approved);
        Assert.NotNull(result.FailureReason);
    }
}
