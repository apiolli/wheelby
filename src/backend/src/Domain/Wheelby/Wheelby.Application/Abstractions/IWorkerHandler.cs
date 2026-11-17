namespace Wheelby.Application.Abstractions;

// Marca los handlers que el Worker registra (AddWheelbyWorker). El resto depende
// de la petición HTTP (ICurrentUser) o de módulos que el Worker no carga.
public interface IWorkerHandler;
