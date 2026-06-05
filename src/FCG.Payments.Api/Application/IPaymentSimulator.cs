namespace FCG.Payments.Api.Application;

public record SimulationResult(bool Approved, string? FailureReason);

public interface IPaymentSimulator
{
    SimulationResult Simulate(decimal amount);
}

// Simulação determinística: valores até R$ 10.000 são aprovados; acima disso, rejeitados.
// Trade-off documentado: facilita reprodutibilidade em demo. Em produção: integração com gateway real.
public class PaymentSimulator : IPaymentSimulator
{
    private const decimal ApprovalThreshold = 10000m;

    public SimulationResult Simulate(decimal amount)
    {
        if (amount < 0)
            return new SimulationResult(false, "Valor inválido");

        if (amount > ApprovalThreshold)
            return new SimulationResult(false, $"Valor acima do limite simulado de {ApprovalThreshold:0.00}");

        return new SimulationResult(true, null);
    }
}
