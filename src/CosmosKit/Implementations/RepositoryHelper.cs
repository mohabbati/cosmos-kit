namespace CosmosKit;

internal class RepositoryHelper
{
    internal static void SetEntityDefaults(EntityBase entity)
    {
        var utcNow = DateTime.UtcNow;
        var isNew = string.IsNullOrEmpty(entity.Id);

        if (isNew) 
            entity.Id = Guid.NewGuid().ToString("N");

        if (entity is AuditableEntity auditableEntity)
        {
            if (isNew) auditableEntity.CreatedAt = utcNow;
            auditableEntity.ModifiedAt = utcNow;
        }
    }
}
