namespace CalorieTracker.BuildingBlocks.Messaging;

// Queries read state and never change it (CQRS); commands go through ICommandHandler
public interface IQueryHandler<in TQuery, TResult>
{
    Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken);
}
