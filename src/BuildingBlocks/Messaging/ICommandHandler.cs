namespace CalorieTracker.BuildingBlocks.Messaging;

// GoF Command: the command object carries the request, its handler executes it.
// Cross-cutting concerns (logging, validation) wrap handlers as decorators.
public interface ICommandHandler<in TCommand, TResult>
{
    Task<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken);
}
