using Application.Persons.Dtos;
using Application.Services;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Data;

namespace Infrastructure.Segmentation;

public class SegmentationService : ISegmentationService
{
    private const string SegmentationQuery = """
IF 0 < ( SELECT COUNT(*) FROM ML_Segmentacion WITH (NOLOCK) WHERE Bup_key = @personBupId )
    WITH LatestSegmentation AS (
        SELECT
            c.Bup_key,
            MAX(c.Time_Key) AS MaxTimeKey
        FROM
            ML_Segmentacion WITH (NOLOCK) c
        WHERE
            c.Bup_key = @personBupId
        GROUP BY
            c.Bup_key
    )

    SELECT
        'GSS_clasificacion' AS indicator,
        1 AS value,
        (
            SELECT
                UPPER(LTRIM(RTRIM(ss.Grupo))) AS Grupo
            FROM
                ML_Segmentacion WITH (NOLOCK) ss
            JOIN
                LatestSegmentation ls ON ss.Bup_key = ls.Bup_key AND ss.Time_Key = ls.MaxTimeKey
            WHERE
                ss.Bup_key = @personBupId
        ) AS aditionalData,
        1 AS processOk
ELSE
    SELECT 'GSS_clasificacion' AS indicator, 0 AS value, '' AS aditionalData, 1 AS processOk;
""";

    private readonly SegmentationOptions _options;
    private readonly ILogger<SegmentationService> _logger;

    public SegmentationService(IOptions<SegmentationOptions> options, ILogger<SegmentationService> logger)
    {
        _options = options.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<SegmentationDto> GetSegmentationAsync(int personId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ConnectionString))
        {
            throw new InvalidOperationException("No se encontró la cadena de conexión para segmentación.");
        }

        await using var connection = new SqlConnection(_options.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = SegmentationQuery;
        command.CommandType = CommandType.Text;
        command.CommandTimeout = _options.CommandTimeoutSeconds;
        command.Parameters.Add(new SqlParameter("@personBupId", SqlDbType.Int) { Value = personId });

        _logger.LogInformation("Ejecutando consulta de segmentación para persona {PersonId} contra {DataSource}/{Database}", personId, connection.DataSource, connection.Database);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return new SegmentationDto
            {
                Indicator = reader.GetString(reader.GetOrdinal("indicator")),
                Value = reader.GetInt32(reader.GetOrdinal("value")),
                AditionalData = reader.IsDBNull(reader.GetOrdinal("aditionalData")) ? null : reader.GetString(reader.GetOrdinal("aditionalData")),
                ProcessOk = reader.GetInt32(reader.GetOrdinal("processOk"))
            };
        }

        _logger.LogWarning("No se encontró información de segmentación para la persona {PersonId}", personId);
        return new SegmentationDto { Indicator = "GSS_clasificacion", ProcessOk = 0, Value = 0 };
    }
}
