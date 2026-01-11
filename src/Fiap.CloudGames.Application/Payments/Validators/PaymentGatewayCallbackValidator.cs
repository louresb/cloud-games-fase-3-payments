using Fiap.CloudGames.Application.Payments.Dtos;
using FluentValidation;

namespace Fiap.CloudGames.Application.Payments.Validators;

public class PaymentGatewayCallbackValidator : AbstractValidator<PaymentGatewayCallbackDto>
{
    public PaymentGatewayCallbackValidator()
    {
        RuleFor(x => x.PaymentTransactionId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("O identificador de transação é obrigatório.")
            .MaximumLength(100).WithMessage("O identificador de transação pode ter no máximo 100 caracteres.")
            .Must(v => v == null || v == v.Trim())
            .WithMessage("O identificador de transação não deve conter espaços no início ou no fim.");
        
        RuleFor(x => x.Status)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("O status da transação é obrigatório.")
            .Must(v => v == "Success" || v == "Failed" || v == "Canceled")
            .WithMessage("O status da transação deve ser 'Success', 'Failed' ou 'Canceled'.");
    }
}
