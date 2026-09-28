using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Hidrix.Application.Common.Interfaces;
using Hidrix.Application.DTOs;
using Hidrix.Domain.Entities;
using Hidrix.Infrastructure.Persistence;

namespace Hidrix.Infrastructure.Persistence.Repositories;

/// <summary>Persistencia de notas de evento de riego.</summary>
public sealed class IrrigationEventNoteService : IIrrigationEventNoteService
{
    private static readonly HashSet<string> ValidIrrigationTypes = new(StringComparer.Ordinal)
    {
        "goteo_terrestre",
        "subterraneo",
        "microaspersion",
        "aspersion",
        "manual",
    };

    private static readonly Dictionary<string, string> IrrigationTypeLabels = new(StringComparer.Ordinal)
    {
        ["goteo_terrestre"] = "Goteo terrestre",
        ["subterraneo"] = "Subterráneo",
        ["microaspersion"] = "Microaspersión",
        ["aspersion"] = "Aspersión",
        ["manual"] = "Manual",
    };

    private readonly HidrixDbContext _db;

    /// <summary>Inicializa el servicio.</summary>
    public IrrigationEventNoteService(HidrixDbContext db)
    {
        _db = db;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<IrrigationEventNoteDto>> ListAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var rows = await QueryWithLocation()
            .Where(x => x.Note.EvriActivo && x.Note.UsuaId == userId)
            .OrderByDescending(x => x.Note.EvriFecha)
            .ThenByDescending(x => x.Note.EvriHoraInicio)
            .ThenByDescending(x => x.Note.EvriId)
            .ToListAsync(cancellationToken);

        return rows.Select(ToDto).ToList();
    }

    /// <inheritdoc />
    public async Task<IrrigationEventNoteDto> CreateAsync(
        string userId,
        CreateIrrigationEventNoteRequest request,
        CancellationToken cancellationToken = default)
    {
        var plotName = (request.PlotName ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(plotName))
        {
            throw new ArgumentException("El nombre de parcela o lote es obligatorio.");
        }

        var cropName = (request.CropName ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(cropName))
        {
            throw new ArgumentException("El cultivo es obligatorio.");
        }

        if (!DateOnly.TryParse(request.EventDate, out var eventDate))
        {
            throw new ArgumentException("La fecha no es válida.");
        }

        if (!TimeOnly.TryParse(request.StartTime, out var startTime))
        {
            throw new ArgumentException("La hora de inicio no es válida.");
        }

        if (!TimeOnly.TryParse(request.EndTime, out var endTime))
        {
            throw new ArgumentException("La hora de finalización no es válida.");
        }

        var irrigationType = (request.IrrigationType ?? string.Empty).Trim();
        if (!ValidIrrigationTypes.Contains(irrigationType))
        {
            throw new ArgumentException("El tipo de riego no es válido.");
        }

        if (request.CityId <= 0)
        {
            throw new ArgumentException("La ciudad es obligatoria.");
        }

        var cityExists = await _db.Ciudades.AsNoTracking()
            .AnyAsync(c => c.CiuId == request.CityId, cancellationToken);
        if (!cityExists)
        {
            throw new ArgumentException("La ciudad seleccionada no es válida.");
        }

        decimal? flowRate = null;
        if (request.FlowRateLph is not null)
        {
            if (request.FlowRateLph < 0)
            {
                throw new ArgumentException("El caudal hora debe ser mayor o igual a cero.");
            }

            flowRate = (decimal)request.FlowRateLph.Value;
        }

        int? cultId = request.CropId;
        if (cultId is null)
        {
            cultId = await _db.Cultivos.AsNoTracking()
                .Where(c => c.CultActivo && c.CultNombre == cropName)
                .Select(c => (int?)c.CultId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var durationMinutes = EstimateDurationMinutes(startTime, endTime);
        var now = DateTime.UtcNow;

        var entity = new HidrtbNotaEventoRiego
        {
            UsuaId = userId,
            EvriNombreParcelaLote = plotName,
            EvriCultivo = cropName,
            CultId = cultId,
            EvriFecha = eventDate,
            EvriHoraInicio = startTime,
            EvriHoraFin = endTime,
            EvriDuracionMinutos = durationMinutes,
            EvriTipoRiego = irrigationType,
            EvriCaudalHoraLph = flowRate,
            EvriTipoSuelo = string.IsNullOrWhiteSpace(request.SoilType)
                ? null
                : request.SoilType.Trim(),
            CiuId = request.CityId,
            EvriActivo = true,
            EvriFechaCreacion = now,
            EvriFechaActualizacion = now,
        };

        _db.NotasEventoRiego.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        var created = await QueryWithLocation()
            .Where(x => x.Note.EvriId == entity.EvriId)
            .FirstAsync(cancellationToken);
        return ToDto(created);
    }

    /// <inheritdoc />
    public async Task<byte[]> ExportExcelAsync(
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken = default)
    {
        if (toDate < fromDate)
        {
            throw new ArgumentException("La fecha final debe ser mayor o igual a la inicial.");
        }

        var rows = await (
            from item in QueryWithLocation()
            join u in _db.Users.AsNoTracking() on item.Note.UsuaId equals u.Id
            where item.Note.EvriActivo
                && item.Note.EvriFecha >= fromDate
                && item.Note.EvriFecha <= toDate
            orderby item.Note.EvriFecha, item.Note.EvriHoraInicio, item.Note.EvriId
            select new
            {
                item.Note,
                item.DepartmentName,
                item.CityName,
                UserEmail = u.Email ?? string.Empty,
                UserName = u.UsuaNombre,
            }
        ).ToListAsync(cancellationToken);

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Notas evento riego");

        var headers = new[]
        {
            "Id",
            "Usuario",
            "Correo",
            "Parcela / lote",
            "Cultivo",
            "Fecha",
            "Hora inicio",
            "Hora fin",
            "Duración (min)",
            "Tipo de riego",
            "Caudal hora (L/h)",
            "Tipo de suelo",
            "Departamento",
            "Ciudad",
            "Fecha registro (UTC)",
        };

        for (var col = 0; col < headers.Length; col++)
        {
            sheet.Cell(1, col + 1).Value = headers[col];
            sheet.Cell(1, col + 1).Style.Font.Bold = true;
        }

        var rowIndex = 2;
        foreach (var row in rows)
        {
            var n = row.Note;
            sheet.Cell(rowIndex, 1).Value = n.EvriId;
            sheet.Cell(rowIndex, 2).Value = row.UserName;
            sheet.Cell(rowIndex, 3).Value = row.UserEmail;
            sheet.Cell(rowIndex, 4).Value = n.EvriNombreParcelaLote;
            sheet.Cell(rowIndex, 5).Value = n.EvriCultivo;
            sheet.Cell(rowIndex, 6).Value = n.EvriFecha.ToDateTime(TimeOnly.MinValue);
            sheet.Cell(rowIndex, 6).Style.DateFormat.Format = "dd/mm/yyyy";
            sheet.Cell(rowIndex, 7).Value = n.EvriHoraInicio.ToString("HH:mm");
            sheet.Cell(rowIndex, 8).Value = n.EvriHoraFin.ToString("HH:mm");
            sheet.Cell(rowIndex, 9).Value = n.EvriDuracionMinutos;
            sheet.Cell(rowIndex, 10).Value = IrrigationTypeLabels.GetValueOrDefault(
                n.EvriTipoRiego,
                n.EvriTipoRiego);
            sheet.Cell(rowIndex, 11).Value = n.EvriCaudalHoraLph;
            sheet.Cell(rowIndex, 12).Value = n.EvriTipoSuelo ?? string.Empty;
            sheet.Cell(rowIndex, 13).Value = row.DepartmentName ?? string.Empty;
            sheet.Cell(rowIndex, 14).Value = row.CityName ?? string.Empty;
            sheet.Cell(rowIndex, 15).Value = n.EvriFechaCreacion;
            sheet.Cell(rowIndex, 15).Style.DateFormat.Format = "dd/mm/yyyy hh:mm";
            rowIndex++;
        }

        sheet.Columns().AdjustToContents();
        sheet.SheetView.FreezeRows(1);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    internal static int EstimateDurationMinutes(TimeOnly start, TimeOnly end)
    {
        var startMinutes = start.Hour * 60 + start.Minute;
        var endMinutes = end.Hour * 60 + end.Minute;
        if (endMinutes >= startMinutes)
        {
            return endMinutes - startMinutes;
        }

        return (24 * 60 - startMinutes) + endMinutes;
    }

    private IQueryable<NoteLocationRow> QueryWithLocation()
    {
        return from n in _db.NotasEventoRiego.AsNoTracking()
            join c in _db.Ciudades.AsNoTracking() on n.CiuId equals c.CiuId into cj
            from c in cj.DefaultIfEmpty()
            join d in _db.Departamentos.AsNoTracking() on c.DepoId equals d.DepoId into dj
            from d in dj.DefaultIfEmpty()
            select new NoteLocationRow
            {
                Note = n,
                DepartmentName = d != null ? d.DepoNombre : null,
                CityName = c != null ? c.CiuNombre : null,
            };
    }

    private static IrrigationEventNoteDto ToDto(NoteLocationRow row)
    {
        var n = row.Note;
        return new IrrigationEventNoteDto
        {
            Id = n.EvriId,
            PlotName = n.EvriNombreParcelaLote,
            CropName = n.EvriCultivo,
            CropId = n.CultId,
            EventDate = n.EvriFecha.ToString("yyyy-MM-dd"),
            StartTime = n.EvriHoraInicio.ToString("HH:mm"),
            EndTime = n.EvriHoraFin.ToString("HH:mm"),
            DurationMinutes = n.EvriDuracionMinutos,
            IrrigationType = n.EvriTipoRiego,
            IrrigationTypeLabel = IrrigationTypeLabels.GetValueOrDefault(n.EvriTipoRiego, n.EvriTipoRiego),
            FlowRateLph = n.EvriCaudalHoraLph is null ? null : (double)n.EvriCaudalHoraLph,
            SoilType = n.EvriTipoSuelo,
            CityId = n.CiuId,
            DepartmentName = row.DepartmentName,
            CityName = row.CityName,
        };
    }

    private sealed class NoteLocationRow
    {
        public HidrtbNotaEventoRiego Note { get; set; } = null!;
        public string? DepartmentName { get; set; }
        public string? CityName { get; set; }
    }
}
