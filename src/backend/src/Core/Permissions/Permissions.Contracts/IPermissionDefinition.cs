namespace Permissions.Contracts;

// Cada módulo registra sus permisos en DI como IPermissionDefinition.
// Es la inversión de dependencia que mantiene al Core independiente.
public interface IPermissionDefinition
{
    string Code { get; }
    string Description { get; }
}
