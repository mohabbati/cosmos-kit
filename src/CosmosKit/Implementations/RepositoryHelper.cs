namespace CosmosKit;

internal class RepositoryHelper
{
    internal static void SetEntityDefaults(EntityBase entity)
    {
        entity.Id = Guid.NewGuid().ToString("N");

        if (entity is AuditableEntity auditableEntity)
        {
            auditableEntity.CreatedAt = DateTime.UtcNow;
            auditableEntity.ModifiedAt = DateTime.UtcNow;
        }
    }
}
