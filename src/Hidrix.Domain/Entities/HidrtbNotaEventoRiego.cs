namespace Hidrix.Domain.Entities;

/// <summary>
/// Nota de evento de riego registrada por un usuario cliente.
/// </summary>
public class HidrtbNotaEventoRiego
{
    /// <summary>Identificador.</summary>
    public int EvriId { get; set; }

    /// <summary>Usuario (FK HidrtbUsuario).</summary>
    public string UsuaId { get; set; } = string.Empty;

    /// <summary>Nombre de parcela o lote.</summary>
    public string EvriNombreParcelaLote { get; set; } = string.Empty;

    /// <summary>Nombre del cultivo.</summary>
    public string EvriCultivo { get; set; } = string.Empty;

    /// <summary>Id de cultivo del catálogo si aplica.</summary>
    public int? CultId { get; set; }

    /// <summary>Fecha del riego.</summary>
    public DateOnly EvriFecha { get; set; }

    /// <summary>Hora de inicio.</summary>
    public TimeOnly EvriHoraInicio { get; set; }

    /// <summary>Hora de finalización.</summary>
    public TimeOnly EvriHoraFin { get; set; }

    /// <summary>Duración estimada en minutos.</summary>
    public int EvriDuracionMinutos { get; set; }

    /// <summary>Tipo de riego (código).</summary>
    public string EvriTipoRiego { get; set; } = string.Empty;

    /// <summary>Caudal en litros/hora (opcional).</summary>
    public decimal? EvriCaudalHoraLph { get; set; }

    /// <summary>Tipo de suelo (opcional).</summary>
    public string? EvriTipoSuelo { get; set; }

    /// <summary>Id de ciudad del catálogo (lugar del evento).</summary>
    public int? CiuId { get; set; }

    /// <summary>Ciudad del catálogo.</summary>
    public HidrtbCiudad? Ciudad { get; set; }

    /// <summary>Latitud GPS del lugar (legado).</summary>
    public decimal? EvriLugarLatitud { get; set; }

    /// <summary>Longitud GPS del lugar (legado).</summary>
    public decimal? EvriLugarLongitud { get; set; }

    /// <summary>Registro activo.</summary>
    public bool EvriActivo { get; set; } = true;

    /// <summary>Fecha de creación UTC.</summary>
    public DateTime EvriFechaCreacion { get; set; }

    /// <summary>Fecha de actualización UTC.</summary>
    public DateTime EvriFechaActualizacion { get; set; }
}
