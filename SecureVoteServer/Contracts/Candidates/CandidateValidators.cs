using FluentValidation;

namespace SecureVote.Contracts.Candidates;

public class CreateCandidateRequestValidator : AbstractValidator<CreateCandidateRequest>
{
    public CreateCandidateRequestValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required")
            .MaximumLength(200).WithMessage("Full name must not exceed 200 characters");

        RuleFor(x => x.Symbol)
            .MaximumLength(50).WithMessage("Symbol must not exceed 50 characters");

        RuleFor(x => x.PartyName)
            .MaximumLength(200).WithMessage("Party name must not exceed 200 characters");

        RuleFor(x => x.OrderNumber)
            .GreaterThan(0).WithMessage("Order number must be greater than 0");
    }
}

public class UpdateCandidateRequestValidator : AbstractValidator<UpdateCandidateRequest>
{
    public UpdateCandidateRequestValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required")
            .MaximumLength(200).WithMessage("Full name must not exceed 200 characters");

        RuleFor(x => x.Symbol)
            .MaximumLength(50).WithMessage("Symbol must not exceed 50 characters");

        RuleFor(x => x.PartyName)
            .MaximumLength(200).WithMessage("Party name must not exceed 200 characters");

        RuleFor(x => x.OrderNumber)
            .GreaterThan(0).WithMessage("Order number must be greater than 0");
    }
}
