using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;

namespace CosmosKit.Implementations;

internal class CosmosLinqQuery 
{
    public virtual FeedIterator<T> GetFeedIterator<T>(IQueryable<T> query)
    {
        return query.ToFeedIterator();
    }
}