using FluentValidation;
using MediatR;
using MicroBlogging.Domain.Authentication;
using MicroBlogging.Domain.Entities;
using MicroBlogging.Domain.Repositories;

namespace MicroBlogging.Application.Users.Commands;

public sealed record CreateUserCommand(string Username, string Password) : IRequest<Guid>;

public sealed class CreateUserCommandHandler(IRepository<User> userRepository, IPasswordHasher passwordHasher) : IRequestHandler<CreateUserCommand, Guid>
{
    public async Task<Guid> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var existing = await userRepository.FirstOrDefault(u => u.Username == request.Username);
        if (existing is not null)
            throw new ArgumentException("Username is already taken.");

        var user = new User(request.Username, passwordHasher.HashPassword(request.Password));

        await userRepository.Add(user);
        await userRepository.SaveChanges();

        return user.Id;
    }
}

public sealed class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(6)
            .MaximumLength(128);
    }
}